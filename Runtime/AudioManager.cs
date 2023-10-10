using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
// //using System.Threading.Tasks;
using LiveKitUnity.Runtime.Debugging;
using LiveKitUnity.Runtime.Participants;
using LiveKitUnity.Runtime.Tracks;
using LiveKitUnity.Runtime.Types;
using Unity.WebRTC;
using UnityEngine;
using Object = UnityEngine.Object;
using TrackSource = LiveKit.Proto.TrackSource;

namespace LiveKitUnity.Runtime
{
    public class AudioManager
    {
        private static AudioManager _instance;
        public static AudioManager Instance => _instance ??= new AudioManager();


        public async UniTask SetSpeakerOn(bool speakerOn)
        {
            this.LogWarning($"[AudioManager] Set Speaker On {speakerOn} Not Implemented Yet");
            /*AudioSettings.SetConfiguration(AudioSettings.GetConfiguration() with
            {
                speakerMode = speakerOn ? AudioSpeakerMode.Stereo : AudioSpeakerMode.Mono
            });

            var cfg = AudioSettings.GetConfiguration();
            cfg.speakerMode = speakerOn ? AudioSpeakerMode.Stereo : AudioSpeakerMode.Mono;
            AudioSettings.SetConfiguration(cfg);
            AudioSettings.speakerMode = speakerOn ? AudioSpeakerMode.Stereo : AudioSpeakerMode.Mono;
            throw new System.NotImplementedException();*/
        }

        private Dictionary<string, AudioSource> audioSources = new Dictionary<string, AudioSource>();
        private Dictionary<string, string> audioSourceCid = new Dictionary<string, string>();
        private Dictionary<string, List<string>> participantAudioCid = new Dictionary<string, List<string>>();

        public void AttachToTransform(string participantSid, Transform transform)
        {
            participantAudioCid.TryGetValue(participantSid, out var audioCids);
            if (audioCids == null) return;
            foreach (var audioCid in audioCids)
            {
                if (!audioSources.TryGetValue(audioCid, out var audioSource)) continue;
                if (audioSource != null)
                {
                    audioSource.transform.parent = transform;
                }
            }
        }

        public void UpdateWorldPosition(string participantSid, Vector3 pos)
        {
            participantAudioCid.TryGetValue(participantSid, out var audioCids);
            if (audioCids == null) return;
            foreach (var audioCid in audioCids)
            {
                if (!audioSources.TryGetValue(audioCid, out var audioSource)) continue;
                if (audioSource != null)
                {
                    audioSource.transform.position = pos;
                }
            }
        }

        public async UniTask StartAudio(Participant participant, string trackCid, Types.TrackSource source,
            MediaStreamTrack mediaStreamTrack,
            RTCRtpReceiver receiver)
        {
            this.LogWarning(
                $"[AudioManager] Start Audio {trackCid} {mediaStreamTrack.Id}-{mediaStreamTrack.Enabled} - {receiver}");
            // throw new NotImplementedException();
            if (mediaStreamTrack is AudioStreamTrack audioStreamTrack)
            {
                var a = CreateAudioSource(trackCid, source, participant);
                a.playOnAwake = false;
                a.SetTrack(audioStreamTrack);
                a.volume = 1.0f;
                a.Play();
            }
        }


        public async UniTask StopAudio(string trackCid)
        {
            this.LogWarning(
                $"[AudioManager] Stop Audio {trackCid}");

            if (audioSources.TryGetValue(trackCid, out var audioSource))
            {
                if (audioSource != null)
                {
                    audioSource.Stop();
                }
            }
        }


        public void RemoveAudio(string participantSid, string trackCid)
        {
            if (audioSources.TryGetValue(trackCid, out var audioSource))
            {
                if (audioSource != null)
                {
                    Object.Destroy(audioSource.gameObject);
                }
            }

            removeCachedTrack(participantSid, trackCid);
        }

        private void removeCachedTrack(string participantSid, string trackCid)
        {
            audioSources.Remove(trackCid);
            audioSourceCid.Remove(trackCid);
            participantAudioCid.TryGetValue(participantSid, out var audioCids);
            if (audioCids != null)
            {
                audioCids.Remove(trackCid);
                participantAudioCid[participantSid] = audioCids;
            }
        }

        private void addCachedTrack(string participantSid, string trackCid, AudioSource src)
        {
            audioSources[trackCid] = src;
            audioSourceCid[trackCid] = participantSid;
            participantAudioCid.TryGetValue(participantSid, out var audioCids);
            audioCids ??= new List<string>();
            if (!audioCids.Contains(trackCid))
            {
                audioCids.Add(trackCid);
                participantAudioCid[participantSid] = audioCids;
            }
        }

        public List<AudioStreamTrack> GetAudioTracks(TrackSource source)
        {
            this.LogWarning(
                $"[AudioManager] Get Audio Tracks For Source {source}");
            throw new System.NotImplementedException();
        }

        public AudioSource CreateAudioSource(string name, Types.TrackSource source, Participant participant)
        {
            var combinedName = $"{participant.Sid}_{source}_{name}";
            var a = new GameObject(combinedName).AddComponent<AudioSource>();
            a.transform.parent = LiveKitManager.Instance.AudioSourceTransform;
            a.loop = true;
            addCachedTrack(participant.Sid, name, a);
            return a;
        }

        private int selectedMicrophoneDeviceIndex;
        private string selectedMicrophoneDeviceName;
        private AudioSource microphoneSource;
        private AudioSource customSource;
        private string defaultMicrophoneAudioSourceName = "Local_Microphone";
        private string defaultCustomAudioSourceName = "Local_Custom";

        public AudioClip CreateMicrophoneClip(int index = 0)
        {
            this.LogWarning(
                $"[AudioManager] Create Microphone Clip");
            if (Microphone.devices.Length > 0)
            {
                if (index >= 0 && index < Microphone.devices.Length)
                {
                    selectedMicrophoneDeviceIndex = index;
                }
                else
                {
                    selectedMicrophoneDeviceIndex = 0;
                }

                selectedMicrophoneDeviceName = Microphone.devices[selectedMicrophoneDeviceIndex];
                //When a value of zero is returned in the minFreq and maxFreq parameters, this indicates that the device supports any frequency.
                Microphone.GetDeviceCaps(selectedMicrophoneDeviceName, out var minFrequency, out var maxFrequency);
                this.Log(
                    $"Use Microphone {selectedMicrophoneDeviceIndex} - [{selectedMicrophoneDeviceName}]" +
                    $" Min Frequency {minFrequency} Max Frequency {maxFrequency}");
            }
            else
            {
                this.LogError("No Microphone device");
                return null;
            }

            AudioClip microphoneAudioClip = null;
            try
            {
                microphoneAudioClip = Microphone.Start(selectedMicrophoneDeviceName, true, 2, 48000);
            }
            catch (Exception ex)
            {
                this.LogException(ex);
                return null;
            }


            // set the latency to “0” samples before the audio starts to play.
            while (!(Microphone.GetPosition(selectedMicrophoneDeviceName) > 0))
            {
            }

            return microphoneAudioClip;
        }

        private AudioClip fakeMicClip;

        public AudioSource RecordMicrophone(Participant participant, int deviceIndex = 0)
        {
            this.LogWarning(
                $"[AudioManager] Create Microphone Source");
            var clip = this.fakeMicClip ? this.fakeMicClip : CreateMicrophoneClip();
            if (clip == null)
            {
                return null;
            }

            if (microphoneSource == null)
            {
                microphoneSource = CreateAudioSource(defaultMicrophoneAudioSourceName, Types.TrackSource.Microphone,
                    participant);
            }

            microphoneSource.playOnAwake = false;
            microphoneSource.loop = true;
            microphoneSource.clip = clip;
            microphoneSource.Play();
            return microphoneSource;
        }

        public void ChangeMicrophone(Participant participant, int index)
        {
            if (microphoneSource == null)
            {
                this.LogError("ChangeMicrophone Fail MicrophoneSource is null or not playing");
                return;
            }

            if (index == selectedMicrophoneDeviceIndex)
            {
                this.LogWarning("ChangeMicrophone Fail Same Microphone");
                return;
            }

            if (index < 0 || index >= Microphone.devices.Length)
            {
                this.LogError("ChangeMicrophone Fail Invalid Microphone Index");
                return;
            }

            StopMicrophone();
            RecordMicrophone(participant, index);
        }

        public void StopMicrophone()
        {
            if (Microphone.IsRecording(selectedMicrophoneDeviceName))
            {
                Microphone.End(selectedMicrophoneDeviceName);
            }

            if (microphoneSource != null)
            {
                microphoneSource.Stop();
            }
        }

        public void CleanUp()
        {
            StopMicrophone();
            if (audioSources != null)
            {
                foreach (var audioSource in audioSources)
                {
                    if (audioSource.Value != null)
                    {
                        Object.Destroy(audioSource.Value.gameObject);
                    }
                }

                audioSources.Clear();
            }

            audioSourceCid.Clear();
            participantAudioCid.Clear();
        }

        public void MuteMicrophone()
        {
            if (microphoneSource != null)
            {
                microphoneSource.mute = true;
            }
        }

        public void UnmuteMicrophone()
        {
            if (microphoneSource != null)
            {
                microphoneSource.mute = false;
            }
        }

        public async UniTask<LocalAudioTrack> CreateMicrophoneTrack(Participant participant,
            AudioCaptureOptions captureOptions = null
            , int deviceIndex = 0)
        {
            var recordMicrophone = RecordMicrophone(participant, deviceIndex);
            if (recordMicrophone == null)
            {
                return null;
            }

            var track = await CreateAudioTrack(recordMicrophone, TrackSource.Microphone, captureOptions);
            return track;
        }

        public async UniTask<LocalAudioTrack> CreateAudioTrack(AudioSource src
            , TrackSource tsource, AudioCaptureOptions captureOptions = null)
        {
            try
            {
                if (src == null)
                {
                    return null;
                }

                var track = new AudioStreamTrack(src);
                return await LocalAudioTrack.CreateAudioTrackAsync(tsource,
                    track, captureOptions);
            }
            catch (Exception e)
            {
                this.LogError($"Failed to create microphone track: {e}");
                return null;
            }
        }

        public void SetCustomClip(AudioClip customAudioClip)
        {
            this.fakeMicClip = customAudioClip;
        }

        public AudioSource CreateAudioSourceCustom(Participant participant, AudioClip clip)
        {
            if (customSource == null)
            {
                customSource = CreateAudioSource(defaultCustomAudioSourceName, Types.TrackSource.Unknown, participant);
            }

            customSource.playOnAwake = false;
            customSource.loop = true;
            customSource.clip = clip;
            customSource.Play();
            return customSource;
        }

        public AudioSource GetAudioSource(string getCid)
        {
            if (audioSources.TryGetValue(getCid, out var audioSource))
            {
                return audioSource;
            }

            return null;
        }

        public string GetOwnerCid(string trackCid)
        {
            return audioSourceCid.TryGetValue(trackCid, out var participantSid) ? participantSid : null;
        }
    }
}