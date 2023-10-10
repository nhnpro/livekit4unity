using System;
using System.Collections.Generic;
using LiveKit.Proto;
using Unity.WebRTC;


namespace LiveKitUnity.Runtime.Tracks
{
    public class RemoteVideoTrack : RemoteTrack
    {
        public RemoteVideoTrack(TrackSource source, MediaStreamTrack track
            , RTCRtpReceiver receiver = null) : base(TrackType.Video, source, track, receiver)
        {
        }

        public List<object> ViewKeys { get; set; }
        public Action<string> onVideoViewBuild;
    }
}