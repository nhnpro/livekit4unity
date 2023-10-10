using System;
using Unity.Collections;
using Unity.WebRTC;

namespace LiveKitUnity.Runtime
{
    public class AudioStreamSenderAPI : AudioStreamSenderBase
    {
        public AudioStreamTrack audioTrack;

        public override AudioStreamTrack CreateTrack()
        {
            audioTrack = new AudioStreamTrack();
            return audioTrack;
        }

        public void SetData(float[] array, int channels, int sampleRate)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            NativeArray<float> nativeArray = new NativeArray<float>(array, Allocator.Temp);
            audioTrack?.SetData(ref nativeArray, channels, sampleRate);
            nativeArray.Dispose();
        }

        public virtual void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        ~AudioStreamSenderAPI()
        {
            Dispose();
        }
    }
}