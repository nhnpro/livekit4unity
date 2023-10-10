using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
//using System.Threading.Tasks;
using LiveKitUnity.Runtime.Tracks;
using LiveKitUnity.Runtime.Types;
using Unity.WebRTC;
using UnityEngine;
using TrackSource = LiveKit.Proto.TrackSource;

namespace LiveKitUnity.Runtime
{
    public class VideoManager
    {
        private static VideoManager _instance;
        public static VideoManager Instance => _instance ??= new VideoManager();

        public List<VideoStreamTrack> GetVideoTracks(TrackSource source)
        {
            throw new NotImplementedException();
        }

        public async UniTask<Texture> CreateWebCamTexture(CameraCaptureOptions captureOptions)
        {
            throw new NotImplementedException();
        }

        public async UniTask<LocalVideoTrack> CreateWebcamTrack(CameraCaptureOptions captureOptions)
        {
            try
            {
                var texture = await CreateWebCamTexture(captureOptions);
                var track = new VideoStreamTrack(texture);
                return LocalVideoTrack.CreateVideoTrackAsync(TrackSource.Camera, track, captureOptions);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to create webcam track: {e}");
                return null;
            }
        }

        public void CleanUp()
        {
            
        }
    }
}