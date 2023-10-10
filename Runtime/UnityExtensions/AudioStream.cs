using System.Collections.Generic;
using LiveKitUnity.Runtime.Debugging;
using LiveKitUnity.Runtime.Types;
using Unity.WebRTC;
using UnityEngine;

namespace LiveKitUnity.Runtime
{
    public class AudioStream
    {
        private RingBuffer _buffer;
        private short[] _tempBuffer;
        private uint _numChannels;
        private uint _sampleRate;

        private AudioStreamTrack _track;
        private RTCRtpTransceiver _receiverTransceiver;

        public AudioStream(AudioStreamTrack track, RTCRtpTransceiver transceiver)
        {
            _track = track;
            // _track.onReceived += onTrackReceived;
            _receiverTransceiver = transceiver;
        }

        private void onTrackReceived(float[] data, int channels, int samplerate)
        {
            PrintArray("onTrackReceived", data, true);
        }

        public void Play()
        {
            var audioSource = GameObject.Find("AudioSourceOther").GetComponent<AudioSource>();
            audioSource.SetTrack(_track);
            audioSource.loop = true;
            audioSource.Play();
            Debug.Log($"Play {_track.Id} {audioSource.isPlaying}");

            // var filter = audioSource.gameObject.AddComponent<AudioFilter>();
            // filter.AudioRead += OnAudioRead;

            /*void OnTransformedFrame(RTCTransformEvent e)
            {
                OnAudioStreamEvent(_receiver, e);
            }

            _receiver.Transform = new RTCRtpScriptTransform(TrackKind.Audio, OnTransformedFrame);*/
        }

        private byte[] _tempBufferBytes;

        private void OnAudioRead(float[] data, int channels, int sampleRate)
        {
            /*
          if (_buffer == null || channels != _numChannels || sampleRate != _sampleRate ||
              data.Length != _tempBuffer.Length)
          {
              int size = (int)(channels * sampleRate * 0.2);
              _buffer = new RingBuffer(size * sizeof(short));
              _tempBuffer = new short[data.Length];
              _numChannels = (uint)channels;
              _sampleRate = (uint)sampleRate;
          }

          float S16ToFloat(short v)
          {
              const float kMaxInt16Inverse = 1f / short.MaxValue;
              const float kMinInt16Inverse = 1f / short.MinValue;
              return v * (v > 0 ? kMaxInt16Inverse : -kMinInt16Inverse);
          }


          // "Send" the data to Unity
          var temp = MemoryMarshal.Cast<short, byte>
              (_tempBuffer.AsSpan().Slice(0, data.Length));
          int read = _buffer.Read(temp);


          Array.Clear(data, 0, data.Length);

          for (int i = 0; i < data.Length; i++)
          {
              data[i] = S16ToFloat(_tempBuffer[i]);
          }*/

            this.Log($"OnAudioRead {data.Length} {channels} {sampleRate}");
            PrintArray("OnAudioRead", data, true);
        }


        // private void OnAudioStreamEvent(RTCRtpReceiver r, RTCTransformEvent e)
        // {
        // if (_numChannels == 0)
        // return;

        /*var audioFrame = e.Frame;
        var array = audioFrame.GetData();
        audioFrame.SetData(array);
        PrintArray("StreamEvent", array);

        // var uFrame = _resampler.RemixAndResample(frame, _numChannels, _sampleRate);
        // var data = new Span<byte>(uFrame.Data.ToPointer(), uFrame.Length);
        _buffer.Write(array);*/
        // }


        public void PrintArray<T>(string prefix, IEnumerable<T> data, bool skipZero = false)
        {
            var str = $"{prefix}[";
            var count = 0;
            foreach (var item in data)
            {
                if (!item.Equals(default(T)) || !skipZero)
                {
                    str += item + ",";
                    count++;
                }
            }

            if (count == 0)
            {
                str += "All Zero";
            }

            str = str.TrimEnd(',') + "]";
            Debug.Log(str);
        }
    }
}