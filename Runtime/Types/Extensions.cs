using System;
using System.Linq;
using LiveKit.Proto;
using Unity.WebRTC;

namespace LiveKitUnity.Runtime.Types
{
    public static class DataPacketKindExtensions
    {
        public static string ObjectId(this object obj)
        {
            return $"{obj.GetType()}#{obj.GetHashCode()}";
        }

        public static string ToStringValue(this ProtocolVersion version)
        {
            return version switch
            {
                ProtocolVersion.V2 => "2",
                ProtocolVersion.V3 => "3",
                ProtocolVersion.V4 => "4",
                ProtocolVersion.V5 => "5",
                ProtocolVersion.V6 => "6",
                ProtocolVersion.V7 => "7",
                ProtocolVersion.V8 => "8",
                ProtocolVersion.V9 => "9",
                _ => throw new ArgumentOutOfRangeException(nameof(version))
            };
        }

        public static Reliability ToSDKType(this DataPacket.Types.Kind kind)
        {
            return kind switch
            {
                DataPacket.Types.Kind.Reliable => Reliability.Reliable,
                DataPacket.Types.Kind.Lossy => Reliability.Lossy,
                _ => throw new ArgumentOutOfRangeException(nameof(kind))
            };
        }

        public static DataPacket.Types.Kind ToPBType(this Reliability reliability)
        {
            return reliability switch
            {
                Reliability.Reliable => DataPacket.Types.Kind.Reliable,
                Reliability.Lossy => DataPacket.Types.Kind.Lossy,
                _ => throw new ArgumentOutOfRangeException(nameof(reliability))
            };
        }
    }


    public static class RTCExtensions
    {
        public static RTCIceServer ToSDKType(this ICEServer server)
        {
            return new RTCIceServer
            {
                urls = server.Urls.ToArray(),
                username = string.IsNullOrEmpty(server.Username) ? null : server.Username,
                credential = string.IsNullOrEmpty(server.Credential) ? null : server.Credential,
            };
        }

        public static DataChannelInfo ToLKInfoType(this RTCDataChannel dataChannel)
        {
            return new DataChannelInfo
            {
                Id = (uint)dataChannel.Id,
                Label = dataChannel.Label,
            };
        }

        public static bool IsConnected(this RTCPeerConnectionState state)
        {
            return state == RTCPeerConnectionState.Connected;
        }

        public static bool IsClosed(this RTCPeerConnectionState state)
        {
            return state == RTCPeerConnectionState.Closed;
        }

        public static bool IsDisconnectedOrFailed(this RTCPeerConnectionState state)
        {
            return state is RTCPeerConnectionState.Disconnected or RTCPeerConnectionState.Failed;
        }

        public static string ToStringValue(this RTCIceTransportPolicy policy)
        {
            switch (policy)
            {
                case RTCIceTransportPolicy.All:
                    return "all";
                case RTCIceTransportPolicy.Relay:
                    return "relay";
                default:
                    throw new ArgumentOutOfRangeException(nameof(policy), policy, null);
            }
        }

        public static RTCIceCandidate FromJson(string jsonString)
        {
            var x = LiveKitUtils.ConvertJson<RTCIceCandidateInit>(jsonString);
            return new RTCIceCandidate(x);
        }

        public static string ToJson(this RTCIceCandidate candidate)
        {
            var x = new RTCIceCandidateInit
            {
                candidate = candidate.Candidate,
                sdpMid = candidate.SdpMid,
                sdpMLineIndex = candidate.SdpMLineIndex,
            };
            return LiveKitUtils.ToJson(x);
        }
    }


    public static class RTCSessionDescriptionExt
    {
        public static SessionDescription ToPBType(this RTCSessionDescription description)
        {
            return new SessionDescription
            {
                Type = description.type.ToString().ToLower(),
                Sdp = description.sdp
            };
        }

        public static RTCSessionDescription ToSDKType(this SessionDescription description)
        {
            var r = RTCSdpType.Answer;
            var t = description.Type.ToLower();
            r = t switch
            {
                "offer" => RTCSdpType.Offer,
                "pranswer" => RTCSdpType.Pranswer,
                "rollback" => RTCSdpType.Rollback,
                "answer" => RTCSdpType.Answer,
                _ => r
            };
            return new RTCSessionDescription
            {
                sdp = description.Sdp,
                type = r
            };
        }
    }

    public static class ConnectionQualityExt
    {
        public static ConnectionQuality ToLKType(this LiveKit.Proto.ConnectionQuality quality)
        {
            return quality switch
            {
                LiveKit.Proto.ConnectionQuality.Poor => ConnectionQuality.Poor,
                LiveKit.Proto.ConnectionQuality.Good => ConnectionQuality.Good,
                LiveKit.Proto.ConnectionQuality.Excellent => ConnectionQuality.Excellent,
                _ => ConnectionQuality.Unknown
            };
        }

        public static LiveKit.Proto.ConnectionQuality ToPBType(this ConnectionQuality quality)
        {
            return quality switch
            {
                ConnectionQuality.Poor => LiveKit.Proto.ConnectionQuality.Poor,
                ConnectionQuality.Good => LiveKit.Proto.ConnectionQuality.Good,
                ConnectionQuality.Excellent => LiveKit.Proto.ConnectionQuality.Excellent,
                _ => LiveKit.Proto.ConnectionQuality.Good
            };
        }
    }

    public static class PBTrackSourceExt
    {
        public static TrackSource ToLKType(this LiveKit.Proto.TrackSource source)
        {
            return source switch
            {
                LiveKit.Proto.TrackSource.Unknown => TrackSource.Unknown,
                LiveKit.Proto.TrackSource.Camera =>TrackSource.Camera,
                LiveKit.Proto.TrackSource.Microphone =>TrackSource.Microphone,
                LiveKit.Proto.TrackSource.ScreenShare =>TrackSource.ScreenShareVideo,
                LiveKit.Proto.TrackSource.ScreenShareAudio => TrackSource.ScreenShareAudio,
                _ =>TrackSource.Unknown
            };
        }

        public static LiveKit.Proto.TrackSource ToPBType(this TrackSource source)
        {
            return source switch
            {
               TrackSource.Unknown => LiveKit.Proto.TrackSource.Unknown,
               TrackSource.Camera => LiveKit.Proto.TrackSource.Camera,
             TrackSource.Microphone => LiveKit.Proto.TrackSource.Microphone,
                TrackSource.ScreenShareVideo => LiveKit.Proto.TrackSource.ScreenShare,
         TrackSource.ScreenShareAudio => LiveKit.Proto.TrackSource.ScreenShareAudio,
                _ => LiveKit.Proto.TrackSource.Unknown
            };
        }
    }


    public static class PBStreamStateExt
    {
        public static StreamState ToLKType(this LiveKit.Proto.StreamState state)
        {
            return state switch
            {
                LiveKit.Proto.StreamState.Paused =>StreamState.Paused,
                LiveKit.Proto.StreamState.Active =>StreamState.Active,
                _ =>StreamState.Active
            };
        }
    }

    public static class VideoQualityExt
    {
        public static string ToRid(this VideoQuality quality)
        {
            return quality switch
            {
                VideoQuality.High => "f",
                VideoQuality.Medium => "h",
                VideoQuality.Low => "q",
                _ => null
            };
        }
    }

    public static class ParticipantTrackPermissionExt
    {
        public static TrackPermission ToPBType(this ParticipantTrackPermission permission)
        {
            var t = new TrackPermission
            {
                ParticipantIdentity = permission.ParticipantIdentity,
                AllTracks = permission.AllTracksAllowed,
            };
            t.TrackSids.AddRange(permission.AllowedTrackSids);
            return t;
        }
    }

    public static class EncryptionTypeExt
    {
        public static EncryptionType ToLkType(this Encryption.Types.Type encryptionType)
        {
            switch (encryptionType)
            {
                case Encryption.Types.Type.None:
                    return EncryptionType.None;
                case Encryption.Types.Type.Gcm:
                    return EncryptionType.Gcm;
                case Encryption.Types.Type.Custom:
                    return EncryptionType.Custom;
                default:
                    return EncryptionType.None;
            }
        }
    }

    public static class DisconnectReasonExt
    {
        public static DisconnectReason ToSDKType(this LiveKit.Proto.DisconnectReason reason)
        {
            return reason switch
            {
                LiveKit.Proto.DisconnectReason.UnknownReason => DisconnectReason.Unknown,
                LiveKit.Proto.DisconnectReason.ClientInitiated => DisconnectReason.ClientInitiated,
                LiveKit.Proto.DisconnectReason.DuplicateIdentity => DisconnectReason.DuplicateIdentity,
                LiveKit.Proto.DisconnectReason.ServerShutdown => DisconnectReason.ServerShutdown,
                LiveKit.Proto.DisconnectReason.ParticipantRemoved => DisconnectReason.ParticipantRemoved,
                LiveKit.Proto.DisconnectReason.RoomDeleted => DisconnectReason.RoomDeleted,
                LiveKit.Proto.DisconnectReason.StateMismatch => DisconnectReason.StateMismatch,
                LiveKit.Proto.DisconnectReason.JoinFailure => DisconnectReason.JoinFailure,
                _ => DisconnectReason.Unknown
            };
        }
    }
}