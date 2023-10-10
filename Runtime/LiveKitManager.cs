using System;
using Cysharp.Threading.Tasks;
// //using System.Threading.Tasks;
using LiveKitUnity.Runtime.Core;
using LiveKitUnity.Runtime.Events;
using LiveKitUnity.Runtime.Participants;
using LiveKitUnity.Runtime.TrackPublications;
using LiveKitUnity.Runtime.Types;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
using Unity.WebRTC;
using UnityEngine;
using UnityEngine.Events;

namespace LiveKitUnity.Runtime
{
    public class LiveKitManager : MonoBehaviour
    {
        private static LiveKitManager _instance;

        public static LiveKitManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<LiveKitManager>();
                }

                if (_instance == null)
                {
                    var go = new GameObject("LiveKitManager");
                    _instance = go.AddComponent<LiveKitManager>();
                }

                return _instance;
            }
        }

        private bool _isInitialized = false;
        private Room activeRoom;
        public Transform AudioSourceTransform;

        public void Awake()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (_isInitialized) return;
            WebRTC.Initialize();
            StartCoroutine(WebRTC.Update());
            if (AudioSourceTransform == null)
            {
                AudioSourceTransform = this.transform;
            }

            _isInitialized = true;
        }

        private void OnDestroy()
        {
            StopCoroutine(WebRTC.Update());
            WebRTC.Dispose();
            Disconnect();
            _isInitialized = false;
        }

        public Room GetActiveRoom()
        {
            return activeRoom;
        }

        public async void Disconnect()
        {
            if (activeRoom != null)
            {
                await activeRoom.Disconnect();
            }

            AudioManager.Instance.CleanUp();
            VideoManager.Instance.CleanUp();
        }

        public string Url;
        public string Token;

#if ODIN_INSPECTOR
        [Button]
#endif
        public async void Connect()
        {
            if (string.IsNullOrEmpty(Url) || string.IsNullOrEmpty(Token))
            {
                Debug.LogWarning($"[LiveKitManager]Can not Connect cause Url or Token is empty");
                return;
            }

            await ConnectRoom(Url, Token, internalRoomCallback);
        }

        private void internalRoomCallback(IRoomEvent roomEvent)
        {
            var t = roomEvent.GetType();
            Debug.Log($"[LiveKitManager] internalRoomCallback - Room Event {t}");
        }


        public async UniTask<Room> ConnectRoom(string url, string token, Action<IRoomEvent> onRoomCallbackEvent = null)
        {
            activeRoom = new Room();
            if (onRoomCallbackEvent != null)
            {
                var listener = activeRoom.CreateListener(true);
                listener.On<IRoomEvent>(onRoomCallbackEvent);
            }

            await activeRoom.Connect(url, token);
            return activeRoom;
        }

        public async void MicrophoneStart(int index = 0)
        {
            if (activeRoom == null) return;
            if (activeRoom.ConnectionState != ConnectionState.Connected)
            {
                Debug.LogWarning($"[LiveKitManager]Can not Start Microphone cause Room is not connected");
                return;
            }

            // AudioManager.Instance.SetCustomClip(FakeMicrophoneClip);
            await activeRoom.LocalParticipant.SetMicrophoneEnabled(true, null, index);
        }

        public async void MicrophoneStop()
        {
            if (activeRoom == null) return;
            AudioManager.Instance.StopMicrophone();
            await activeRoom.LocalParticipant.UnpublishMicrophoneTrack();
        }

        public async void MicrophoneMute()
        {
            if (activeRoom == null) return;
            var pub = activeRoom.LocalParticipant.getTrackPublicationBySource(TrackSource.Microphone);
            if (pub is { Muted: false })
            {
                await activeRoom.LocalParticipant.SetMicrophoneEnabled(false);
            }
        }

        public async void MicrophoneUnmute()
        {
            if (activeRoom == null) return;
            var pub = activeRoom.LocalParticipant.getTrackPublicationBySource(TrackSource.Microphone);
            if (pub is { Muted: true })
            {
                await activeRoom.LocalParticipant.SetMicrophoneEnabled(true);
            }
        }

        private LocalTrackPublication customPublication;

        public async void StartPlayCustomClip(Participant participant, AudioClip clip)
        {
            if (activeRoom == null) return;
            if (activeRoom.ConnectionState != ConnectionState.Connected)
            {
                Debug.LogWarning($"[LiveKitManager]Can not Start Microphone cause Room is not connected");
                return;
            }

            var src = AudioManager.Instance.CreateAudioSourceCustom(participant, clip);
            var pub = await StreamCustomAudioSource(src);
            if (pub != null)
            {
                customPublication = pub;
            }
        }

        public async UniTask<LocalTrackPublication> StreamCustomAudioSource(AudioSource src,
            AudioCaptureOptions audioCaptureOptions = null)
        {
            if (src != null)
            {
                var captureOptions = audioCaptureOptions ?? activeRoom.RoomOptions.DefaultAudioCaptureOptions;
                var track = await AudioManager.Instance.CreateAudioTrack(src, LiveKit.Proto.TrackSource.Unknown,
                    captureOptions);
                if (track != null)
                {
                    return await activeRoom.LocalParticipant.PublishAudioTrack(track);
                }
            }

            return null;
        }

        public async void StopPlayCustomClip()
        {
            if (customPublication == null) return;
            await customPublication.Mute();
            await activeRoom.LocalParticipant.UnpublishTrack(customPublication.Sid);
            customPublication = null;
        }

        public async void MuteCustomClip()
        {
            if (customPublication == null) return;
            await customPublication.Mute();
        }

        public async void UnmuteCustomClip()
        {
            if (customPublication == null) return;
            await customPublication.Unmute();
        }
    }
}