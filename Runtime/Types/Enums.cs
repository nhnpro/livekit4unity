using System.Collections.Generic;

namespace LiveKitUnity.Runtime.Types
{
// Enums
    public enum ClientDisconnectReason {
        User,
        PeerConnectionClosed,
        NegotiationFailed,
        Signal,
        Reconnect,
        LeaveReconnect,
    }
    
    public enum ProtocolVersion
    {
        V2,
        V3, // Subscriber as primary
        V4,
        V5,
        V6, // Session migration
        V7, // Remote unpublish
        V8,
        V9,
    }

    public enum ConnectionState
    {
        Disconnected,
        Connecting,
        Reconnecting,
        Connected,
    }

    public enum ConnectionQuality
    {
        Unknown,
        Poor,
        Good,
        Excellent,
    }

    public enum Reliability
    {
        Reliable,
        Lossy,
    }

    public enum TrackSource
    {
        Unknown,
        Camera,
        Microphone,
        ScreenShareVideo,
        ScreenShareAudio,
    }

    public enum TrackSubscriptionState
    {
        Unsubscribed,
        Subscribed,
        NotAllowed,
    }

    public enum StreamState
    {
        Paused,
        Active,
    }

    public enum DisconnectReason
    {
        Unknown,
        ClientInitiated,
        DuplicateIdentity,
        ServerShutdown,
        ParticipantRemoved,
        RoomDeleted,
        StateMismatch,
        JoinFailure,
    }


    public enum TrackSubscribeFailReason
    {
        InvalidServerResponse,
        NotTrackMetadataFound,
        UnsupportedTrackType,
        // ...
    }


    public class ParticipantTrackPermission
    {
        public string ParticipantIdentity;
        public bool AllTracksAllowed;
        public List<string> AllowedTrackSids;

        public ParticipantTrackPermission(
            string participantIdentity,
            bool allTracksAllowed,
            List<string> allowedTrackSids)
        {
            this.ParticipantIdentity = participantIdentity;
            this.AllTracksAllowed = allTracksAllowed;
            this.AllowedTrackSids = allowedTrackSids;
        }
    }
}