//using System.Threading.Tasks;

using Cysharp.Threading.Tasks;
using LiveKit.Proto;
using LiveKitUnity.Runtime.Types;
using Unity.WebRTC;
using TrackSource = LiveKit.Proto.TrackSource;

namespace LiveKitUnity.Runtime.Tracks
{
    public class LocalAudioTrack : LocalTrack
    {
        public LocalAudioTrack(TrackSource source, MediaStreamTrack mediaStreamTrack
            , AudioCaptureOptions options = null, RTCRtpSender sender = null)
            : base(TrackType.Audio, source, mediaStreamTrack, sender)
        {
            this.CurrentOptions = options ?? new AudioCaptureOptions();
        }


        public override LocalTrackOptions CurrentOptions { get; set; }
       

        public static async UniTask<LocalAudioTrack> CreateAudioTrackAsync(TrackSource tsource, AudioStreamTrack track,
            AudioCaptureOptions options = null)
        {
            options ??= new AudioCaptureOptions();
            return new LocalAudioTrack(tsource, track, options);
        }

        /*public override async UniTask<bool> OnPublish()
        {
            var b = await base.OnPublish();
            if (b)
            {
                AudioManager.Instance.PublishAudioTrack(this.GetCid());
            }
            return b;
        }
        public override async UniTask<bool> OnUnpublish()
        {
            var b = await base.OnUnpublish();
            if (b)
            {
                AudioManager.Instance.UnpublishAudioTrack(this.GetCid());
            }

            return b;
        }*/
    }
}