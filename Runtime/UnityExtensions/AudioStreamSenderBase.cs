using System;
using LiveKitUnity.Runtime.TrackPublications;
using LiveKitUnity.Runtime.Tracks;
using LiveKitUnity.Runtime.Types;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
using Unity.WebRTC;
using UnityEngine;

namespace LiveKitUnity.Runtime
{
    public class AudioStreamSenderBase : MonoBehaviour
    {
        public TrackSource trackSource;
        public AudioStreamTrack currentStreamTrack;
        public LocalTrackPublication trackPublication;

        public virtual AudioStreamTrack CreateTrack()
        {
            return null;
        }

#if ODIN_INSPECTOR
        [Button]
#endif
        public virtual async void Publish()
        {
            currentStreamTrack = CreateTrack();
            if (currentStreamTrack == null)
                throw new Exception("Track is null");
            var localAudioTrack = await LocalAudioTrack.CreateAudioTrackAsync(trackSource.ToPBType()
                , currentStreamTrack);
            if (localAudioTrack == null)
                throw new Exception("LocalAudioTrack is null");
            var room = LiveKitManager.Instance.GetActiveRoom();
            if (room == null)
                throw new Exception("Room is null");
            if (room.ConnectionState != ConnectionState.Connected)
                throw new Exception("Room is not connected");
            trackPublication = await room.LocalParticipant.PublishAudioTrack(localAudioTrack);

            if (trackPublication != null)
            {
                trackPublication.Track.OnDispose += OnTrackDispose;
            }
        }

        public virtual void OnTrackDispose()
        {
            trackPublication = null;
        }


#if ODIN_INSPECTOR
        [Button]
#endif
        public virtual async void Unpublish()
        {
            if (trackPublication == null)
                throw new Exception("TrackPublication is null");

            var room = LiveKitManager.Instance.GetActiveRoom();
            if (room == null)
                throw new Exception("Room is null");
            if (room.ConnectionState != ConnectionState.Connected)
                throw new Exception("Room is not connected");

            await room.LocalParticipant.UnpublishTrack(trackPublication.Sid);
        }

#if ODIN_INSPECTOR
        [Button]
#endif
        public virtual async void Mute()
        {
            if (trackPublication != null)
            {
                await trackPublication.Mute();
            }
        }

#if ODIN_INSPECTOR
        [Button]
#endif
        public virtual async void Unmute()
        {
            if (trackPublication != null)
            {
                await trackPublication.Unmute();
            }
        }
    }
}