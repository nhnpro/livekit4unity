namespace LiveKitUnity.Runtime.Types
{
    using System;
    using UnityEngine;

    [Serializable]
    public class VideoDimensions
    {
        public int width;
        public int height;

        public VideoDimensions(int width, int height)
        {
            this.width = width;
            this.height = height;
        }

        public override string ToString()
        {
            return $"{GetType()}({width}x{height})";
        }

        public VideoDimensions CopyWith(int? width = null, int? height = null)
        {
            return new VideoDimensions(width ?? this.width, height ?? this.height);
        }
    }

    public static class VideoDimensionsHelpers
    {
        public const double Aspect169 = 16.0 / 9.0;
        public const double Aspect43 = 4.0 / 3.0;

        public static double Aspect(this VideoDimensions dimensions)
        {
            return dimensions.width > dimensions.height
                ? (double)dimensions.width / dimensions.height
                : (double)dimensions.height / dimensions.width;
        }

        public static int Max(this VideoDimensions dimensions)
        {
            return Mathf.Max(dimensions.width, dimensions.height);
        }

        public static int Min(this VideoDimensions dimensions)
        {
            return Mathf.Min(dimensions.width, dimensions.height);
        }

        public static int Area(this VideoDimensions dimensions)
        {
            return dimensions.width * dimensions.height;
        }
    }

    public static class VideoDimensionsPresets
    {
        public static readonly VideoDimensions H90169 = new VideoDimensions(160, 90);
        public static readonly VideoDimensions H180169 = new VideoDimensions(320, 180);
        public static readonly VideoDimensions H216169 = new VideoDimensions(384, 216);
        public static readonly VideoDimensions H360169 = new VideoDimensions(640, 360);
        public static readonly VideoDimensions H540169 = new VideoDimensions(960, 540);
        public static readonly VideoDimensions H720169 = new VideoDimensions(1280, 720);
        public static readonly VideoDimensions H1080169 = new VideoDimensions(1920, 1080);
        public static readonly VideoDimensions H1440169 = new VideoDimensions(2560, 1440);
        public static readonly VideoDimensions H2160169 = new VideoDimensions(3840, 2160);

        public static readonly VideoDimensions H12043 = new VideoDimensions(160, 120);
        public static readonly VideoDimensions H18043 = new VideoDimensions(240, 180);
        public static readonly VideoDimensions H24043 = new VideoDimensions(320, 240);
        public static readonly VideoDimensions H36043 = new VideoDimensions(480, 360);
        public static readonly VideoDimensions H48043 = new VideoDimensions(640, 480);
        public static readonly VideoDimensions H54043 = new VideoDimensions(720, 540);
        public static readonly VideoDimensions H72043 = new VideoDimensions(960, 720);
        public static readonly VideoDimensions H108043 = new VideoDimensions(1440, 1080);
        public static readonly VideoDimensions H144043 = new VideoDimensions(1920, 1440);
    }
}