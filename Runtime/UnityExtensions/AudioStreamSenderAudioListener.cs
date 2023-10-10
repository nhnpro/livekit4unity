using System;
using Unity.Collections;
using Unity.WebRTC;
using UnityEngine;

namespace LiveKitUnity.Runtime
{
    public class AudioStreamSenderAudioListener : AudioStreamSenderBase
    {
        public AudioStreamTrack audioTrack;
        public AudioListener audioListener;
        public AudioFilter _audioCapturer;
        public bool LoopBack = false;

        public override AudioStreamTrack CreateTrack()
        {
            if (audioListener == null)
                throw new ArgumentNullException(nameof(audioListener));
            audioTrack = new AudioStreamTrack();
            _audioCapturer = audioListener.gameObject.GetOrAddComponent<AudioFilter>();
            // _audioCapturer.hideFlags = HideFlags.HideInInspector;
            _audioCapturer.onAudioRead -= SetData;
            _audioCapturer.onAudioRead += SetData;
            _audioCapturer.loopback = LoopBack;
            _audioCapturer.sender = true;
            return audioTrack;
        }

        public void SetLoopBackOn()
        {
            LoopBack = true;
            if (_audioCapturer != null)
                _audioCapturer.loopback = true;
        }
        
        public void SetLoopBackOff()
        {
            LoopBack = false;
            if (_audioCapturer != null)
                _audioCapturer.loopback = false;
        }

        public void SetLoopBack(bool value)
        {
            LoopBack = value;
            if (_audioCapturer != null)
                _audioCapturer.loopback = value;
        }
        
        public void SetData(float[] array, int channels, int sampleRate)
        {
            if (array == null || audioTrack == null)
                throw new ArgumentNullException("array or audioTrack is null");
            NativeArray<float> nativeArray = new NativeArray<float>(array, Allocator.Temp);
            audioTrack.SetData(ref nativeArray, channels, sampleRate);
            nativeArray.Dispose();
        }

        public virtual void Dispose()
        {
            GC.SuppressFinalize(this);
        }
        
        public override void OnTrackDispose()
        {
            base.OnTrackDispose();
            _audioCapturer.onAudioRead -= SetData;
        }


        ~AudioStreamSenderAudioListener()
        {
            Dispose();
        }
    }
}