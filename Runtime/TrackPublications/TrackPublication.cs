using System;
using Cysharp.Threading.Tasks;
//using System.Threading.Tasks;
using LiveKit.Proto;
using LiveKitUnity.Runtime.Debugging;
using LiveKitUnity.Runtime.Events;
using LiveKitUnity.Runtime.Tracks;
using LiveKitUnity.Runtime.Types;
using LiveKitUnity.Runtime.Participants;
using StreamState = LiveKitUnity.Runtime.Types.StreamState;
using TrackSource = LiveKitUnity.Runtime.Types.TrackSource;

namespace LiveKitUnity.Runtime.TrackPublications
{
    public abstract class TrackPublication : EventsEmittable
    {
        public string Sid { get; set; }
        public string Name { get; set; }
        public TrackType Kind { get; set; }
        public TrackSource Source { get; set; }

        private Track _track;
        public Track Track => _track;
        public abstract Participant Participant { get; set; }
        public bool Muted => Track?.Muted ?? false;

        private bool _simulcasted;
        public bool Simulcasted => _simulcasted;

        private string _mimeType;
        public string MimeType => _mimeType;


        private VideoDimensions _dimensions;
        public VideoDimensions? Dimensions => _dimensions;

        public virtual bool Subscribed => Track != null;

        public EncryptionType EncryptionType
        {
            get
            {
                if (latestInfo == null) return EncryptionType.None;
                return latestInfo.Encryption.ToLkType();
            }
        }

        public bool IsScreenShare => Kind == TrackType.Video && Source == TrackSource.ScreenShareVideo;


        internal TrackInfo latestInfo;


        public TrackSubscriptionState SubscriptionState { get; set; }


        public TrackPublication(TrackInfo trackInfo)
        {
            Sid = trackInfo.Sid;
            Name = trackInfo.Name;
            Kind = trackInfo.Type;
            Source = trackInfo.Source.ToLKType();
            UpdateFromInfo(trackInfo);
        }

        public void UpdateFromInfo(TrackInfo info)
        {
            _simulcasted = info.Simulcast;
            _mimeType = info.MimeType;
            if (info.Type == TrackType.Video)
            {
                _dimensions = new VideoDimensions((int)info.Width, (int)info.Height);
            }

            latestInfo = info;
        }

        public virtual void UpdateStreamState(LiveKit.Proto.StreamState streamState)
        {
            throw new NotImplementedException();
        }
        

        public virtual async UniTask<bool> UpdateTrack(Track newValue)
        {
            if (Track == newValue)
            {
                return false;
            }

            // Dispose previous track (if exists)
            if (Track != null)
            {
                await Track.Dispose();
            }

            _track = newValue;

            if (newValue != null)
            {
                // Listen for Track's muted events
                var listener = newValue.CreateListener(true);
                listener.On<InternalTrackMuteUpdatedEvent>(_OnTrackMuteUpdatedEvent);
                // Dispose listener when the track is disposed
                // newValue.OnDispose(() => listener.Dispose());
            }

            return true;
        }

        private void _OnTrackMuteUpdatedEvent(InternalTrackMuteUpdatedEvent eventArgs)
        {
            if (Participant == null)
            {
                return;
            }

            // Send signal to server (if mute initiated by local user)
            if (eventArgs.ShouldSendSignal)
            {
                this.Log($"{this} Sending mute signal... sid:{Sid}, muted:{eventArgs.Muted}");
                Participant.room?.engine.Client.SendMuteTrack(Sid, eventArgs.Muted);
            }

            // Emit events
            IRoomEvent newEvent = eventArgs.Muted
                ? new TrackMutedEvent(Participant, this)
                : new TrackUnmutedEvent(Participant, this);

            Participant.events.Emit(newEvent);
            Participant.room?.events.Emit(newEvent);
        }


        public virtual async UniTask Mute()
        {
            throw new System.NotImplementedException();
        }

        public virtual async UniTask Unmute()
        {
            throw new System.NotImplementedException();
        }

        public virtual async UniTask UpdateSubscriptionAllowed(bool eventDataAllowed)
        {
            this.Log("UpdateSubscriptionAllowed NotImplemented");
        }

        public async UniTask Dispose()
        {
            if (_track != null)
            {
                await _track.Dispose();
            }
        }
    }
}