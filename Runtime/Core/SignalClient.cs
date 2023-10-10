using System;
using System.Collections.Generic;
using System.Linq;
// //using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Google.Protobuf;
using LiveKit.Proto;
using LiveKitUnity.Runtime.Debugging;
using LiveKitUnity.Runtime.Events;
using LiveKitUnity.Runtime.Types;
using NativeWebSocket;
using Unity.WebRTC;
using DisconnectReason = LiveKit.Proto.DisconnectReason;
using TrackSource = LiveKit.Proto.TrackSource;

namespace LiveKitUnity.Runtime.Core
{
    public class SignalClient : EventsEmittable, IDisposable
    {
        private ConnectionState _connectionState = ConnectionState.Disconnected;
        private WebSocket _wsConnector;
        private Queue<SignalRequest> _queue;
        private Uri uri;
        private bool isDisposed = false;

        public int PingCount => pingCount;
        public bool IsConnected => _wsConnector is { State: WebSocketState.Open };

        private int pingCount = 0;

        public SignalClient()
        {
            _queue = new Queue<SignalRequest>();
        }

        public async void Dispose()
        {
            isDisposed = true;
            await CleanUp();
            await events.Dispose();
        }

        /*private IEnumerator<float> websocketUpdateLoop()
        {
            while (_wsConnector != null)
            {
                _wsConnector.DispatchMessageQueue();
                yield return Timing.WaitForOneFrame;
            }
        }*/

        private async UniTask websocketUpdateLoopAsync()
        {
            while (_wsConnector != null)
            {
                _wsConnector.DispatchMessageQueue();
                await UniTask.DelayFrame(1);
            }
        }

        public async UniTask Connect(string uriString, string token
            , ConnectOptions connectOptions
            , RoomOptions roomOptions
            , bool reconnect = false, string sid = "")
        {
            try
            {
                uri = await LiveKitUtils.BuildUri(
                    uriString,
                    token,
                    connectOptions,
                    roomOptions,
                    reconnect,
                    sid,
                    false,
                    false
                );

                this.Log($"connecting to {uri}");
                updateConnectionState(reconnect
                    ? ConnectionState.Reconnecting
                    : ConnectionState.Connecting);
                await CleanUp();
                _wsConnector = new WebSocket(uri.ToString());
                _wsConnector.OnOpen += onSocketOpen;
                _wsConnector.OnMessage += onSocketMessage;
                _wsConnector.OnError += onSocketError;
                _wsConnector.OnClose += onSocketClose;
                // Timing.RunCoroutine(websocketUpdateLoop());
                websocketUpdateLoopAsync();
                await _wsConnector.Connect();
            }
            catch (Exception ex)
            {
                var finalError = ex;
                try
                {
                    if (reconnect)
                        throw;
                    //TODO: 1B Re-build same uri for validate mode
                    var validateUri = await LiveKitUtils.BuildUri(
                        uriString,
                        token,
                        connectOptions,
                        roomOptions,
                        reconnect,
                        sid,
                        true,
                        LiveKitUtils.IsSecureScheme(uri.Scheme)
                    );

                    var validateResponse = await LiveKitUtils.HTTPGet(validateUri);
                    if (validateResponse.responseCode != 200)
                    {
                        finalError = new ConnectException(validateResponse.downloadHandler.text);
                    }
                }
                catch (Exception validateError)
                {
                    finalError = validateError;
                }
                finally
                {
                    updateConnectionState(ConnectionState.Disconnected);
                    throw finalError;
                }
            }
        }

        public async UniTask CleanUp()
        {
            this.Log("cleanUp");
            _queue.Clear();
            clearPingInterval();

            try
            {
                if (_wsConnector != null)
                {
                    await _wsConnector.Close();
                    _wsConnector = null;
                }
            }
            catch (Exception e)
            {
                this.LogException(e);
            }
        }

        /*Ping pong start*/
        private int pingTimeoutDuration = 0;
        // CoroutineHandle pingTimeoutTimer;

        private int pingIntervalDuration = 0;
        // CoroutineHandle pingIntervalTimer;

        private void startPingInterval()
        {
            clearPingInterval();
            resetPingTimeout();

            if (pingIntervalDuration == 0)
            {
                this.LogWarning("ping timeout duration not set");
                return;
            }

            //TODO: 1B
            /*_pingIntervalTimer ??=
                Timer.periodic(pingIntervalDuration!, (_) => sendPing());*/
        }

        private void resetPingTimeout()
        {
            clearPingTimeout();
            if (pingTimeoutDuration == 0)
            {
                this.LogWarning("ping timeout duration not set");
                return;
            }

            //TODO: 1B
            /*_pingTimeoutTimer ??= Timer(pingTimeoutDuration!, () {
                this.LogWarning("ping timeout");
                onSocketClose(WebSocketCloseCode.Abnormal);
            });*/
        }

        private void clearPingTimeout()
        {
            //TODO: 1B
            // Timing.KillCoroutines(pingTimeoutTimer);
        }

        private void clearPingInterval()
        {
            //TODO: 1B
            clearPingTimeout();
            // Timing.KillCoroutines(pingIntervalTimer);
        }


        private async UniTask sendPing()
        {
            await SendRequest(new SignalRequest
                {
                    Ping = currentMilliseconds(),
                }
            );
        }
        /*ping pong end*/

        private void onSocketMessage(byte[] messageBytes)
        {
            if (messageBytes == null)
            {
                return;
            }

            var msg = SignalResponse.Parser.ParseFrom(messageBytes);
            this.Log($"received signal message: {msg.MessageCase}");
            switch (msg.MessageCase)
            {
                case SignalResponse.MessageOneofCase.Join:
                    if (msg.Join.PingTimeout > 0)
                    {
                        pingTimeoutDuration = msg.Join.PingTimeout;
                        pingIntervalDuration = msg.Join.PingInterval;
                        this.Log($"ping config timeout: {msg.Join.PingTimeout}, interval: {msg.Join.PingInterval}");
                        startPingInterval();
                    }

                    events.Emit(new SignalJoinResponseEvent(msg.Join));
                    break;
                case SignalResponse.MessageOneofCase.Answer:
                    events.Emit(new SignalAnswerEvent(msg.Answer.ToSDKType()));
                    break;
                case SignalResponse.MessageOneofCase.Offer:
                    events.Emit(new SignalOfferEvent(msg.Offer.ToSDKType()));
                    break;
                case SignalResponse.MessageOneofCase.Trickle:
                    events.Emit(new SignalTrickleEvent
                    (
                        RTCExtensions.FromJson(msg.Trickle.CandidateInit),
                        msg.Trickle.Target
                    ));
                    break;
                case SignalResponse.MessageOneofCase.Update:
                    this.LogWarning($"xxxxxxxxxxxxxxxxxxxxxxxxx {msg.Update.Participants}");
                    events.Emit(new SignalParticipantUpdateEvent(msg.Update.Participants.ToList()));
                    break;
                case SignalResponse.MessageOneofCase.TrackPublished:
                    events.Emit(new SignalLocalTrackPublishedEvent
                    (
                        msg.TrackPublished.Cid,
                        msg.TrackPublished.Track
                    ));
                    break;
                case SignalResponse.MessageOneofCase.TrackUnpublished:
                    events.Emit(new SignalTrackUnpublishedEvent
                    (
                        msg.TrackUnpublished.TrackSid
                    ));
                    break;
                case SignalResponse.MessageOneofCase.SpeakersChanged:
                    events.Emit(new SignalSpeakersChangedEvent(msg.SpeakersChanged.Speakers.ToList()));
                    break;
                case SignalResponse.MessageOneofCase.RoomUpdate:
                    events.Emit(new SignalRoomUpdateEvent(msg.RoomUpdate.Room));
                    break;
                case SignalResponse.MessageOneofCase.ConnectionQuality:
                    events.Emit(new SignalConnectionQualityUpdateEvent(msg.ConnectionQuality.Updates.ToList()));
                    break;
                case SignalResponse.MessageOneofCase.Leave:
                    events.Emit(new SignalLeaveEvent(msg.Leave.CanReconnect, msg.Leave.Reason));
                    break;
                case SignalResponse.MessageOneofCase.Mute:
                    events.Emit(new SignalRemoteMuteTrackEvent(msg.Mute.Sid, msg.Mute.Muted));
                    break;
                case SignalResponse.MessageOneofCase.StreamStateUpdate:
                    events.Emit(new SignalStreamStateUpdatedEvent(msg.StreamStateUpdate.StreamStates.ToList()));
                    break;
                case SignalResponse.MessageOneofCase.SubscribedQualityUpdate:
                    events.Emit(new SignalSubscribedQualityUpdatedEvent(
                        msg.SubscribedQualityUpdate.TrackSid,
                        msg.SubscribedQualityUpdate.SubscribedQualities.ToList(),
                        msg.SubscribedQualityUpdate.SubscribedCodecs.ToList()));
                    break;
                case SignalResponse.MessageOneofCase.SubscriptionPermissionUpdate:
                    events.Emit(new SignalSubscriptionPermissionUpdateEvent(
                        msg.SubscriptionPermissionUpdate.ParticipantSid,
                        msg.SubscriptionPermissionUpdate.TrackSid,
                        msg.SubscriptionPermissionUpdate.Allowed));
                    break;
                case SignalResponse.MessageOneofCase.RefreshToken:
                    events.Emit(new SignalTokenUpdatedEvent(msg.RefreshToken));
                    break;
                case SignalResponse.MessageOneofCase.None:
                    this.Log("signal message not set");
                    break;
                case SignalResponse.MessageOneofCase.Pong:
                    pingCount++;
                    resetPingTimeout();
                    break;
                case SignalResponse.MessageOneofCase.Reconnect:
                    events.Emit(new SignalReconnectResponseEvent(msg.Reconnect));
                    break;
                default:
                    this.LogWarning("received unknown signal message");
                    break;
            }
        }

        private void onSocketClose(WebSocketCloseCode closeCode)
        {
            this.Log("socket closed with code: " + closeCode);
            updateConnectionState(ConnectionState.Disconnected);
        }

        private void onSocketError(string error)
        {
            this.Log($"socket error: {error}");
        }

        private void onSocketOpen()
        {
            this.Log("socket opened");
            updateConnectionState(ConnectionState.Connected);
        }

        private void updateConnectionState(ConnectionState newValue)
        {
            if (_connectionState == newValue) return;
            this.Log($"SignalClient ConnectionState {_connectionState} -> {newValue}");

            bool didReconnect = _connectionState == ConnectionState.Reconnecting &&
                                newValue == ConnectionState.Connected;

            var oldState = _connectionState;

            if (newValue == ConnectionState.Connected &&
                oldState == ConnectionState.Reconnecting)
            {
                // restart ping interval as it's cleared for reconnection
                startPingInterval();
            }
            else if (newValue == ConnectionState.Reconnecting)
            {
                // clear ping interval and restart it once reconnected
                clearPingInterval();
            }

            _connectionState = newValue;

            events.Emit(new SignalConnectionStateUpdatedEvent(
                _connectionState, oldState, didReconnect, DisconnectReason.UnknownReason.ToSDKType()
            ));
        }

        public async UniTask SendRequest(SignalRequest req, bool enqueueIfReconnecting = true)
        {
            if (isDisposed)
            {
                this.LogWarning($"Could not send message, already disposed");
                return;
            }

            if (_connectionState == ConnectionState.Reconnecting
                && canQueue(req.MessageCase) && enqueueIfReconnecting)
            {
                _queue.Enqueue(req);
                return;
            }

            if (_wsConnector == null)
            {
                this.LogWarning(" Could not send message, socket is null");
                return;
            }

            try
            {
                if (_wsConnector.State != WebSocketState.Open)
                {
                    this.LogWarning(" Could not send message, socket is not open");
                    return;
                }

                await _wsConnector.Send(req.ToByteArray());
                this.Log($"sent signal message: {req.MessageCase}");
            }
            catch (Exception e)
            {
                this.LogException(e);
            }
        }


        private long currentMilliseconds()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        private bool canQueue(SignalRequest.MessageOneofCase messageCase)
        {
            switch (messageCase)
            {
                case SignalRequest.MessageOneofCase.SyncState:
                case SignalRequest.MessageOneofCase.Trickle:
                case SignalRequest.MessageOneofCase.Offer:
                case SignalRequest.MessageOneofCase.Answer:
                case SignalRequest.MessageOneofCase.Simulate:
                    return false;
            }

            return true;
        }


        public async UniTask SendQueuedRequests()
        {
            // Queue is empty
            if (_queue.Count == 0)
            {
                return;
            }

            // Send requests
            foreach (var request in _queue)
            {
                await SendRequest(request, enqueueIfReconnecting: false);
            }

            _queue.Clear();
        }

        public void ClearQueue() => _queue.Clear();

        public async UniTask SendOffer(RTCSessionDescription offer)
        {
            await SendRequest(new SignalRequest
            {
                Offer = offer.ToPBType()
            });
        }

        public async UniTask SendAnswer(RTCSessionDescription answer)
        {
            await SendRequest(new SignalRequest
            {
                Answer = answer.ToPBType()
            });
        }

        public async UniTask SendIceCandidate(RTCIceCandidate candidate, SignalTarget target)
        {
            await SendRequest(new SignalRequest
            {
                Trickle = new TrickleRequest
                {
                    CandidateInit = candidate.ToJson(),
                    Target = target
                }
            });
        }

        public async UniTask SendMuteTrack(string trackSid, bool muted)
        {
            await SendRequest(new SignalRequest
            {
                Mute = new MuteTrackRequest
                {
                    Sid = trackSid,
                    Muted = muted
                }
            });
        }

        public async UniTask SendUpdateLocalMetadata(UpdateParticipantMetadata metadata)
        {
            await SendRequest(new SignalRequest
            {
                UpdateMetadata = metadata
            });
        }

        public async UniTask SendUpdateTrackSettings(UpdateTrackSettings settings)
        {
            await SendRequest(new SignalRequest
            {
                TrackSetting = settings
            });
        }

        public async UniTask SendUpdateSubscription(UpdateSubscription subscription)
        {
            await SendRequest(new SignalRequest
            {
                Subscription = subscription
            });
        }

        public async UniTask SendUpdateVideoLayers(string trackSid, List<VideoLayer> layers)
        {
            await SendRequest(new SignalRequest
            {
                UpdateLayers = new UpdateVideoLayers
                {
                    TrackSid = trackSid,
                    Layers = { layers.ToList() }
                }
            });
        }

        public async UniTask SendUpdateSubscriptionPermissions(bool allParticipants,
            List<TrackPermission> trackPermissions)
        {
            await SendRequest(new SignalRequest
            {
                SubscriptionPermission = new SubscriptionPermission
                {
                    AllParticipants = allParticipants,
                    TrackPermissions = { trackPermissions.ToList() }
                }
            });
        }

        public async UniTask SendSyncState(
            SessionDescription? answer,
            UpdateSubscription subscription,
            List<TrackPublishedResponse>? publishTracks,
            List<DataChannelInfo>? dataChannelInfo)
        {
            await SendRequest(new SignalRequest
            {
                SyncState = new SyncState
                {
                    Answer = answer,
                    Subscription = subscription,
                    PublishTracks = { publishTracks?.ToList() },
                    DataChannels = { dataChannelInfo?.ToList() }
                }
            });
        }

        public async UniTask SendSimulateScenario(
            int speakerUpdate,
            bool nodeFailure,
            bool migration,
            bool serverLeave,
            bool switchCandidate)
        {
            await SendRequest(new SignalRequest
            {
                Simulate = new SimulateScenario
                {
                    SpeakerUpdate = speakerUpdate,
                    NodeFailure = nodeFailure,
                    Migration = migration,
                    ServerLeave = serverLeave,
                    SwitchCandidateProtocol = switchCandidate ? CandidateProtocol.Tcp : CandidateProtocol.Tls
                }
            });
        }


        public async UniTask SendLeave()
        {
            await SendRequest(new SignalRequest
            {
                Leave = new LeaveRequest()
            });
        }


        public async UniTask SendAddTrack(string cid, string name, TrackType type, TrackSource source
            , VideoDimensions dimensions, bool? dtx, List<VideoLayer> videoLayers
            , EncryptionType encryptionType, List<SimulcastCodec> simulcastCodecs, string sid)
        {
            /*var req = new AddTrackRequest
            {
                Cid = cid,
                Name = name,
                Type = type,
                Source = source,
                Encryption = encryptionType.ToPbType(),
                Sid = sid,
                Muted = false
            };*/

            var req = new AddTrackRequest
            {
                Cid = cid,
                Name = name,
                Type = type,
                Source = source,
                Encryption = encryptionType.ToPbType(),
            };

            if (simulcastCodecs != null)
            {
                req.SimulcastCodecs.AddRange(simulcastCodecs);
            }

            if (type == TrackType.Video)
            {
                // video specific
                if (dimensions != null)
                {
                    req.Width = (uint)dimensions.width;
                    req.Height = (uint)dimensions.height;
                }

                if (videoLayers != null && videoLayers.Any())
                {
                    // req.Layers = new List<VideoLayer>();
                    req.Layers.AddRange(videoLayers);
                }
            }

            if (type == TrackType.Audio && dtx != null)
            {
                // audio specific
                req.DisableDtx = !(bool)dtx;
            }

            await SendRequest(new SignalRequest
            {
                AddTrack = req
            });
        }
    }
}