using System.Collections.Generic;
using LiveKit.Proto;
using LiveKitUnity.Runtime.Tracks;
using LiveKitUnity.Runtime.Types;
using Unity.WebRTC;
using DisconnectReason = LiveKitUnity.Runtime.Types.DisconnectReason;

namespace LiveKitUnity.Runtime.Events
{
    public interface InternalEvent : ILiveKitEvent
    {
        // Add any properties or methods specific to InternalEvent
    }

    public abstract class EnginePeerStateUpdatedEvent : IEngineEvent, InternalEvent
    {
        public RTCPeerConnectionState State { get; }
        public bool IsPrimary { get; }

        protected EnginePeerStateUpdatedEvent(RTCPeerConnectionState state, bool isPrimary)
        {
            State = state;
            IsPrimary = isPrimary;
        }
    }

    internal class EngineSubscriberPeerStateUpdatedEvent : EnginePeerStateUpdatedEvent
    {
        public EngineSubscriberPeerStateUpdatedEvent(RTCPeerConnectionState state, bool isPrimary)
            : base(state, isPrimary)
        {
        }

        public override string ToString() => $"{GetType()}(state: {State}, isPrimary: {IsPrimary})";
    }

    internal class EnginePublisherPeerStateUpdatedEvent : EnginePeerStateUpdatedEvent
    {
        public EnginePublisherPeerStateUpdatedEvent(RTCPeerConnectionState state, bool isPrimary)
            : base(state, isPrimary)
        {
        }

        public override string ToString() => $"{GetType()}(state: {State}, isPrimary: {IsPrimary})";
    }

    internal class TrackStreamUpdatedEvent : ITrackEvent, InternalEvent
    {
        public Track Track { get; }
        public RTCRtpSender Sender { get; }
        public RTCRtpReceiver Receiver { get; }

        public TrackStreamUpdatedEvent(Track track, RTCRtpSender sender, RTCRtpReceiver receiver)
        {
            Track = track;
            Sender = sender;
            Receiver = receiver;
        }
    }

    internal class AudioPlaybackStarted : ITrackEvent, IEngineEvent, InternalEvent
    {
        public Track Track { get; }

        public AudioPlaybackStarted(Track track)
        {
            Track = track;
        }
    }

    internal class AudioPlaybackFailed : ITrackEvent, IEngineEvent, InternalEvent
    {
        public Track Track { get; }

        public AudioPlaybackFailed(Track track)
        {
            Track = track;
        }
    }

    internal class LocalTrackOptionsUpdatedEvent : ITrackEvent, InternalEvent
    {
        public LocalTrack Track { get; }
        public LocalTrackOptions Options { get; }

        public LocalTrackOptionsUpdatedEvent(LocalTrack track, LocalTrackOptions options)
        {
            Track = track;
            Options = options;
        }
    }

    internal class InternalTrackMuteUpdatedEvent : ITrackEvent, InternalEvent
    {
        public Track Track { get; }
        public bool Muted { get; }
        public bool ShouldSendSignal { get; }

        public InternalTrackMuteUpdatedEvent(Track track, bool muted, bool shouldSendSignal)
        {
            Track = track;
            Muted = muted;
            ShouldSendSignal = shouldSendSignal;
        }

        public override string ToString() => $"TrackMuteUpdatedEvent(track: {Track}, muted: {Muted})";
    }

    internal class SignalJoinResponseEvent : ISignalEvent, InternalEvent
    {
        public JoinResponse Response { get; }

        public SignalJoinResponseEvent(JoinResponse response)
        {
            Response = response;
        }
    }

    internal class SignalReconnectResponseEvent : ISignalEvent, InternalEvent
    {
        public ReconnectResponse Response { get; }

        public SignalReconnectResponseEvent(ReconnectResponse response)
        {
            Response = response;
        }
    }

    internal abstract class ConnectionStateUpdatedEvent : InternalEvent
    {
        public ConnectionState NewState { get; }
        public ConnectionState OldState { get; }
        public bool DidReconnect { get; }
        public DisconnectReason? DisconnectReason { get; }

        protected ConnectionStateUpdatedEvent(
            ConnectionState newState,
            ConnectionState oldState,
            bool didReconnect,
            DisconnectReason? disconnectReason)
        {
            NewState = newState;
            OldState = oldState;
            DidReconnect = didReconnect;
            DisconnectReason = disconnectReason;
        }

        public override string ToString() =>
            $"{GetType()}(newState: {NewState}, didReconnect: {DidReconnect}, disconnectReason: {DisconnectReason})";
    }

    internal class SignalConnectionStateUpdatedEvent : ConnectionStateUpdatedEvent, ISignalEvent
    {
        public SignalConnectionStateUpdatedEvent(
            ConnectionState newState,
            ConnectionState oldState,
            bool didReconnect,
            LiveKitUnity.Runtime.Types.DisconnectReason? disconnectReason)
            : base(newState, oldState, didReconnect, disconnectReason)
        {
        }
    }

    internal class EngineConnectionStateUpdatedEvent : ConnectionStateUpdatedEvent, IEngineEvent
    {
        public bool FullReconnect { get; }

        public EngineConnectionStateUpdatedEvent(
            ConnectionState newState,
            ConnectionState oldState,
            bool didReconnect,
            bool fullReconnect,
            DisconnectReason? disconnectReason)
            : base(newState, oldState, didReconnect, disconnectReason)
        {
            FullReconnect = fullReconnect;
        }
    }

    internal class SignalOfferEvent : ISignalEvent, InternalEvent
    {
        public RTCSessionDescription Sd { get; }

        public SignalOfferEvent(RTCSessionDescription sd)
        {
            Sd = sd;
        }
    }

    internal class SignalAnswerEvent : ISignalEvent, InternalEvent
    {
        public RTCSessionDescription Sd { get; }

        public SignalAnswerEvent(RTCSessionDescription sd)
        {
            Sd = sd;
        }
    }

    internal class SignalTrickleEvent : ISignalEvent, InternalEvent
    {
        public RTCIceCandidate Candidate { get; }
        public SignalTarget Target { get; }

        public SignalTrickleEvent(RTCIceCandidate candidate, SignalTarget target)
        {
            Candidate = candidate;
            Target = target;
        }
    }

    internal class SignalParticipantUpdateEvent : ISignalEvent, InternalEvent
    {
        public List<ParticipantInfo> Participants { get; }

        public SignalParticipantUpdateEvent(List<ParticipantInfo> participants)
        {
            Participants = participants;
        }
    }

    internal class SignalConnectionQualityUpdateEvent : ISignalEvent, InternalEvent
    {
        public List<ConnectionQualityInfo> Updates { get; }

        public SignalConnectionQualityUpdateEvent(List<ConnectionQualityInfo> updates)
        {
            Updates = updates;
        }
    }

    internal class SignalLocalTrackPublishedEvent : ISignalEvent, InternalEvent
    {
        public string Cid { get; }
        public TrackInfo Track { get; }

        public SignalLocalTrackPublishedEvent(string cid, TrackInfo track)
        {
            Cid = cid;
            Track = track;
        }
    }

    internal class SignalTrackUnpublishedEvent : ISignalEvent, InternalEvent
    {
        public string TrackSid { get; }

        public SignalTrackUnpublishedEvent(string trackSid)
        {
            TrackSid = trackSid;
        }
    }

    internal class SignalRoomUpdateEvent : ISignalEvent, InternalEvent
    {
        public Room Room { get; }

        public SignalRoomUpdateEvent(Room room)
        {
            Room = room;
        }
    }

    internal class SignalSpeakersChangedEvent : ISignalEvent, InternalEvent
    {
        public List<SpeakerInfo> Speakers { get; }

        public SignalSpeakersChangedEvent(List<SpeakerInfo> speakers)
        {
            Speakers = speakers;
        }
    }

    internal class EngineActiveSpeakersUpdateEvent : IEngineEvent, InternalEvent
    {
        public List<SpeakerInfo> Speakers { get; }

        public EngineActiveSpeakersUpdateEvent(List<SpeakerInfo> speakers)
        {
            Speakers = speakers;
        }
    }

    internal class SignalLeaveEvent : ISignalEvent, InternalEvent
    {
        public bool CanReconnect { get; }
        public LiveKit.Proto.DisconnectReason Reason { get; }

        public SignalLeaveEvent(bool canReconnect, LiveKit.Proto.DisconnectReason reason)
        {
            CanReconnect = canReconnect;
            Reason = reason;
        }
    }

    internal class SignalRemoteMuteTrackEvent : ISignalEvent, InternalEvent
    {
        public string Sid { get; }
        public bool Muted { get; }

        public SignalRemoteMuteTrackEvent(string sid, bool muted)
        {
            Sid = sid;
            Muted = muted;
        }
    }

    internal class SignalStreamStateUpdatedEvent : ISignalEvent, InternalEvent
    {
        public List<StreamStateInfo> Updates { get; }

        public SignalStreamStateUpdatedEvent(List<StreamStateInfo> updates)
        {
            Updates = updates;
        }
    }

    internal class SignalSubscribedQualityUpdatedEvent : ISignalEvent, InternalEvent
    {
        public string TrackSid { get; }
        public List<SubscribedQuality> SubscribedQualities { get; }
        public List<SubscribedCodec> SubscribedCodecs { get; }

        public SignalSubscribedQualityUpdatedEvent(
            string trackSid,
            List<SubscribedQuality> subscribedQualities,
            List<SubscribedCodec> subscribedCodecs)
        {
            TrackSid = trackSid;
            SubscribedQualities = subscribedQualities;
            SubscribedCodecs = subscribedCodecs;
        }
    }

    internal class SignalSubscriptionPermissionUpdateEvent : ISignalEvent, InternalEvent
    {
        public string ParticipantSid { get; }
        public string TrackSid { get; }
        public bool Allowed { get; }

        public SignalSubscriptionPermissionUpdateEvent(
            string participantSid,
            string trackSid,
            bool allowed)
        {
            ParticipantSid = participantSid;
            TrackSid = trackSid;
            Allowed = allowed;
        }
    }

    internal class SignalTokenUpdatedEvent : ISignalEvent, InternalEvent
    {
        public string Token { get; }

        public SignalTokenUpdatedEvent(string token)
        {
            Token = token;
        }
    }

    internal class EngineTrackAddedEvent : IEngineEvent, InternalEvent
    {
        public MediaStreamTrack Track { get; }
        public MediaStream Stream { get; }
        public RTCRtpTransceiver Transceiver { get; }

        public EngineTrackAddedEvent(
            MediaStreamTrack track,
            MediaStream stream,
            RTCRtpTransceiver transceiver)
        {
            Track = track;
            Stream = stream;
            Transceiver = transceiver;
        }
    }

    internal class EngineDataPacketReceivedEvent : IEngineEvent, InternalEvent
    {
        public UserPacket Packet { get; }
        public DataPacket.Types.Kind Kind { get; }

        public EngineDataPacketReceivedEvent(
            UserPacket packet,
            DataPacket.Types.Kind kind)
        {
            Packet = packet;
            Kind = kind;
        }
    }

    internal abstract class DataChannelStateUpdatedEvent : IEngineEvent, InternalEvent
    {
        public bool IsPrimary { get; }
        public Reliability Type { get; }
        public RTCDataChannelState State { get; }

        protected DataChannelStateUpdatedEvent(
            bool isPrimary,
            Reliability type,
            RTCDataChannelState state)
        {
            IsPrimary = isPrimary;
            Type = type;
            State = state;
        }
    }

    internal class PublisherDataChannelStateUpdatedEvent : DataChannelStateUpdatedEvent
    {
        public PublisherDataChannelStateUpdatedEvent(
            bool isPrimary,
            Reliability type,
            RTCDataChannelState state)
            : base(isPrimary, type, state)
        {
        }
    }

    internal class SubscriberDataChannelStateUpdatedEvent : DataChannelStateUpdatedEvent
    {
        public SubscriberDataChannelStateUpdatedEvent(
            bool isPrimary,
            Reliability type,
            RTCDataChannelState state)
            : base(isPrimary, type, state)
        {
        }
    }
}