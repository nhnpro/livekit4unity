using System;
using System.Collections.Generic;
using LiveKitUnity.Runtime.Tracks;
using LiveKitUnity.Runtime.Types;
using Unity.WebRTC;

namespace LiveKitUnity.Runtime.Types
{
    public class TrackOption<E, T>
    {
        public E Enabled { get; }
        public T Track { get; }

        public TrackOption(E enabled, T track)
        {
            Enabled = enabled;
            Track = track;
        }
    }

    public class FastConnectOptions
    {
        public TrackOption<bool?, LocalAudioTrack> Microphone { get; }
        public TrackOption<bool?, LocalVideoTrack> Camera { get; }
        public TrackOption<bool?, LocalVideoTrack> Screen { get; }

        public FastConnectOptions(
            TrackOption<bool?, LocalAudioTrack> microphone = null,
            TrackOption<bool?, LocalVideoTrack> camera = null,
            TrackOption<bool?, LocalVideoTrack> screen = null)
        {
            Microphone = microphone ?? new TrackOption<bool?, LocalAudioTrack>(false, null);
            Camera = camera ?? new TrackOption<bool?, LocalVideoTrack>(false, null);
            Screen = screen ?? new TrackOption<bool?, LocalVideoTrack>(false, null);
        }
    }

    public class ConnectOptions
    {
        public bool AutoSubscribe { get; }
        public RTCConfiguration RtcConfiguration { get; }
        public ProtocolVersion ProtocolVersion { get; }
        public Timeouts Timeouts { get; }

        public ConnectOptions(
            bool autoSubscribe = true,
            RTCConfiguration? rtcConfiguration = null,
            ProtocolVersion protocolVersion = ProtocolVersion.V9,
            Timeouts timeouts = null)
        {
            AutoSubscribe = autoSubscribe;
            RtcConfiguration = rtcConfiguration ?? new RTCConfiguration();
            ProtocolVersion = protocolVersion;
            Timeouts = timeouts ?? Timeouts.DefaultTimeouts;
        }
    }

    public class E2EEOptions
    {
        //TODO
        public EncryptionType encryptionType;
        public string KeyProvider { get; set; }
    }

    public class RoomOptions
    {
        public CameraCaptureOptions DefaultCameraCaptureOptions { get; }
        public ScreenShareCaptureOptions DefaultScreenShareCaptureOptions { get; }
        public AudioCaptureOptions DefaultAudioCaptureOptions { get; }
        public VideoPublishOptions DefaultVideoPublishOptions { get; }
        public AudioPublishOptions DefaultAudioPublishOptions { get; }
        public AudioOutputOptions DefaultAudioOutputOptions { get; }
        public bool AdaptiveStream { get; }
        public bool Dynacast { get; }
        public bool StopLocalTrackOnUnpublish { get; }
        public E2EEOptions E2eeOptions { get; }

        public RoomOptions(
            CameraCaptureOptions defaultCameraCaptureOptions = null,
            ScreenShareCaptureOptions defaultScreenShareCaptureOptions = null,
            AudioCaptureOptions defaultAudioCaptureOptions = null,
            VideoPublishOptions defaultVideoPublishOptions = null,
            AudioPublishOptions defaultAudioPublishOptions = null,
            AudioOutputOptions defaultAudioOutputOptions = null,
            bool adaptiveStream = false,
            bool dynacast = false,
            bool stopLocalTrackOnUnpublish = true,
            E2EEOptions e2eeOptions = null)
        {
            DefaultCameraCaptureOptions = defaultCameraCaptureOptions ?? new CameraCaptureOptions();
            DefaultScreenShareCaptureOptions = defaultScreenShareCaptureOptions ?? new ScreenShareCaptureOptions();
            DefaultAudioCaptureOptions = defaultAudioCaptureOptions ?? new AudioCaptureOptions();
            DefaultVideoPublishOptions = defaultVideoPublishOptions ?? new VideoPublishOptions();
            DefaultAudioPublishOptions = defaultAudioPublishOptions ?? new AudioPublishOptions();
            DefaultAudioOutputOptions = defaultAudioOutputOptions ?? new AudioOutputOptions();
            AdaptiveStream = adaptiveStream;
            Dynacast = dynacast;
            StopLocalTrackOnUnpublish = stopLocalTrackOnUnpublish;
            E2eeOptions = e2eeOptions;
        }

        public RoomOptions CopyWith(
            CameraCaptureOptions defaultCameraCaptureOptions = null,
            ScreenShareCaptureOptions defaultScreenShareCaptureOptions = null,
            AudioCaptureOptions defaultAudioCaptureOptions = null,
            VideoPublishOptions defaultVideoPublishOptions = null,
            AudioPublishOptions defaultAudioPublishOptions = null,
            AudioOutputOptions defaultAudioOutputOptions = null,
            bool? adaptiveStream = null,
            bool? dynacast = null,
            bool? stopLocalTrackOnUnpublish = null,
            E2EEOptions e2eeOptions = null)
        {
            return new RoomOptions(
                defaultCameraCaptureOptions ?? DefaultCameraCaptureOptions,
                defaultScreenShareCaptureOptions ?? DefaultScreenShareCaptureOptions,
                defaultAudioCaptureOptions ?? DefaultAudioCaptureOptions,
                defaultVideoPublishOptions ?? DefaultVideoPublishOptions,
                defaultAudioPublishOptions ?? DefaultAudioPublishOptions,
                defaultAudioOutputOptions ?? DefaultAudioOutputOptions,
                adaptiveStream ?? AdaptiveStream,
                dynacast ?? Dynacast,
                stopLocalTrackOnUnpublish ?? StopLocalTrackOnUnpublish,
                e2eeOptions ?? E2eeOptions);
        }
    }

    public class BackupVideoCodec
    {
        public string Codec { get; set; } = "vp8";
        public VideoEncoding? Encoding { get; set; }
        public bool Simulcast { get; set; } = true;
    }

    public class VideoPublishOptions
    {
        public const string DefaultCameraName = "camera";
        public const string DefaultScreenShareName = "screenshare";

        public string VideoCodec { get; set; } = "H264";
        public VideoEncoding? VideoEncoding { get; set; }
        public bool Simulcast { get; set; } = true;
        public string? Name { get; set; }
        public List<VideoParameters> VideoSimulcastLayers { get; set; } = new List<VideoParameters>();
        public List<VideoParameters> ScreenShareSimulcastLayers { get; set; } = new List<VideoParameters>();
        public string? ScalabilityMode { get; set; }
        public BackupVideoCodec? BackupCodec { get; set; }

        public VideoPublishOptions()
        {
        }

        public VideoPublishOptions(
            VideoEncoding? videoEncoding = null,
            bool? simulcast = null,
            List<VideoParameters>? videoSimulcastLayers = null,
            List<VideoParameters>? screenShareSimulcastLayers = null,
            string? videoCodec = null,
            BackupVideoCodec? backupCodec = null,
            string? scalabilityMode = null,
            string? name = null)
        {
            VideoEncoding = videoEncoding;
            Simulcast = simulcast ?? true;
            VideoSimulcastLayers = videoSimulcastLayers ?? new List<VideoParameters>();
            ScreenShareSimulcastLayers = screenShareSimulcastLayers ?? new List<VideoParameters>();
            VideoCodec = videoCodec ?? "H264";
            BackupCodec = backupCodec;
            ScalabilityMode = scalabilityMode;
            Name = name;
        }

        public VideoPublishOptions CopyWith(
            VideoEncoding? videoEncoding = null,
            bool? simulcast = null,
            List<VideoParameters>? videoSimulcastLayers = null,
            List<VideoParameters>? screenShareSimulcastLayers = null,
            string? videoCodec = null,
            BackupVideoCodec? backupCodec = null,
            string? scalabilityMode = null)
        {
            return new VideoPublishOptions(
                videoEncoding,
                simulcast ?? Simulcast,
                videoSimulcastLayers ?? VideoSimulcastLayers,
                screenShareSimulcastLayers ?? ScreenShareSimulcastLayers,
                videoCodec ?? VideoCodec,
                backupCodec ?? BackupCodec,
                scalabilityMode ?? ScalabilityMode);
        }

        public override string ToString()
        {
            return $"{GetType().Name}(videoEncoding: {VideoEncoding}, simulcast: {Simulcast})";
        }
    }

    public static class AudioPreset
    {
        public const int Telephone = 12000;
        public const int Speech = 20000;
        public const int Music = 32000;
        public const int CD = 44100;
        public const int MusicStereo = 48000;
        public const int MusicHighQuality = 64000;
        public const int MusicHighQualityStereo = 96000;
    }

    public class AudioPublishOptions
    {
        public const string DefaultMicrophoneName = "microphone";

        public bool Dtx { get; set; } = true;
        public int AudioBitrate { get; set; } = AudioPreset.MusicStereo;

        [Obsolete("Mic indicator will always turn off now when muted.")]
        public bool StopMicTrackOnMute { get; set; } = true;

        public string? Name { get; set; }

        public AudioPublishOptions(bool dtx = true, int audioBitrate = AudioPreset.MusicStereo,
            bool stopMicTrackOnMute = true, string? name = null)
        {
            Dtx = dtx;
            AudioBitrate = audioBitrate;
            StopMicTrackOnMute = stopMicTrackOnMute;
            Name = name;
        }

        public override string ToString()
        {
            return $"{GetType().Name}(dtx: {Dtx})";
        }
    }

    public static class Constants
    {
        public static readonly List<string> BackupCodecs = new() { "vp8", "h264" };
        public static readonly string[] VideoCodecs = { "vp8", "h264", "vp9", "av1" };

        public static bool IsBackupCodec(string codec)
        {
            return BackupCodecs.Contains(codec.ToLower());
        }
    }

    public abstract class LocalTrackOptions
    {
        public abstract Dictionary<string, object> ToMediaConstraintsMap();
    }

    public class VideoCaptureOptions : LocalTrackOptions
    {
        public VideoParameters Params { get; }
        public string? DeviceId { get; }
        public double? MaxFrameRate { get; }

        public VideoCaptureOptions(VideoParameters paramsValue = null, string? deviceId = null,
            double? maxFrameRate = null)
        {
            Params = paramsValue ?? VideoParametersPresets.H540169;
            DeviceId = deviceId;
            MaxFrameRate = maxFrameRate;
        }

        public override Dictionary<string, object> ToMediaConstraintsMap()
        {
            return Params.ToMediaConstraintsMap();
        }
    }


    public class AudioCaptureOptions : LocalTrackOptions
    {
        public string? DeviceId { get; }
        public bool NoiseSuppression { get; }
        public bool EchoCancellation { get; }
        public bool AutoGainControl { get; }
        public bool HighPassFilter { get; }
        public bool TypingNoiseDetection { get; }

        public AudioCaptureOptions(string? deviceId = null, bool noiseSuppression = true, bool echoCancellation = true,
            bool autoGainControl = true, bool highPassFilter = false, bool typingNoiseDetection = true)
        {
            DeviceId = deviceId;
            NoiseSuppression = noiseSuppression;
            EchoCancellation = echoCancellation;
            AutoGainControl = autoGainControl;
            HighPassFilter = highPassFilter;
            TypingNoiseDetection = typingNoiseDetection;
        }

        public override Dictionary<string, object> ToMediaConstraintsMap()
        {
            var constraints = new Dictionary<string, object>();

            if (DeviceId != null)
            {
                constraints["deviceId"] = DeviceId;
            }

            if (!string.IsNullOrEmpty(DeviceId))
            {
                constraints["optional"] = new List<Dictionary<string, object>>
                {
                    new Dictionary<string, object> { { "echoCancellation", EchoCancellation } },
                    new Dictionary<string, object> { { "googDAEchoCancellation", EchoCancellation } },
                    new Dictionary<string, object> { { "googEchoCancellation", EchoCancellation } },
                    new Dictionary<string, object> { { "googEchoCancellation2", EchoCancellation } },
                    new Dictionary<string, object> { { "noiseSuppression", NoiseSuppression } },
                    new Dictionary<string, object> { { "googNoiseSuppression", NoiseSuppression } },
                    new Dictionary<string, object> { { "googNoiseSuppression2", NoiseSuppression } },
                    new Dictionary<string, object> { { "googAutoGainControl", AutoGainControl } },
                    new Dictionary<string, object> { { "googHighpassFilter", HighPassFilter } },
                    new Dictionary<string, object> { { "googTypingNoiseDetection", TypingNoiseDetection } },
                };
            }

            return constraints;
        }

        public AudioCaptureOptions CopyWith(string? deviceId = null, bool? noiseSuppression = null,
            bool? echoCancellation = null, bool? autoGainControl = null, bool? highPassFilter = null,
            bool? typingNoiseDetection = null)
        {
            return new AudioCaptureOptions(
                deviceId ?? DeviceId,
                noiseSuppression ?? NoiseSuppression,
                echoCancellation ?? EchoCancellation,
                autoGainControl ?? AutoGainControl,
                highPassFilter ?? HighPassFilter,
                typingNoiseDetection ?? TypingNoiseDetection
            );
        }
    }

    public class AudioOutputOptions
    {
        public string? DeviceId { get; }
        public bool? SpeakerOn { get; }

        public AudioOutputOptions(string? deviceId = null, bool? speakerOn = null)
        {
            DeviceId = deviceId;
            SpeakerOn = speakerOn;
        }

        public AudioOutputOptions CopyWith(string? deviceId = null, bool? speakerOn = null)
        {
            return new AudioOutputOptions(deviceId ?? DeviceId, speakerOn ?? SpeakerOn);
        }
    }


    public enum CameraPosition
    {
        Front,
        Back
    }

    public static class CameraPositionExt
    {
        public static CameraPosition Switched(this CameraPosition cameraPosition)
        {
            return cameraPosition == CameraPosition.Front ? CameraPosition.Back : CameraPosition.Front;
        }
    }

    public class CameraCaptureOptions : VideoCaptureOptions
    {
        public CameraPosition CameraPosition { get; }

        public CameraCaptureOptions(CameraPosition cameraPosition = CameraPosition.Front, string deviceId = null,
            double? maxFrameRate = null, VideoParameters vparams = null)
            : base(vparams, deviceId, maxFrameRate)
        {
            CameraPosition = cameraPosition;
        }

        public CameraCaptureOptions(VideoCaptureOptions captureOptions)
            : this(CameraPosition.Front, captureOptions.DeviceId, captureOptions.MaxFrameRate, captureOptions.Params)
        {
        }

        public override Dictionary<string, object> ToMediaConstraintsMap()
        {
            var constraints = base.ToMediaConstraintsMap();

            if (DeviceId == null)
            {
                constraints["facingMode"] = CameraPosition == CameraPosition.Front ? "user" : "environment";
            }

            if (DeviceId != null)
            {
                constraints["deviceId"] = DeviceId;
            }

            if (MaxFrameRate != null)
            {
                constraints["frameRate"] = new Dictionary<string, object> { { "max", MaxFrameRate } };
            }

            return constraints;
        }

        public CameraCaptureOptions CopyWith(VideoParameters vparams = null, CameraPosition? cameraPosition = null,
            string deviceId = null, double? maxFrameRate = null)
        {
            return new CameraCaptureOptions(cameraPosition ?? CameraPosition, deviceId ?? DeviceId,
                maxFrameRate ?? MaxFrameRate, vparams ?? Params);
        }
    }

    public class ScreenShareCaptureOptions : VideoCaptureOptions
    {
        public bool UseiOSBroadcastExtension { get; }
        public bool CaptureScreenAudio { get; }
        public bool PreferCurrentTab { get; }
        public string SelfBrowserSurface { get; }

        public ScreenShareCaptureOptions(bool useiOSBroadcastExtension = false, bool captureScreenAudio = false,
            bool preferCurrentTab = false, string selfBrowserSurface = null, string sourceId = null,
            double? maxFrameRate = null, VideoParameters vparams = null)
            : base(vparams, sourceId, maxFrameRate)
        {
            UseiOSBroadcastExtension = useiOSBroadcastExtension;
            CaptureScreenAudio = captureScreenAudio;
            PreferCurrentTab = preferCurrentTab;
            SelfBrowserSurface = selfBrowserSurface;
        }

        public ScreenShareCaptureOptions(VideoCaptureOptions captureOptions)
            : this(false, false, false, null, captureOptions.DeviceId, captureOptions.MaxFrameRate,
                captureOptions.Params)
        {
        }

        public ScreenShareCaptureOptions CopyWith(bool captureScreenAudio = false, VideoParameters vparams = null,
            string sourceId = null, double? maxFrameRate = null, bool? preferCurrentTab = null,
            string selfBrowserSurface = null)
        {
            return new ScreenShareCaptureOptions(
                UseiOSBroadcastExtension,
                captureScreenAudio,
                preferCurrentTab ?? PreferCurrentTab,
                selfBrowserSurface ?? SelfBrowserSurface,
                sourceId ?? DeviceId,
                maxFrameRate ?? MaxFrameRate,
                vparams ?? Params
            );
        }

        public override Dictionary<string, object> ToMediaConstraintsMap()
        {
            var constraints = base.ToMediaConstraintsMap();

            if (UseiOSBroadcastExtension && LiveKitUtils.IsiOSPlatform())
            {
                constraints["deviceId"] = "broadcast";
            }

            if (LiveKitUtils.IsDesktopPlatform())
            {
                if (DeviceId != null)
                {
                    constraints["deviceId"] = new Dictionary<string, object> { { "exact", DeviceId } };
                }

                if (MaxFrameRate != 0.0)
                {
                    constraints["mandatory"] = new Dictionary<string, object> { { "frameRate", MaxFrameRate } };
                }
            }

            return constraints;
        }
    }
}