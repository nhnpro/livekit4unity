using System.Collections.Generic;
using LiveKitUnity.Runtime.Participants;
using LiveKitUnity.Runtime.TrackPublications;
using LiveKitUnity.Runtime.Tracks;
using LiveKitUnity.Runtime.Types;

namespace LiveKitUnity.Runtime.Events
{
    // Base interface for all LiveKit events
    public interface ILiveKitEvent
    {
    }

// Base interface for all Room events
    public interface IRoomEvent : ILiveKitEvent
    {
    }

// Base interface for all Participant events
    public interface IParticipantEvent : ILiveKitEvent
    {
    }

// Base interface for all Track events
    public interface ITrackEvent : ILiveKitEvent
    {
    }

// Base interface for all Engine events
    public interface IEngineEvent : ILiveKitEvent
    {
    }

// Base interface for all SignalClient events
    public interface ISignalEvent : ILiveKitEvent
    {
    }


// When the connection to the server has been interrupted and it's attempting
// to reconnect. Emitted by Room.
    public class RoomReconnectingEvent : IRoomEvent
    {
        public RoomReconnectingEvent()
        {
        }

        public override string ToString()
        {
            return $"{GetType()}";
        }
    }

// Connection to room is re-established. All existing state is preserved.
// Emitted by Room.
    public class RoomConnectedEvent : IRoomEvent
    {
        public RoomConnectedEvent()
        {
        }

        public override string ToString()
        {
            return $"{GetType()}";
        }
    }
    public class RoomReconnectedEvent : IRoomEvent
    {
        public RoomReconnectedEvent()
        {
        }

        public override string ToString()
        {
            return $"{GetType()}";
        }
    }


// Room restarting event. Emitted by Room.
    public class RoomRestartingEvent : IRoomEvent
    {
        public RoomRestartingEvent()
        {
        }

        public override string ToString()
        {
            return $"{GetType()}";
        }
    }

// Room restarted event. Emitted by Room.
    public class RoomRestartedEvent : IRoomEvent
    {
        public RoomRestartedEvent()
        {
        }

        public override string ToString()
        {
            return $"{GetType()}";
        }
    }

// Disconnected from the room. Emitted by Room.
    public class RoomDisconnectedEvent : IRoomEvent
    {
        public DisconnectReason? Reason { get; }

        public RoomDisconnectedEvent(DisconnectReason? reason)
        {
            Reason = reason;
        }

        public override string ToString()
        {
            return $"{GetType()}(reason = {Reason})";
        }
    }

// Room metadata has changed. Emitted by Room.
    public class RoomMetadataChangedEvent : IRoomEvent
    {
        public string? Metadata { get; }

        public RoomMetadataChangedEvent(string? metadata)
        {
            Metadata = metadata;
        }

        public override string ToString()
        {
            return $"{GetType()}()";
        }
    }

// Room recording status has changed. Emitted by Room.
    public class RoomRecordingStatusChanged : IRoomEvent
    {
        public bool ActiveRecording { get; }

        public RoomRecordingStatusChanged(bool activeRecording)
        {
            ActiveRecording = activeRecording;
        }

        public override string ToString()
        {
            return $"{GetType()}(activeRecording = {ActiveRecording})";
        }
    }

// Participant connected event. Emitted by Room.
    public class ParticipantConnectedEvent : IRoomEvent
    {
        public RemoteParticipant Participant { get; }

        public ParticipantConnectedEvent(RemoteParticipant participant)
        {
            Participant = participant;
        }

        public override string ToString()
        {
            return $"{GetType()}(participant: {Participant})";
        }
    }

// Participant disconnected event. Emitted by Room.
    public class ParticipantDisconnectedEvent : IRoomEvent
    {
        public RemoteParticipant Participant { get; }

        public ParticipantDisconnectedEvent(RemoteParticipant participant)
        {
            Participant = participant;
        }

        public override string ToString()
        {
            return $"{GetType()}(participant: {Participant})";
        }
    }

// Active speakers changed event. Emitted by Room.
    public class ActiveSpeakersChangedEvent : IRoomEvent
    {
        public List<Participant> Speakers { get; }

        public ActiveSpeakersChangedEvent(List<Participant> speakers)
        {
            Speakers = speakers;
        }

        public override string ToString()
        {
            var speakerNames = string.Join(", ", Speakers.ConvertAll(p => p.ToString()).ToArray());
            return $"{GetType()}(speakers: {speakerNames})";
        }
    }

// Track published event. Emitted by Room and RemoteParticipant.
    public class TrackPublishedEvent : IRoomEvent, IParticipantEvent
    {
        public RemoteParticipant Participant { get; }
        public RemoteTrackPublication Publication { get; }

        public TrackPublishedEvent(RemoteParticipant participant, RemoteTrackPublication publication)
        {
            Participant = participant;
            Publication = publication;
        }

        public override string ToString()
        {
            return $"{GetType()}(participant: {Participant}, publication: {Publication})";
        }
    }

// Track unpublished event. Emitted by Room and RemoteParticipant.
    public class TrackUnpublishedEvent : IRoomEvent, IParticipantEvent
    {
        public RemoteParticipant Participant { get; }
        public RemoteTrackPublication Publication { get; }

        public TrackUnpublishedEvent(RemoteParticipant participant, RemoteTrackPublication publication)
        {
            Participant = participant;
            Publication = publication;
        }

        public override string ToString()
        {
            return $"{GetType()}(participant: {Participant}, publication: {Publication})";
        }
    }

// Local track published event. Emitted by Room and LocalParticipant.
    public class LocalTrackPublishedEvent : IRoomEvent, IParticipantEvent
    {
        public LocalParticipant Participant { get; }
        public LocalTrackPublication Publication { get; }

        public LocalTrackPublishedEvent(LocalParticipant participant, LocalTrackPublication publication)
        {
            Participant = participant;
            Publication = publication;
        }

        public override string ToString()
        {
            return $"{GetType()}(participant: {Participant}, publication: {Publication})";
        }
    }


// Local track unpublished event. Emitted by Room and LocalParticipant.
    public class LocalTrackUnpublishedEvent : IRoomEvent, IParticipantEvent
    {
        public LocalParticipant Participant { get; }
        public LocalTrackPublication Publication { get; }

        public LocalTrackUnpublishedEvent(LocalParticipant participant, LocalTrackPublication publication)
        {
            Participant = participant;
            Publication = publication;
        }

        public override string ToString()
        {
            return $"{GetType()}(participant: {Participant}, publication: {Publication})";
        }
    }

// Track subscribed event. Emitted by Room and RemoteParticipant.
    public class TrackSubscribedEvent : IRoomEvent, IParticipantEvent
    {
        public RemoteParticipant Participant { get; }
        public TrackPublication Publication { get; }
        public Track Track { get; }

        public TrackSubscribedEvent(RemoteParticipant participant, TrackPublication publication, Track track)
        {
            Participant = participant;
            Publication = publication;
            Track = track;
        }

        public override string ToString()
        {
            return $"{GetType()}(participant: {Participant}, publication: {Publication}, track: {Track})";
        }
    }

// Track subscription exception event. Emitted by Room and RemoteParticipant.
    public class TrackSubscriptionExceptionEvent : IRoomEvent, IParticipantEvent
    {
        public RemoteParticipant Participant { get; }
        public string Sid { get; }
        public TrackSubscribeFailReason Reason { get; }

        public TrackSubscriptionExceptionEvent(RemoteParticipant participant, string sid,
            TrackSubscribeFailReason reason)
        {
            Participant = participant;
            Sid = sid;
            Reason = reason;
        }
    }

// Track unsubscribed event. Emitted by Room and RemoteParticipant.
    public class TrackUnsubscribedEvent : IRoomEvent, IParticipantEvent
    {
        public RemoteParticipant Participant { get; }
        public RemoteTrackPublication Publication { get; }
        public Track Track { get; }

        public TrackUnsubscribedEvent(RemoteParticipant participant, RemoteTrackPublication publication, Track track)
        {
            Participant = participant;
            Publication = publication;
            Track = track;
        }

        public override string ToString()
        {
            return $"{GetType()}(participant: {Participant}, publication: {Publication}, track: {Track})";
        }
    }

// Track muted event. Emitted by RemoteParticipant and LocalParticipant.
    public class TrackMutedEvent : IRoomEvent, IParticipantEvent
    {
        public Participant Participant { get; }
        public TrackPublication Publication { get; }

        public TrackMutedEvent(Participant participant, TrackPublication publication)
        {
            Participant = participant;
            Publication = publication;
        }

        [System.Obsolete("Use Publication instead")]
        public TrackPublication Track => Publication;

        public override string ToString()
        {
            return $"{GetType()}(participant: {Participant}, publication: {Publication})";
        }
    }

// Track unmuted event. Emitted by RemoteParticipant and LocalParticipant.
    public class TrackUnmutedEvent : IRoomEvent, IParticipantEvent
    {
        public Participant Participant { get; }
        public TrackPublication Publication { get; }

        public TrackUnmutedEvent(Participant participant, TrackPublication publication)
        {
            Participant = participant;
            Publication = publication;
        }

        [System.Obsolete("Use Publication instead")]
        public TrackPublication Track => Publication;

        public override string ToString()
        {
            return $"{GetType()}(participant: {Participant}, publication: {Publication})";
        }
    }


// Track stream state updated event. Emitted by Room and RemoteParticipant.
    public class TrackStreamStateUpdatedEvent : IRoomEvent, IParticipantEvent
    {
        public RemoteParticipant Participant { get; }
        public RemoteTrackPublication Publication { get; }
        public StreamState StreamState { get; }

        public TrackStreamStateUpdatedEvent(RemoteParticipant participant, RemoteTrackPublication publication,
            StreamState streamState)
        {
            Participant = participant;
            Publication = publication;
            StreamState = streamState;
        }

        [System.Obsolete("Use Publication instead")]
        public RemoteTrackPublication TrackPublication => Publication;

        public override string ToString()
        {
            return $"{GetType()}(participant: {Participant}, publication: {Publication}, streamState: {StreamState})";
        }
    }

// Participant metadata updated event. Emitted by Room and Participant.
    public class ParticipantMetadataUpdatedEvent : IRoomEvent, IParticipantEvent
    {
        public Participant Participant { get; }

        public ParticipantMetadataUpdatedEvent(Participant participant)
        {
            Participant = participant;
        }

        public override string ToString()
        {
            return $"{GetType()}(participant: {Participant})";
        }
    }

// Participant connection quality updated event. Emitted by Room and Participant.
    public class ParticipantConnectionQualityUpdatedEvent : IRoomEvent, IParticipantEvent
    {
        public Participant Participant { get; }
        public ConnectionQuality ConnectionQuality { get; }

        public ParticipantConnectionQualityUpdatedEvent(Participant participant, ConnectionQuality connectionQuality)
        {
            Participant = participant;
            ConnectionQuality = connectionQuality;
        }

        public override string ToString()
        {
            return $"{GetType()}(participant: {Participant}, connectionQuality: {ConnectionQuality})";
        }
    }

// Data received event. Emitted by Room and RemoteParticipant.
    public class DataReceivedEvent : IRoomEvent, IParticipantEvent
    {
        public RemoteParticipant Participant { get; }
        public byte[] Data { get; }
        public string Topic { get; }

        public DataReceivedEvent(RemoteParticipant participant, byte[] data, string topic)
        {
            Participant = participant;
            Data = data;
            Topic = topic;
        }

        public override string ToString()
        {
            return $"{GetType()}(participant: {Participant}, data: {Data})";
        }
    }

// Speaking changed event. Emitted by Participant.
    public class SpeakingChangedEvent : IParticipantEvent
    {
        public Participant Participant { get; }
        public bool Speaking { get; }

        public SpeakingChangedEvent(Participant participant, bool speaking)
        {
            Participant = participant;
            Speaking = speaking;
        }

        public override string ToString()
        {
            return $"{GetType()}(participant: {Participant}, speaking: {Speaking})";
        }
    }

// Track subscription permission changed event. Emitted by Room and Participant.
    public class TrackSubscriptionPermissionChangedEvent : IRoomEvent, IParticipantEvent
    {
        public Participant Participant { get; }
        public RemoteTrackPublication Publication { get; }
        public TrackSubscriptionState State { get; }

        public TrackSubscriptionPermissionChangedEvent(Participant participant, RemoteTrackPublication publication,
            TrackSubscriptionState state)
        {
            Participant = participant;
            Publication = publication;
            State = state;
        }

        public override string ToString()
        {
            return $"{GetType()}(participant: {Participant}, publication: {Publication}, state: {State})";
        }
    }

// Participant permissions updated event. Emitted by Room and LocalParticipant.
    public class ParticipantPermissionsUpdatedEvent : IRoomEvent, IParticipantEvent
    {
        public Participant Participant { get; }
        public ParticipantPermissions Permissions { get; }
        public ParticipantPermissions OldPermissions { get; }

        public ParticipantPermissionsUpdatedEvent(Participant participant, ParticipantPermissions permissions,
            ParticipantPermissions oldPermissions)
        {
            Participant = participant;
            Permissions = permissions;
            OldPermissions = oldPermissions;
        }

        public override string ToString()
        {
            return $"{GetType()}(participant: {Participant}, permissions: {Permissions})";
        }
    }

// Participant name updated event. Emitted by Room and Participant.
    public class ParticipantNameUpdatedEvent : IRoomEvent, IParticipantEvent
    {
        public Participant Participant { get; }
        public string Name { get; }

        public ParticipantNameUpdatedEvent(Participant participant, string name)
        {
            Participant = participant;
            Name = name;
        }

        public override string ToString()
        {
            return $"{GetType()}(participant: {Participant}, name: {Name})";
        }
    }

// Audio playback status changed event. Emitted by Room.
    public class AudioPlaybackStatusChanged : IRoomEvent
    {
        public bool IsPlaying { get; }

        public AudioPlaybackStatusChanged(bool isPlaying)
        {
            IsPlaying = isPlaying;
        }

        public override string ToString()
        {
            return $"{GetType()}(Audio Playback Status Changed, isPlaying: {IsPlaying})";
        }
    }

// Audio sender stats event. Emitted by Track.
    public class AudioSenderStatsEvent : ITrackEvent
    {
        public AudioSenderStats Stats { get; }
        public float CurrentBitrate { get; }

        public AudioSenderStatsEvent(AudioSenderStats stats, float currentBitrate)
        {
            Stats = stats;
            CurrentBitrate = currentBitrate;
        }

        public override string ToString()
        {
            return $"{GetType()}(stats: {Stats})";
        }
    }

// Video sender stats event. Emitted by Track.
    public class VideoSenderStatsEvent : ITrackEvent
    {
        public Dictionary<string, VideoSenderStats> Stats { get; }
        public Dictionary<string, float> BitrateForLayers { get; }
        public float CurrentBitrate { get; }

        public VideoSenderStatsEvent(Dictionary<string, VideoSenderStats> stats,
            Dictionary<string, float> bitrateForLayers, float currentBitrate)
        {
            Stats = stats;
            BitrateForLayers = bitrateForLayers;
            CurrentBitrate = currentBitrate;
        }

        public override string ToString()
        {
            return $"{GetType()}(stats: {Stats})";
        }
    }

// Audio receiver stats event. Emitted by Track.
    public class AudioReceiverStatsEvent : ITrackEvent
    {
        public AudioReceiverStats Stats { get; }
        public float CurrentBitrate { get; }

        public AudioReceiverStatsEvent(AudioReceiverStats stats, float currentBitrate)
        {
            Stats = stats;
            CurrentBitrate = currentBitrate;
        }

        public override string ToString()
        {
            return $"{GetType()}(stats: {Stats})";
        }
    }

// Video receiver stats event. Emitted by Track.
    public class VideoReceiverStatsEvent : ITrackEvent
    {
        public VideoReceiverStats Stats { get; }
        public float CurrentBitrate { get; }

        public VideoReceiverStatsEvent(VideoReceiverStats stats, float currentBitrate)
        {
            Stats = stats;
            CurrentBitrate = currentBitrate;
        }

        public override string ToString()
        {
            return $"{GetType()}(stats: {Stats})";
        }
    }
}