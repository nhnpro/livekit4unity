using System;
using Cysharp.Threading.Tasks;
//using System.Threading.Tasks;
using LiveKit.Proto;
using LiveKitUnity.Runtime.Debugging;
using LiveKitUnity.Runtime.Events;
using LiveKitUnity.Runtime.Participants;
using LiveKitUnity.Runtime.Tracks;
using LiveKitUnity.Runtime.Types;
using StreamState = LiveKit.Proto.StreamState;

namespace LiveKitUnity.Runtime.TrackPublications
{
    public class RemoteTrackPublication : TrackPublication
    {
        public override Participant Participant { get; set; }

        private bool _enabled = true;
        public bool Enabled => _enabled;

        private int _fps;
        public int Fps => _fps;

        private VideoQuality _videoQuality = VideoQuality.High;
        public VideoQuality VideoQuality => _videoQuality;

        private StreamState _streamState = StreamState.Paused;
        public StreamState StreamState => _streamState;

        private bool _metadataMuted = false;

        private bool _subscriptionAllowed = true;
        public bool SubscriptionAllowed => _subscriptionAllowed;

        public override bool Subscribed
        {
            get
            {
                // Always return false when subscription is not allowed
                if (!_subscriptionAllowed) return false;
                return base.Subscribed;
            }
        }

        public TrackSubscriptionState SubscriptionState
        {
            get
            {
                if (!_subscriptionAllowed) return TrackSubscriptionState.NotAllowed;
                return base.Subscribed
                    ? TrackSubscriptionState.Subscribed
                    : TrackSubscriptionState.Unsubscribed;
            }
        }

        public override void UpdateStreamState(StreamState streamState)
        {
            // Return if no change
            if (_streamState == streamState) return;
            _streamState = streamState;
            if (this.Participant is RemoteParticipant rp)
            {
                rp.events.Emit(new TrackStreamStateUpdatedEvent(rp, this, streamState.ToLKType()));
            }
        }

        private void _computeVideoViewVisibility(bool quick = false)
        {
            /*
                Vector2 MaxOfSizes(Vector2 s1, Vector2 s2) => new Vector2(Mathf.Max(s1.x, s2.x), Mathf.Max(s1.y, s2.y));

                var videoTrack = Track as RemoteVideoTrack;
                var settings = new UpdateTrackSettings
                {
                    Disabled = true,
                };
                settings.TrackSids.Add(Sid);

                // Filter visible build contexts
                var viewSizes = videoTrack.ViewKeys
                    .Select(e => e.CurrentContext)
                    .WhereNotNull()
                    .Select(e => e.FindRenderObject() as RenderBox)
                    .WhereNotNull()
                    .Select(e => e.Size);

                this.Log($"[Visibility] {Track?.Sid} watching {viewSizes.Count()} views...");

                if (viewSizes.Any())
                {
                    // Compute the largest size
                    var largestSize = viewSizes.Aggregate(MaxOfSizes);
                    settings.Disabled = false;
                    settings.Width = (uint)Mathf.CeilToInt(largestSize.Width);
                    settings.Height = (uint)Mathf.CeilToInt(largestSize.Height);
                }

                // Only send new settings to the server if it changed
                if (!settings.Equals(_lastSentTrackSettings))
                {
                    _lastSentTrackSettings = settings;
                    this.Log("[Visibility] Change detected, quick: " + quick);
                    if (quick)
                    {
                        _cancelPendingTrackSettingsUpdateRequest?.Invoke();
                        _sendPendingTrackSettingsUpdateRequest(settings);
                    }
                    else
                    {
                        Timing.CallDelayed(1.5f, () =>
                        {
                            _sendPendingTrackSettingsUpdateRequest(settings);
                        });
                    }
                }*/
        }

        private async void _sendPendingTrackSettingsUpdateRequest(UpdateTrackSettings settings)
        {
            this.Log($"[Visibility] Sending... TrackSettings {settings}");
            await this.Participant.room.engine.Client.SendUpdateTrackSettings(settings);
        }

        private UpdateTrackSettings _lastSentTrackSettings;

        // private Timer _visibilityTimer;
        private Action _cancelPendingTrackSettingsUpdateRequest;


        public RemoteTrackPublication(Participant participant, TrackInfo trackInfo, Track track) : base(trackInfo)
        {
            this.Log($"RemoteTrackPublication.init track: {track}, info: {trackInfo}");

            // Register dispose func
            /*OnDispose(async () =>
            {
                _cancelPendingTrackSettingsUpdateRequest?.Invoke();
                _visibilityTimer?.Stop();
                // This object is responsible for disposing the track
                await Track?.DisposeAsync();
            });*/
            this.Participant = participant;
            UpdateTrack(track);
        }


        public async UniTask SetVideoQuality(VideoQuality newValue)
        {
            if (newValue == _videoQuality) return;
            _videoQuality = newValue;
            await SendUpdateTrackSettings();
        }

        public async UniTask SetVideoFPS(int newValue)
        {
            if (newValue == _fps) return;
            _fps = newValue;
            await SendUpdateTrackSettings();
        }

        public async UniTask Enable()
        {
            if (_enabled) return;
            _enabled = true;
            await SendUpdateTrackSettings();
        }

        public async UniTask Disable()
        {
            if (!_enabled) return;
            _enabled = false;
            await SendUpdateTrackSettings();
        }

        public async UniTask Subscribe()
        {
            if (base.Subscribed || !_subscriptionAllowed)
            {
                this.Log($"Ignoring Subscribe() request {_subscriptionAllowed} {base.Subscribed}...");
                return;
            }

            await SendUpdateSubscription(true);
        }

        public async UniTask Unsubscribe()
        {
            if (!base.Subscribed || !_subscriptionAllowed)
            {
                this.Log("Ignoring Unsubscribe() request...");
                return;
            }

            await SendUpdateSubscription(false);
            if (Track != null)
            {
                // Ideally, we should wait for WebRTC's onRemoveTrack event
                // but it does not work reliably across platforms.
                // So for now, we will assume remove track succeeded.
                var newEvent = new TrackUnsubscribedEvent(this.Participant as RemoteParticipant, this, Track);
                Participant.events.Emit(newEvent);
                Participant.room.events.Emit(newEvent);
                // Simply set to null for now
                await UpdateTrack(null);
            }
        }

        private async UniTask SendUpdateSubscription(bool subscribed)
        {
            this.Log($"Sending update subscription... {Sid} {subscribed}");
            var participantTrack = new ParticipantTracks
            {
                ParticipantSid = Participant.Sid,
            };
            participantTrack.TrackSids.Add(Sid);
            var subscription = new UpdateSubscription
            {
                Subscribe = subscribed
            };
            subscription.ParticipantTracks.Add(participantTrack);
            subscription.TrackSids.Add(Sid);
            await Participant.room.engine.Client.SendUpdateSubscription(subscription);
        }

        public async UniTask SendUpdateTrackSettings()
        {
            var settings = new UpdateTrackSettings
            {
                Disabled = !_enabled,
            };
            settings.TrackSids.Add(Sid);
            if (Kind == TrackType.Video)
            {
                settings.Quality = _videoQuality;
                if (_fps != 0) settings.Fps = (uint)_fps;
            }

            await Participant.room.engine.Client.SendUpdateTrackSettings(settings);
        }

        public async UniTask<bool> UpdateSubscriptionAllowed(bool allowed)
        {
            if (_subscriptionAllowed == allowed) return false;
            _subscriptionAllowed = allowed;

            this.Log($"UpdateSubscriptionAllowed allowed: {allowed}");
            // Emit events
            Participant.events.Emit(
                new TrackSubscriptionPermissionChangedEvent(this.Participant, this, SubscriptionState));

            if (!_subscriptionAllowed && base.Subscribed /* Track != null */)
            {
                // Ideally, we should wait for WebRTC's onRemoveTrack event
                // but it does not work reliably across platforms.
                var e = new TrackUnsubscribedEvent(this.Participant as RemoteParticipant, this, Track);
                // So for now, we will assume remove track succeeded.
                Participant.events.Emit(e);
                Participant.room.events.Emit(e);
                // Simply set to null for now
                await UpdateTrack(null);
            }

            return true;
        }


        public override async UniTask<bool> UpdateTrack(Track newValue)
        {
            this.Log("RemoteTrackPublication.UpdateTrack track: " + newValue);
            var didUpdate = await base.UpdateTrack(newValue);

            if (didUpdate)
            {
                // Stop the current visibility timer (if it exists)
                _cancelPendingTrackSettingsUpdateRequest?.Invoke();
                // _visibilityTimer?.Stop();

                if (newValue != null)
                {
                    var roomOptions = Participant.room.RoomOptions;
                    if (roomOptions.AdaptiveStream && newValue is RemoteVideoTrack)
                    {
                        // Start monitoring visibility
                        /*_visibilityTimer = new Timer(300);
                        _visibilityTimer.Elapsed += (sender, e) => _computeVideoViewVisibility();
                        _visibilityTimer.Start();*/

                        newValue.OnVideoViewBuild = (_) =>
                        {
                            this.Log("[Visibility] VideoView did build");
                            if (_lastSentTrackSettings?.Disabled == true)
                            {
                                // Quick enable
                                _cancelPendingTrackSettingsUpdateRequest?.Invoke();
                                _computeVideoViewVisibility(quick: true);
                            }
                        };
                    }

                    // If a new Track has been set to this RemoteTrackPublication,
                    // update the Track's muted state from the latest info.
                    await newValue.UpdateMuted(_metadataMuted,
                        shouldNotify: false); // Don't emit an event since this is the initial state
                }
            }

            return didUpdate;
        }
    }
}