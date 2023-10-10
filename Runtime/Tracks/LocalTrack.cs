using System;
using Cysharp.Threading.Tasks;
//using System.Threading.Tasks;
using LiveKit.Proto;
using LiveKitUnity.Runtime.Debugging;
using LiveKitUnity.Runtime.Events;
using LiveKitUnity.Runtime.Types;
using Unity.WebRTC;
using TrackSource = LiveKit.Proto.TrackSource;

namespace LiveKitUnity.Runtime.Tracks
{
    public abstract class LocalTrack : Track
    {
        public virtual LocalTrackOptions CurrentOptions { get; set; }

        private bool _published = false;
        public bool IsPublished => _published;
        public string Codec { get; set; }

        public LocalTrack(TrackType kind, TrackSource source
            , MediaStreamTrack mediaStreamTrack, RTCRtpSender sender = null)
            : base(kind, source, mediaStreamTrack, sender)
        {
        }

        public async UniTask<bool> Mute()
        {
            this.Log($"LocalTrack.Mute() muted: {Muted}");

            if (Muted)
                return false; // already muted

            await Disable();
            // await Stop();
            await UpdateMuted(true, shouldSendSignal: true);
            return true;
        }

        public async UniTask<bool> Unmute()
        {
            this.Log($"LocalTrack.Unmute() muted: {Muted}");

            if (!Muted)
                return false; // already un-muted
            // await RestartTrack();
            await Enable();
            await UpdateMuted(false, shouldSendSignal: true);
            return true;
        }

        public override async UniTask<bool> Stop()
        {
            bool didStop = await base.Stop();

            if (didStop)
            {
                this.Log("Stopping mediaStreamTrack...");

                try
                {
                    MediaStreamTrack.Stop();
                    // SenderRaw.Stop();
                }
                catch (Exception error)
                {
                    this.LogError($"MediaStreamTrack.StopAsync() did throw {error}");
                }

                /*try
                {
                    Transceiver.Dispose();
                }
                catch (Exception error)
                {
                    this.LogError($"MediaStream.DisposeAsync() did throw {error}");
                }*/
            }

            return didStop;
        }


        public async UniTask RestartTrack(LocalTrackOptions options = null)
        {
            if (SenderRaw == null) throw new TrackCreateException("could not restart track");
            if (options != null && CurrentOptions.GetType() != options.GetType())
            {
                throw new Exception($"options must be a {CurrentOptions.GetType()}");
            }

            CurrentOptions = options ?? CurrentOptions;

            // stop if not already stopped...
            await Stop();


            // create new track with options
            // var newStream = await LocalTrack.CreateStream(CurrentOptions);
            // var newTrack = newStream.GetTracks().First();

            // replace track on sender
            try
            {
                // Sender?.ReplaceTrack(newTrack);
                if (this is LocalVideoTrack)
                {
                    var videoTrack = this as LocalVideoTrack;
                    // await videoTrack.ReplaceTrackForMultiCodecSimulcast(newTrack);
                }
            }
            catch (Exception error)
            {
                this.LogError($"RTCRtpSender.replaceTrack() did throw {error}");
            }

            // set new stream & track to this object
            // await UpdateMediaStreamAndTrack(newStream, newTrack);

            // mark as started
            await Start(currentParticipant);

            // notify so VideoView can re-compute mirror mode if necessary
            events.Emit(new LocalTrackOptionsUpdatedEvent(this, CurrentOptions));
        }

        public override async UniTask<bool> OnPublish()
        {
            if (_published)
            {
                // already published
                return false;
            }

            this.Log($"publish()");
            _published = true;
            return true;
        }

        public override async UniTask<bool> OnUnpublish()
        {
            if (!_published)
            {
                // already unpublished
                return false;
            }

            this.Log($".unpublish()");
            _published = false;
            return true;
        }
    }
}