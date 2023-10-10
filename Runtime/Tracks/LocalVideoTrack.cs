using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
//using System.Threading.Tasks;
using LiveKit.Proto;
using LiveKitUnity.Runtime.Types;
using Unity.WebRTC;
using TrackSource = LiveKit.Proto.TrackSource;

namespace LiveKitUnity.Runtime.Tracks
{
    public class SimulcastTrackInfo
    {
        public string Codec { get; set; }
        public MediaStreamTrack MediaStreamTrack { get; set; }
        public RTCRtpSender Sender { get; set; }
        public List<RTCRtpEncodingParameters> Encodings { get; set; }

        public SimulcastTrackInfo(string codec, MediaStreamTrack mediaStreamTrack, RTCRtpSender sender
            , List<RTCRtpEncodingParameters> encodings = null)
        {
            Codec = codec;
            MediaStreamTrack = mediaStreamTrack;
            Sender = sender;
            Encodings = encodings ?? new List<RTCRtpEncodingParameters>();
        }
    }

    public class LocalVideoTrack : LocalTrack
    {
        private double _currentBitrate;
        public double CurrentBitrate => _currentBitrate;
        public Dictionary<string, VideoSenderStats> PrevStats { get; }

        private readonly Dictionary<string, double> _bitrateForLayers = new Dictionary<string, double>();

        public Dictionary<string, SimulcastTrackInfo> SimulcastCodecs { get; }
        public List<SubscribedCodec> SubscribedCodecs { get; }


        public LocalVideoTrack(TrackSource source, MediaStreamTrack mediaStreamTrack,
            VideoCaptureOptions options = null,
            RTCRtpSender sender = null) : base(TrackType.Video, source, mediaStreamTrack, sender)
        {
            this.CurrentOptions = options ?? new VideoCaptureOptions();
            SimulcastCodecs = new Dictionary<string, SimulcastTrackInfo>();
            SubscribedCodecs = new List<SubscribedCodec>();
            PrevStats = new Dictionary<string, VideoSenderStats>();
        }

        public async UniTask<IEnumerable<string>> SetPublishingCodecs(List<SubscribedCodec> eventDataSubscribedCodecs,
            LocalVideoTrack videoTrack)
        {
            throw new System.NotImplementedException();
        }

        public async UniTask UpdatePublishingLayers(LocalVideoTrack videoTrack,
            List<SubscribedQuality> eventDataSubscribedQualities)
        {
            throw new System.NotImplementedException();
        }


        public override LocalTrackOptions CurrentOptions { get; set; }
        

        public static async UniTask<LocalVideoTrack> CreateScreenShareTrackAsync(ScreenShareCaptureOptions options = null)
        {
            if (options == null)
            {
                options = new ScreenShareCaptureOptions();
            }

            // rtc.MediaStream stream = await LocalTrack.CreateStream(options);
            VideoStreamTrack videoTrack =
                VideoManager.Instance.GetVideoTracks(TrackSource.ScreenShare).FirstOrDefault();

            if (videoTrack == null)
            {
                // Handle the case where there is no video track in the stream.
                throw new Exception("No video track found in the stream.");
            }

            return new LocalVideoTrack(TrackSource.ScreenShare, videoTrack, options);
        }

        public static async UniTask<List<LocalTrack>> CreateScreenShareTracksWithAudioAsync(
            ScreenShareCaptureOptions options = null)
        {
            options = options == null ? new ScreenShareCaptureOptions(captureScreenAudio: true) : options.CopyWith(true);

            // rtc.MediaStream stream = await LocalTrack.CreateStream(options);
            VideoStreamTrack videoTrack =
                VideoManager.Instance.GetVideoTracks(TrackSource.ScreenShare).FirstOrDefault();
            List<LocalTrack> tracks = new List<LocalTrack>();

            if (videoTrack != null)
            {
                tracks.Add(new LocalVideoTrack(TrackSource.ScreenShare, videoTrack, options));
            }

            MediaStreamTrack audioTrack =
                AudioManager.Instance.GetAudioTracks(TrackSource.ScreenShare).FirstOrDefault();

            if (audioTrack != null)
            {
                tracks.Add(new LocalAudioTrack(TrackSource.ScreenShareAudio, audioTrack, new AudioCaptureOptions()));
            }

            return tracks;
        }

        
        public static LocalVideoTrack CreateVideoTrackAsync(TrackSource tsource
            , VideoStreamTrack track,
            CameraCaptureOptions options = null)
        {
            options ??= new CameraCaptureOptions();
            return new LocalVideoTrack(tsource, track, options);
        }
    }
}