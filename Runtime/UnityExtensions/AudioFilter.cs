using UnityEngine;

namespace LiveKitUnity.Runtime
{
    public delegate void AReadEventHandler(float[] data, int channels, int sampleRate);

    public class AudioFilter : MonoBehaviour
    {
        public event AReadEventHandler onAudioRead;
        public bool sender;
        public bool loopback = false;
        private int m_sampleRate;

        void OnEnable()
        {
            OnAudioConfigurationChanged(false);
            AudioSettings.OnAudioConfigurationChanged += OnAudioConfigurationChanged;
        }

        void OnDisable()
        {
            AudioSettings.OnAudioConfigurationChanged -= OnAudioConfigurationChanged;
        }

        void OnAudioConfigurationChanged(bool deviceWasChanged)
        {
            m_sampleRate = AudioSettings.outputSampleRate;
        }

        /// <summary>
        /// </summary>
        /// <note>
        /// Call on the audio thread, not main thread.
        /// </note>
        /// <param name="data"></param>
        /// <param name="channels"></param>
        void OnAudioFilterRead(float[] data, int channels)
        {
            onAudioRead?.Invoke(data, channels, m_sampleRate);

            if (sender && !loopback)
            {
                for (int i = 0; i < data.Length; i++)
                {
                    data[i] = 0;
                }
            }
        }
    }
}