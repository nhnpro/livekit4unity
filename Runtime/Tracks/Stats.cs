using System.Collections.Generic;
using LiveKit.Proto;

namespace LiveKitUnity.Runtime.Tracks
{
    public class CodecStats
    {
        public string MimeType { get; set; }
        public float PayloadType { get; set; }
        public float Channels { get; set; }
        public float ClockRate { get; set; }
    }

    public class SenderStats : CodecStats
    {
        public float PacketsSent { get; set; }
        public float BytesSent { get; set; }
        public float Jitter { get; set; }
        public float PacketsLost { get; set; }
        public float RoundTripTime { get; set; }
        public string StreamId { get; set; }
        public string EncoderImplementation { get; set; }
        public float Timestamp { get; set; }

        public SenderStats(string streamId, float timestamp)
        {
            StreamId = streamId;
            Timestamp = timestamp;
        }
    }

    public class AudioSenderStats : SenderStats
    {
        public TrackType Type { get; set; }

        public AudioSenderStats(string streamId, float timestamp) : base(streamId, timestamp)
        {
            Type = TrackType.Audio;
        }
    }

    public class VideoSenderStats : SenderStats
    {
        public TrackType Type { get; set; }
        public float FirCount { get; set; }
        public float PliCount { get; set; }
        public float NackCount { get; set; }
        public string Rid { get; set; }
        public float FrameWidth { get; set; }
        public float FrameHeight { get; set; }
        public float FramesSent { get; set; }
        public float FramesPerSecond { get; set; }
        public string QualityLimitationReason { get; set; }
        public float QualityLimitationResolutionChanges { get; set; }
        public float RetransmittedPacketsSent { get; set; }

        public VideoSenderStats(string streamId, float timestamp) : base(streamId, timestamp)
        {
            Type = TrackType.Video;
        }
    }

    public class ReceiverStats : CodecStats
    {
        public float JitterBufferDelay { get; set; }
        public float PacketsLost { get; set; }
        public float PacketsReceived { get; set; }
        public float BytesReceived { get; set; }
        public string StreamId { get; set; }
        public float Jitter { get; set; }
        public float Timestamp { get; set; }

        public ReceiverStats(string streamId, float timestamp)
        {
            StreamId = streamId;
            Timestamp = timestamp;
        }
    }

    public class AudioReceiverStats : ReceiverStats
    {
        public TrackType Type { get; set; }
        public float ConcealedSamples { get; set; }
        public float ConcealmentEvents { get; set; }
        public float SilentConcealedSamples { get; set; }
        public float SilentConcealmentEvents { get; set; }
        public float TotalAudioEnergy { get; set; }
        public float TotalSamplesDuration { get; set; }

        public AudioReceiverStats(string streamId, float timestamp) : base(streamId, timestamp)
        {
            Type = TrackType.Audio;
        }
    }

    public class VideoReceiverStats : ReceiverStats
    {
        public TrackType Type { get; set; }
        public float FramesDecoded { get; set; }
        public float FramesDropped { get; set; }
        public float FramesReceived { get; set; }
        public float FramesPerSecond { get; set; }
        public float FrameWidth { get; set; }
        public float FrameHeight { get; set; }
        public float FirCount { get; set; }
        public float PliCount { get; set; }
        public float NackCount { get; set; }
        public string DecoderImplementation { get; set; }

        public VideoReceiverStats(string streamId, float timestamp) : base(streamId, timestamp)
        {
            Type = TrackType.Video;
        }
    }

    public static class StatsUtils
    {
        public static float ComputeBitrateForSenderStats(SenderStats currentStats, SenderStats prevStats)
        {
            if (prevStats == null)
            {
                return 0;
            }

            float? bytesNow = currentStats.BytesSent;
            float? bytesPrev = prevStats.BytesSent;

            if (bytesNow == null || bytesPrev == null)
            {
                return 0;
            }

            return (float)(((bytesNow - bytesPrev) * 8) / (currentStats.Timestamp - prevStats.Timestamp));
        }

        public static float ComputeBitrateForReceiverStats(ReceiverStats currentStats, ReceiverStats prevStats)
        {
            if (prevStats == null)
            {
                return 0;
            }

            float? bytesNow = currentStats.BytesReceived;
            float? bytesPrev = prevStats.BytesReceived;

            if (bytesNow == null || bytesPrev == null)
            {
                return 0;
            }

            return (float)(((bytesNow - bytesPrev) * 8) / (currentStats.Timestamp - prevStats.Timestamp));
        }

        public static float? GetNumValFromReport(Dictionary<string, object> values, string key)
        {
            if (values.ContainsKey(key))
            {
                if (values[key] is string stringValue && float.TryParse(stringValue, out float result))
                {
                    return result;
                }
                else if (values[key] is float floatValue)
                {
                    return floatValue;
                }
            }

            return null;
        }

        public static string GetStringValFromReport(Dictionary<string, object> values, string key)
        {
            if (values.ContainsKey(key) && values[key] is string stringValue)
            {
                return stringValue;
            }

            return null;
        }
    }
}