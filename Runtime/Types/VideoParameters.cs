namespace LiveKitUnity.Runtime.Types
{
    using System;
    using System.Collections.Generic;

    [Serializable]
    public class VideoParameters : IComparable<VideoParameters>
    {
        public string description;
        public VideoDimensions dimensions;
        public VideoEncoding encoding;

        public VideoParameters(string description, VideoDimensions dimensions, VideoEncoding encoding)
        {
            this.description = description;
            this.dimensions = dimensions;
            this.encoding = encoding;
        }

        public override bool Equals(object obj)
        {
            return obj is VideoParameters parameters &&
                   description == parameters.description &&
                   EqualityComparer<VideoDimensions>.Default.Equals(dimensions, parameters.dimensions) &&
                   EqualityComparer<VideoEncoding>.Default.Equals(encoding, parameters.encoding);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(description, dimensions, encoding);
        }

        public int CompareTo(VideoParameters other)
        {
            // Compare by dimension's area
            int result = dimensions.Area().CompareTo(other.dimensions.Area());

            // If dimensions have equal area, compare by encoding
            if (result == 0)
            {
                result = encoding.CompareTo(other.encoding);
            }

            return result;
        }

        public Dictionary<string, object> ToMediaConstraintsMap()
        {
            return new Dictionary<string, object>
            {
                { "width", dimensions.width },
                { "height", dimensions.height },
                { "frameRate", encoding.maxFramerate }
            };
        }
    }

    public static class VideoParametersPresets
    {
        // 16:9 default
        public static List<VideoParameters> DefaultSimulcast169 = new List<VideoParameters>
        {
            H180169,
            H360169
        };

        // 4:3 default
        public static List<VideoParameters> DefaultSimulcast43 = new List<VideoParameters>
        {
            H18043,
            H36043
        };

        // all 16:9 presets
        public static List<VideoParameters> All169 = new List<VideoParameters>
        {
            H90169,
            H180169,
            H216169,
            H360169,
            H540169,
            H720169,
            H1080169,
            H1440169,
            H2160169
        };

        // all 4:3 presets
        public static List<VideoParameters> All43 = new List<VideoParameters>
        {
            H12043,
            H18043,
            H24043,
            H36043,
            H48043,
            H54043,
            H72043,
            H108043,
            H144043
        };

        // all screen share presets
        public static List<VideoParameters> AllScreenShare = new List<VideoParameters>
        {
            ScreenShareH360FPS3,
            ScreenShareH720FPS5,
            ScreenShareH720FPS15,
            ScreenShareH1080FPS15,
            ScreenShareH1080FPS30
        };

        // 16:9 Presets
        public static readonly VideoParameters H90169 = new VideoParameters(
            "h90_169",
            VideoDimensionsPresets.H90169,
            new VideoEncoding(15, 90 * 1000)
        );

        public static readonly VideoParameters H180169 = new VideoParameters(
            "h180_169",
            VideoDimensionsPresets.H180169,
            new VideoEncoding(15, 160 * 1000)
        );

        public static readonly VideoParameters H216169 = new VideoParameters(
            "h216_169",
            VideoDimensionsPresets.H216169,
            new VideoEncoding(15, 180 * 1000)
        );

        public static readonly VideoParameters H360169 = new VideoParameters(
            "h360_169",
            VideoDimensionsPresets.H360169,
            new VideoEncoding(20, 450 * 1000)
        );

        public static readonly VideoParameters H540169 = new VideoParameters(
            "h540_169",
            VideoDimensionsPresets.H540169,
            new VideoEncoding(25, 800 * 1000)
        );

        public static readonly VideoParameters H720169 = new VideoParameters(
            "h720_169",
            VideoDimensionsPresets.H720169,
            new VideoEncoding(30, 1700 * 1000)
        );

        public static readonly VideoParameters H1080169 = new VideoParameters(
            "h1080_169",
            VideoDimensionsPresets.H1080169,
            new VideoEncoding(30, 3000 * 1000)
        );

        public static readonly VideoParameters H1440169 = new VideoParameters(
            "h1440_169",
            VideoDimensionsPresets.H1440169,
            new VideoEncoding(30, 5000 * 1000)
        );

        public static readonly VideoParameters H2160169 = new VideoParameters(
            "h2160_169",
            VideoDimensionsPresets.H2160169,
            new VideoEncoding(30, 8000 * 1000)
        );

        // 4:3 presets
        public static readonly VideoParameters H12043 = new VideoParameters(
            "h120_43",
            VideoDimensionsPresets.H12043,
            new VideoEncoding(15, 70 * 1000)
        );

        public static readonly VideoParameters H18043 = new VideoParameters(
            "h180_43",
            VideoDimensionsPresets.H18043,
            new VideoEncoding(15, 125 * 1000)
        );

        public static readonly VideoParameters H24043 = new VideoParameters(
            "h240_43",
            VideoDimensionsPresets.H24043,
            new VideoEncoding(15, 140 * 1000)
        );

        public static readonly VideoParameters H36043 = new VideoParameters(
            "h360_43",
            VideoDimensionsPresets.H36043,
            new VideoEncoding(20, 330 * 1000)
        );

        public static readonly VideoParameters H48043 = new VideoParameters(
            "h480_43",
            VideoDimensionsPresets.H48043,
            new VideoEncoding(20, 500 * 1000)
        );

        public static readonly VideoParameters H54043 = new VideoParameters(
            "h540_43",
            VideoDimensionsPresets.H54043,
            new VideoEncoding(25, 600 * 1000)
        );

        public static readonly VideoParameters H72043 = new VideoParameters(
            "h720_43",
            VideoDimensionsPresets.H72043,
            new VideoEncoding(30, 1300 * 1000)
        );

        public static readonly VideoParameters H108043 = new VideoParameters(
            "h1080_43",
            VideoDimensionsPresets.H108043,
            new VideoEncoding(30, 2300 * 1000)
        );

        public static readonly VideoParameters H144043 = new VideoParameters(
            "h1440_43",
            VideoDimensionsPresets.H144043,
            new VideoEncoding(30, 3800 * 1000)
        );

        // Screen share
        public static readonly VideoParameters ScreenShareH360FPS3 = new VideoParameters(
            "screenShareH360FPS3",
            VideoDimensionsPresets.H360169,
            new VideoEncoding(3, 200 * 1000)
        );

        public static readonly VideoParameters ScreenShareH720FPS5 = new VideoParameters(
            "screenShareH720FPS5",
            VideoDimensionsPresets.H720169,
            new VideoEncoding(5, 400 * 1000)
        );

        public static readonly VideoParameters ScreenShareH720FPS15 = new VideoParameters(
            "screenShareH720FPS15",
            VideoDimensionsPresets.H720169,
            new VideoEncoding(15, 1500 * 1000)
        );

        public static readonly VideoParameters ScreenShareH1080FPS15 = new VideoParameters(
            "screenShareH1080FPS15",
            VideoDimensionsPresets.H1080169,
            new VideoEncoding(15, 2500 * 1000)
        );

        public static readonly VideoParameters ScreenShareH1080FPS30 = new VideoParameters(
            "screenShareH1080FPS30",
            VideoDimensionsPresets.H1080169,
            new VideoEncoding(30, 4000 * 1000)
        );

        public static readonly VideoParameters ScreenShareH1440FPS30 = new VideoParameters(
            "screenShareH1440FPS30",
            VideoDimensionsPresets.H1440169,
            new VideoEncoding(30, 6000 * 1000)
        );

        public static readonly VideoParameters ScreenShareH2160FPS30 = new VideoParameters(
            "screenShareH2160FPS30",
            VideoDimensionsPresets.H2160169,
            new VideoEncoding(30, 8000 * 1000)
        );
    }
}