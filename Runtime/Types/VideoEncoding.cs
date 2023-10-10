using Unity.WebRTC;

namespace LiveKitUnity.Runtime.Types
{
    using System;

    [Serializable]
    public class VideoEncoding : IComparable<VideoEncoding>
    {
        public int maxFramerate;
        public int maxBitrate;

        public VideoEncoding(int maxFramerate, int maxBitrate)
        {
            this.maxFramerate = maxFramerate;
            this.maxBitrate = maxBitrate;
        }

        public VideoEncoding CopyWith(int? maxFramerate = null, int? maxBitrate = null)
        {
            return new VideoEncoding(
                maxFramerate ?? this.maxFramerate,
                maxBitrate ?? this.maxBitrate
            );
        }

        public override string ToString()
        {
            return $"{GetType()}(maxFramerate: {maxFramerate}, maxBitrate: {maxBitrate})";
        }

        public override bool Equals(object obj)
        {
            return obj is VideoEncoding encoding &&
                   maxFramerate == encoding.maxFramerate &&
                   maxBitrate == encoding.maxBitrate;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(maxFramerate, maxBitrate);
        }

        public int CompareTo(VideoEncoding other)
        {
            // Compare by bitrates
            int result = maxBitrate.CompareTo(other.maxBitrate);

            // If bitrates are the same, compare by fps
            if (result == 0)
            {
                result = maxFramerate.CompareTo(other.maxFramerate);
            }

            return result;
        }
    }

    public static class VideoEncodingExt
    {
        public static RTCRtpEncodingParameters ToRTCRtpEncoding(this VideoEncoding encoding, string rid = null,
            double scaleResolutionDownBy = 1.0, int? numTemporalLayers = null)
        {
            return new RTCRtpEncodingParameters
            {
                rid = rid,
                scaleResolutionDownBy = scaleResolutionDownBy,
                maxFramerate = (uint?)encoding.maxFramerate,
                maxBitrate = (ulong?)encoding.maxBitrate,
                //scaleResolutionDownBy
                // numTemporalLayers = numTemporalLayers
            };
        }
    }
}