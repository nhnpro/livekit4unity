using System;
using Cysharp.Threading.Tasks;
//using System.Threading.Tasks;
using LiveKit.Proto;
using LiveKitUnity.Runtime.Events;
using LiveKitUnity.Runtime.Participants;
using Unity.WebRTC;

namespace LiveKitUnity.Runtime.Tracks
{
    public class RemoteAudioTrack : RemoteTrack
    {
        public RemoteAudioTrack(TrackSource source, MediaStreamTrack track, RTCRtpReceiver receiver = null)
            : base(TrackType.Audio, source, track,  receiver)
        {
        }

        public override async UniTask<bool> Start(Participant participant)
        {
            var didStart = await base.Start(participant);
            if (didStart)
            {
                try
                {
                    // web support
                    await AudioManager.Instance.StartAudio(currentParticipant,this. GetCid(), Source, MediaStreamTrack, ReceiverRaw);
                }
                catch (Exception ex)
                {
                    events.Emit(new AudioPlaybackFailed(track: this));
                }
            }

            return didStart;
        }

        public override async UniTask<bool> Stop()
        {
            var didStop = await base.Stop();
            if (didStop)
            {
                try
                {
                    // web support
                    await AudioManager.Instance.StopAudio(GetCid());
                }
                catch (Exception ex)
                {
                    events.Emit(new AudioPlaybackFailed(track: this));
                }
            }

            return didStop;
        }
    }
}