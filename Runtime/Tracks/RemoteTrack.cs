//using System.Threading.Tasks;

using Cysharp.Threading.Tasks;
using LiveKit.Proto;
using LiveKitUnity.Runtime.Participants;
using Unity.WebRTC;

namespace LiveKitUnity.Runtime.Tracks
{
    public class RemoteTrack : Track
    {
        public RemoteTrack(TrackType kind, TrackSource source, MediaStreamTrack mediaStreamTrack,
            RTCRtpReceiver receiver = null)
            : base(kind, source, mediaStreamTrack, null, receiver)
        {
        }

        public override async UniTask<bool> Start(Participant participant)
        {
            var didStart = await base.Start(participant);
            if (didStart)
            {
                await Enable();
            }

            return didStart;
        }

        public override async UniTask<bool> Stop()
        {
            var didStop = await base.Stop();
            if (didStop)
            {
                await Disable();
            }

            return didStop;
        }
    }
}