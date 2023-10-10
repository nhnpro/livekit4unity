using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
//using System.Threading.Tasks;
using LiveKit.Proto;
using LiveKitUnity.Runtime.Debugging;
// using LiveKit.Proto;
using LiveKitUnity.Runtime.Events;
using LiveKitUnity.Runtime.TrackPublications;
using LiveKitUnity.Runtime.Types;
using ConnectionQuality = LiveKitUnity.Runtime.Types.ConnectionQuality;
using TrackSource = LiveKitUnity.Runtime.Types.TrackSource;

namespace LiveKitUnity.Runtime.Participants
{
    public abstract class Participant : EventsEmittable
    {
        public Core.Room room;

        public Dictionary<string, TrackPublication> TrackPublications { get; } =
            new Dictionary<string, TrackPublication>();

        public double AudioLevel { get; set; } = 0;
        public string Sid { get; protected set; }
        public string Identity { get; protected set; }

        private string _name;

        public string Name
        {
            get => _name;
            protected set { _name = value; }
        }

        public string Metadata { get; set; }
        public DateTime? LastSpokeAt { get; set; }

        private ParticipantInfo _participantInfo;
        private bool _isSpeaking = false;
        private ConnectionQuality _connectionQuality = ConnectionQuality.Unknown;

        ParticipantPermissions _permissions = new();
        public ParticipantPermissions Permissions => _permissions;


        public bool IsSpeaking
        {
            get => _isSpeaking;
            set
            {
                if (_isSpeaking == value)
                {
                    return;
                }

                _isSpeaking = value;
                if (value)
                {
                    LastSpokeAt = DateTime.Now;
                }

                var e = new SpeakingChangedEvent(this, value);
                events.Emit(e);
                room.events.Emit(e);
            }
        }

        public ConnectionQuality ConnectionQuality
        {
            get => _connectionQuality;
            protected set => _connectionQuality = value;
        }

        public DateTime JoinedAt
        {
            get
            {
                if (_participantInfo != null)
                {
                    return DateTimeOffset.FromUnixTimeSeconds(_participantInfo.JoinedAt).UtcDateTime;
                }

                return DateTime.Now;
            }
        }

        public bool IsMuted
        {
            get
            {
                if (AudioTracks.Count > 0)
                {
                    return AudioTracks[0].Muted;
                }

                return true;
            }
        }

        public List<TrackPublication> AudioTracks
        {
            get { return TrackPublications.Values.Where(track => track.Kind == TrackType.Audio).ToList(); }
        }

        public List<TrackPublication> VideoTracks
        {
            get { return TrackPublications.Values.Where(track => track.Kind == TrackType.Video).ToList(); }
        }

        public bool HasAudio => AudioTracks.Count > 0;

        public bool HasVideo => VideoTracks.Count > 0;

        public EncryptionType FirstTrackEncryptionType
        {
            get
            {
                if (HasAudio)
                {
                    return AudioTracks[0].EncryptionType;
                }
                else if (HasVideo)
                {
                    return VideoTracks[0].EncryptionType;
                }
                else
                {
                    return EncryptionType.None;
                }
            }
        }

        protected bool HasInfo => _participantInfo != null;


        protected Participant(LiveKitUnity.Runtime.Core.Room room, string sid, string identity, string name)
        {
            this.room = room;
            Sid = sid;
            Identity = identity;
            Name = name;

            events.Listen(eventData =>
            {
                this.Log($"[ParticipantEvent] {eventData}, will NotifyListeners()");
                // notifyListeners();
            });
        }

        private void SetMetadata(string md)
        {
            bool changed = _participantInfo?.Metadata != md;
            Metadata = md;
            if (changed)
            {
                var e = new ParticipantMetadataUpdatedEvent(this);
                events.Emit(e);
                room.events.Emit(e);
            }
        }

        internal void UpdateConnectionQuality(ConnectionQuality quality)
        {
            if (_connectionQuality == quality)
            {
                return;
            }

            _connectionQuality = quality;
            events.Emit(new ParticipantConnectionQualityUpdatedEvent(this, _connectionQuality));
            room.events.Emit(new ParticipantConnectionQualityUpdatedEvent(this, _connectionQuality));
        }

        public virtual void UpdateFromInfo(ParticipantInfo info)
        {
            Identity = info.Identity;
            Sid = info.Sid;
            UpdateName(info.Name);
            if (!string.IsNullOrEmpty(info.Metadata))
            {
                SetMetadata(info.Metadata);
            }

            _participantInfo = info;
            SetPermissions(info.Permission.ToLKType());
        }


        public virtual ParticipantPermissions SetPermissions(ParticipantPermissions newValue)
        {
            if (_permissions == newValue)
            {
                return null;
            }

            var oldValue = _permissions;
            _permissions = newValue;
            return oldValue;
        }

        internal void UpdateName(string name)
        {
            if (_name == name)
            {
                return;
            }

            _name = name;
            events.Emit(new ParticipantNameUpdatedEvent(this, name));
            room.events.Emit(new ParticipantNameUpdatedEvent(this, name));
        }

// For internal use
        internal void RemoveTrackPublication(string sid)
        {
            TrackPublications.Remove(sid);
        }

        internal void AddTrackPublication(TrackPublication pub)
        {
            if (pub.Track != null)
            {
                pub.Track.Sid = pub.Sid;
            }

            TrackPublications[pub.Sid] = pub;
        }

        public abstract UniTask UnpublishTrack(string trackSid, bool notify = true);

        public async UniTask UnpublishAllTracks(bool notify = true, bool? stopOnUnpublish = null)
        {
            var trackSids = new HashSet<string>(TrackPublications.Keys);
            foreach (var trackId in trackSids)
            {
                await UnpublishTrack(trackId, notify);
            }
        }

        public bool IsCameraEnabled()
        {
            var cameraTrackPublication = getTrackPublicationBySource(TrackSource.Camera);
            return cameraTrackPublication is { Muted: false };
        }

        public bool IsMicrophoneEnabled()
        {
            var microphoneTrackPublication = getTrackPublicationBySource(TrackSource.Microphone);
            return microphoneTrackPublication is { Muted: false };
        }

        public bool IsScreenShareEnabled()
        {
            var screenShareVideoTrackPublication = getTrackPublicationBySource(TrackSource.ScreenShareVideo);
            return screenShareVideoTrackPublication is { Muted: false };
        }

        public TrackPublication getTrackPublicationBySource(TrackSource source)
        {
            if (source == TrackSource.Unknown)
            {
                return null;
            }

            // Try to find by source
            var result = TrackPublications.Values.FirstOrDefault(e => e.Source == source);
            if (result != null)
            {
                return result;
            }

            // Try to find by compatibility
            return TrackPublications.Values.FirstOrDefault(e =>
                (source == TrackSource.Microphone && e.Kind == TrackType.Audio) ||
                (source == TrackSource.Camera && e.Kind == TrackType.Video) ||
                (source == TrackSource.ScreenShareVideo && e.Kind == TrackType.Video) ||
                (source == TrackSource.ScreenShareAudio && e.Kind == TrackType.Audio));
        }


        public bool IsDisposed = false;

        public async UniTask Dispose()
        {
            IsDisposed = true;
            await events.Dispose();
            await UnpublishAllTracks();
        }
    }
}