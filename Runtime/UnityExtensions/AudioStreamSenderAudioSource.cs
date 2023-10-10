using System;
using Unity.Collections;
using Unity.WebRTC;
using UnityEngine;

namespace LiveKitUnity.Runtime
{
    public class AudioStreamSenderAudioSource : AudioStreamSenderBase
    {
        public AudioStreamTrack audioTrack;
        public AudioSource audioSource;
        public bool LoopBack = false;
        public AudioFilter _audioCapturer;

        public override AudioStreamTrack CreateTrack()
        {
            if (audioSource == null)
                throw new ArgumentNullException(nameof(audioSource));
            audioTrack = new AudioStreamTrack();
            _audioCapturer = audioSource.gameObject.GetOrAddComponent<AudioFilter>();
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


        public override async void Publish()
        {
            audioSource.loop = true;
            audioSource.playOnAwake = false;
            audioSource.Play();
            base.Publish();
        }

        public override void Unpublish()
        {
            _audioCapturer.onAudioRead -= SetData;
            audioSource.Stop();
            base.Unpublish();
        }

        public override void Mute()
        {
            audioSource.mute = true;
            base.Mute();
        }

        public override void Unmute()
        {
            audioSource.mute = false;
            base.Unmute();
        }

        public override void OnTrackDispose()
        {
            base.OnTrackDispose();
            _audioCapturer.onAudioRead -= SetData;
            audioSource.Stop();
        }

        public virtual void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        ~AudioStreamSenderAudioSource()
        {
            Dispose();
        }
    }
}