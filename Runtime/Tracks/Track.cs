using System;
using Cysharp.Threading.Tasks;
//using System.Threading.Tasks;
using LiveKit.Proto;
using LiveKitUnity.Runtime.Debugging;
using LiveKitUnity.Runtime.Events;
using LiveKitUnity.Runtime.Participants;
using LiveKitUnity.Runtime.Types;
using Unity.WebRTC;
using TrackSource = LiveKitUnity.Runtime.Types.TrackSource;

// using MEC;

namespace LiveKitUnity.Runtime.Tracks
{
    public abstract class Track : EventsEmittable
    {
        public string Sid { get; set; }

        public TrackType Kind { get; }
        public TrackSource Source { get; }
        public MediaStreamTrack MediaStreamTrack => _mediaStreamTrack;
        private MediaStreamTrack _mediaStreamTrack;


        private string _cid;
        public bool Active => _active;
        private bool _active = false;
        public bool Muted => _muted;
        private bool _muted = false;

        public RTCRtpSender SenderRaw { get; set; }
        public RTCRtpReceiver ReceiverRaw { get; set; }

        public TrackKind MediaType
        {
            get
            {
                return Kind switch
                {
                    TrackType.Audio => TrackKind.Audio,
                    TrackType.Video => TrackKind.Video,
                    _ => TrackKind.Audio
                };
            }
        }

        public Action<object> OnVideoViewBuild { get; set; }

        public Track(TrackType kind, LiveKit.Proto.TrackSource source, MediaStreamTrack mediaStreamTrack,
            RTCRtpSender sender = null, RTCRtpReceiver receiver = null)
        {
            Kind = kind;
            Source = source.ToLKType();
            _mediaStreamTrack = mediaStreamTrack;
            SenderRaw = sender;
            ReceiverRaw = receiver;


            events.Listen(eventObj =>
            {
                this.Log($"[TrackEvent] {eventObj}, will NotifyListeners()");
                //notifyListeners();
            });
        }


        public Action OnDispose;

        public async UniTask Dispose()
        {
            this.Log($"OnDispose()");
            MediaStreamTrack?.Dispose();
            SenderRaw?.Dispose();
            ReceiverRaw?.Dispose();
            await Stop();
            await events.Dispose();
            OnDispose?.Invoke();
        }

        public string GetCid()
        {
            _cid = (_cid ?? MediaStreamTrack?.Id) ?? Guid.NewGuid().ToString();
            return _cid;
        }

        internal Participant currentParticipant;

        public virtual async UniTask<bool> Start(Participant participant)
        {
            if (_active)
            {
                return false;
            }

            currentParticipant = participant;
            this.Log($"Start() - {Sid}");
            startMonitor();
            _active = true;
            return true;
        }

        public virtual async UniTask<bool> Stop()
        {
            if (!_active)
            {
                return false;
            }

            stopMonitor();
            this.Log($"Stop() - {Sid}");

            _active = false;
            return true;
        }

        // private CoroutineHandle statsTimerHandle;
        public const float monitorFrequency = 2.0f;

        public virtual async UniTask<bool> OnUnpublish()
        {
            throw new NotImplementedException();
        }

        public virtual async UniTask<bool> OnPublish()
        {
            throw new NotImplementedException();
        }

        public virtual void onStatsTimer()
        {
            // this.Log("onStatsTimer");
        }

        private void startMonitor()
        {
            // statsTimerHandle = Timing.CallPeriodically(float.PositiveInfinity, monitorFrequency, onStatsTimer);
        }

        private void stopMonitor()
        {
            // Timing.KillCoroutines(statsTimerHandle);
        }

        public virtual async UniTask Enable()
        {
            this.Log($"Enabling {GetCid()}...");
            try
            {
                if (_active)
                {
                    MediaStreamTrack.Enabled = true;
                }
            }
            catch (Exception e)
            {
                this.LogWarning($"Set MediaStreamTrack.Enabled did throw {e}");
            }
        }

        public virtual async UniTask Disable()
        {
            this.Log($"Disable() disabling {GetCid()}...");
            try
            {
                if (_active)
                {
                    MediaStreamTrack.Enabled = false;
                }
            }
            catch (Exception e)
            {
                this.LogWarning($"[Set rtc.MediaStreamTrack.Enabled did throw {e}");
            }
        }

        public virtual async UniTask UpdateMuted(bool muted, bool shouldNotify = true, bool shouldSendSignal = false)
        {
            if (_muted == muted)
            {
                return;
            }

            _muted = muted;
            if (shouldNotify)
            {
                events.Emit(new InternalTrackMuteUpdatedEvent(this, muted, shouldSendSignal));
            }
        }

        public virtual async UniTask UpdateTrackAndTransceiver(MediaStreamTrack track, RTCRtpSender sender,
            RTCRtpReceiver receiver)
        {
            _mediaStreamTrack = track;
            SenderRaw = sender;
            ReceiverRaw = receiver;
            events.Emit(new TrackStreamUpdatedEvent(this, sender, receiver));
        }
    }
}