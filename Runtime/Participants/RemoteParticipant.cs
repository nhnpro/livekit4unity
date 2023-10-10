using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
//using System.Threading.Tasks;
using LiveKit.Proto;
using LiveKitUnity.Runtime.Debugging;
using LiveKitUnity.Runtime.Events;
using LiveKitUnity.Runtime.TrackPublications;
using LiveKitUnity.Runtime.Tracks;
using LiveKitUnity.Runtime.Types;
using Unity.WebRTC;
using Room = LiveKitUnity.Runtime.Core.Room;

namespace LiveKitUnity.Runtime.Participants
{
    public class RemoteParticipant : Participant
    {
        public List<RemoteTrackPublication> SubscribedTracks
        {
            get
            {
                return TrackPublications.Values
                    .Where(e => e.Subscribed)
                    .OfType<RemoteTrackPublication>()
                    .ToList();
            }
        }

        public RemoteParticipant(Room room, string sid, string identity, string name) : base(room, sid, identity, name)
        {
        }

        public RemoteParticipant(Room room, ParticipantInfo info) : base(room, info.Sid, info.Identity, info.Name)
        {
            UpdateFromInfo(info);
        }

        public RemoteTrackPublication GetTrackPublication(string sid)
        {
            if (TrackPublications.TryGetValue(sid, out var pub)
                && pub is RemoteTrackPublication remotePub)
            {
                return remotePub;
            }

            return null;
        }

        public override async void UpdateFromInfo(ParticipantInfo info)
        {
            this.Log($"RemoteParticipant.UpdateFromInfo(info: {info})");
            base.UpdateFromInfo(info);

            // figuring out deltas between tracks
            var newPubs = new List<RemoteTrackPublication>();

            foreach (var trackInfo in info.Tracks)
            {
                RemoteTrackPublication pub = GetTrackPublication(trackInfo.Sid);
                if (pub == null)
                {
                    switch (trackInfo.Type)
                    {
                        case TrackType.Video:
                        case TrackType.Audio:
                            pub = new RemoteTrackPublication(this, trackInfo, null);
                            break;
                        default:
                            throw new UnexpectedStateException("Unknown track type");
                    }

                    newPubs.Add(pub);
                    AddTrackPublication(pub);
                }
                else
                {
                    pub.UpdateFromInfo(trackInfo);
                }
            }

            if (room.ConnectionState == ConnectionState.Connected)
            {
                // always emit events for new publications, Room will not forward them unless it's ready
                foreach (var pub in newPubs)
                {
                    this.Log($"Emitting TrackPublishedEvent for {pub.Sid}");
                    var eventArgs = new TrackPublishedEvent(this, pub);
                    events.Emit(eventArgs);
                    room.events.Emit(eventArgs);
                }
            }

            // remove any published track that is not in the info
            var validSids = info.Tracks.Select(e => e.Sid);
            var removeSids = TrackPublications.Keys.Where(e => !validSids.Contains(e)).ToList();
            foreach (var sid in removeSids)
            {
                await RemovePublishedTrack(sid);
            }
        }

        public override UniTask UnpublishTrack(string trackSid, bool notify = true)
        {
            return RemovePublishedTrack(trackSid, notify);
        }

        public async UniTask RemovePublishedTrack(string trackSid, bool notify = true)
        {
            this.Log($"RemovePublishedTrack track sid: {trackSid}, notify: {notify}");
            TrackPublications.TryGetValue(trackSid, out var pub);
            // var pub = TrackPublications.Remove(trackSid);
            if (pub == null)
            {
                this.LogWarning($"Publication not found {trackSid}");
                return;
            }

            // await pub.Dispose();

            var track = pub.Track;
            // if has track
            if (track != null)
            {
                await track.Stop();
                if (track is RemoteAudioTrack)
                {
                    AudioManager.Instance.RemoveAudio(this.Sid, track.GetCid());
                }

                var unsubscribedEvent = new TrackUnsubscribedEvent(this, pub as RemoteTrackPublication, track);
                events.Emit(unsubscribedEvent);
                room.events.Emit(unsubscribedEvent);
            }

            if (notify)
            {
                var unpublishedEvent = new TrackUnpublishedEvent(this, pub as RemoteTrackPublication);
                events.Emit(unpublishedEvent);
                room.events.Emit(unpublishedEvent);
            }

            await pub.Dispose();
            RemoveTrackPublication(trackSid);
        }

        public async UniTask AddSubscribedMediaTrack(MediaStreamTrack mediaTrack, MediaStream stream,
            string trackSid, RTCRtpReceiver receiver = null, AudioOutputOptions audioOutputOptions = null)
        {
            this.Log("AddSubscribedMediaTrack()");

            // If publication doesn't exist yet...
            RemoteTrackPublication pub = GetTrackPublication(trackSid);
            if (pub == null)
            {
                this.Log("AddSubscribedMediaTrack() pub is null, will wait...");
                this.Log($"AddSubscribedMediaTrack() tracks: {string.Join(",", TrackPublications)}");
                // Wait for the metadata to arrive
                var eventArgs = await events.WaitFor<TrackPublishedEvent>(
                    filter: e => e.Participant == this && e.Publication.Sid == trackSid,
                    duration: room.ConnectOptions.Timeouts.Publish,
                    onTimeout: () => throw new Exception("TrackPublishedEvent not received in time"));

                pub = eventArgs.Publication;
                this.Log("AddSubscribedMediaTrack() did receive pub");
            }

            // Check if track type is supported, throw if not.
            if (pub.Kind != TrackType.Audio && pub.Kind != TrackType.Video)
            {
                throw new Exception($"Unsupported track type: {pub.Kind}");
            }

            // Create Track
            RemoteTrack track;
            if (pub.Kind == TrackType.Video)
            {
                // Video track
                track = new RemoteVideoTrack(pub.Source.ToPBType(), mediaTrack, receiver);
            }
            else if (pub.Kind == TrackType.Audio)
            {
                // Audio track
                track = new RemoteAudioTrack(pub.Source.ToPBType(), mediaTrack, receiver);

                var listener = track.CreateListener(true);
                listener.On<AudioPlaybackStarted>(eventArgs =>
                {
                    this.Log("AudioPlaybackStarted");
                    room.engine.events.Emit(eventArgs);
                });

                listener.On<AudioPlaybackFailed>(eventArgs =>
                {
                    this.Log("AudioPlaybackFailed");
                    room.engine.events.Emit(eventArgs);
                });
            }
            else
            {
                throw new UnexpectedStateException("Unknown track type");
            }

            await track.Start(this);

            // Apply audio output selection for the web.
            /*if (pub.Kind == TrackType.Audio && lkPlatformIs(PlatformType.Web))
            {
                if (audioOutputOptions?.DeviceId != null)
                {
                    await ((RemoteAudioTrack)track).SetSinkId(audioOutputOptions.DeviceId);
                }
            }*/

            await pub.UpdateTrack(track);
            await pub.UpdateSubscriptionAllowed(true);
            AddTrackPublication(pub);

            var newEvent = new TrackSubscribedEvent(this, pub, track);
            events.Emit(newEvent);
            room.events.Emit(newEvent);
        }

        public ParticipantTracks ParticipantTracks()
        {
            var trackSids = TrackPublications.Values.Select(e => e.Sid).ToList();
            return new ParticipantTracks
            {
                ParticipantSid = Sid,
                TrackSids = { trackSids }
            };
        }
    }
}