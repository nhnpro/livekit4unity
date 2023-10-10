using System;
using System.Collections.Generic;
using System.Linq;
// //using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Google.Protobuf;
using LiveKit.Proto;
using LiveKitUnity.Runtime.Debugging;
using LiveKitUnity.Runtime.Events;
using LiveKitUnity.Runtime.Internal;
using LiveKitUnity.Runtime.TrackPublications;
using LiveKitUnity.Runtime.Tracks;
using LiveKitUnity.Runtime.Types;
// using MEC;
using Unity.WebRTC;
using DisconnectReason = LiveKitUnity.Runtime.Types.DisconnectReason;
using TrackSource = LiveKit.Proto.TrackSource;

namespace LiveKitUnity.Runtime.Core
{
    public class Engine : EventsEmittable, IDisposable
    {
        private const string _lossyDCLabel = "_lossy";
        private const string _reliableDCLabel = "_reliable";

        private readonly SignalClient _signalClient;
        public SignalClient Client => _signalClient;
        private readonly PeerConnectionCreate _peerConnectionCreate;

        public Transport publisher;
        public Transport subscriber;
        private Transport Primary => _subscriberPrimary ? subscriber : publisher;

        public RTCDataChannel DataChannel
        {
            get
            {
                if (_subscriberPrimary)
                {
                    return _reliableDCSub ?? _lossyDCSub;
                }

                return _reliableDCPub ?? _lossyDCPub;
            }
        }

        private RTCDataChannel _reliableDCPub;
        private RTCDataChannel _lossyDCPub;
        private RTCDataChannel _reliableDCSub;
        private RTCDataChannel _lossyDCSub;

        private ConnectionState _connectionState = ConnectionState.Disconnected;
        public ConnectionState ConnectionState => _connectionState;

        private bool _hasPublished = false;
        private bool _restarting = false;
        private ClientConfiguration _clientConfiguration;
        private string url;
        private string token;

        public ConnectOptions ConnectOptions { get; set; }
        public RoomOptions RoomOptions { get; set; }
        public FastConnectOptions FastConnectOptions { get; set; }

        private bool _subscriberPrimary = false;
        private string _participantSid;
        public string ParticipantSid => _participantSid;

        private string _connectedServerAddress;
        public string ConnectedServerAddress => _connectedServerAddress;

        private bool fullReconnect = false;
        public bool FullReconnect => fullReconnect;

        private List<RTCIceServer> _serverProvidedIceServers = new List<RTCIceServer>();

        private EventsListener signalListener;

        private bool isDebugMode = true;

        public Engine(
            ConnectOptions connectOptions,
            RoomOptions roomOptions,
            SignalClient signalClient = null,
            PeerConnectionCreate peerConnectionCreate = null)
        {
            this.ConnectOptions = connectOptions ?? new ConnectOptions();
            this.RoomOptions = roomOptions ?? new RoomOptions();
            this.FastConnectOptions = null;
            this._signalClient = signalClient ?? new SignalClient();
            this._peerConnectionCreate =
                peerConnectionCreate ?? this.internalPeerConnectionCreate;

            if (isDebugMode)
            {
                // log all EngineEvents
                events.Listen(evt => this.Log($"[EngineEvent]{evt}"));
            }

            setUpEngineListeners();
            signalListener = this._signalClient?.CreateListener(synchronized: true);
            setUpSignalListeners();
        }

        private async UniTask<RTCPeerConnection> internalPeerConnectionCreate(RTCConfiguration configuration,
            Dictionary<string, dynamic> constraints)
        {
            var x = new RTCPeerConnection(ref configuration);
            return x;
        }

        public async UniTask Connect(
            string _url, string _token,
            ConnectOptions connectOptions = null,
            RoomOptions roomOptions = null,
            FastConnectOptions fastConnectOptions = null)
        {
            this.url = _url;
            this.token = _token;
            // Update new options (if they exist)
            this.ConnectOptions = connectOptions ?? this.ConnectOptions;
            this.RoomOptions = roomOptions ?? this.RoomOptions;
            this.FastConnectOptions = fastConnectOptions;

            updateConnectionState(ConnectionState.Connecting);

            try
            {
                // Wait for the socket to connect to the RTC server
                _signalClient.Connect(url, token, this.ConnectOptions, this.RoomOptions);
                this.Log("Waiting for engine to connect...");
                await UniTask.WaitUntil(() => _signalClient.IsConnected).Timeout(TimeSpan.FromSeconds(5));
                this.Log("Waiting for join response...");
                // Wait for join response
                await signalListener.WaitFor<SignalJoinResponseEvent>(
                    filter: null,
                    duration: this.ConnectOptions.Timeouts.Connection,
                    onTimeout: () => throw new ConnectException("Timed out waiting for SignalJoinResponseEvent"));

                this.Log("Waiting for rtc to connect...");

                // Wait until the engine is connected
                await events.WaitFor<EnginePeerStateUpdatedEvent>(
                    filter: (e) => e.IsPrimary && e.State == RTCPeerConnectionState.Connected,
                    duration: this.ConnectOptions.Timeouts.Connection,
                    onTimeout: () => throw new ConnectException("Timed out waiting for EnginePeerStateUpdatedEvent"));

                updateConnectionState(ConnectionState.Connected);
                this.Log("Engine Connected...");
            }
            catch (Exception error)
            {
                this.Log("Connect Error: " + error);
                updateConnectionState(ConnectionState.Disconnected);
                throw;
            }
        }

        public async UniTask<TrackInfo> AddTrack(
            string cid, string name,
            TrackType kind, TrackSource source,
            VideoDimensions dimensions,
            bool dtx, List<VideoLayer> videoLayers,
            List<SimulcastCodec> simulcastCodecs, string sid
        )
        {
            // TODO: Check if cid already published

            EncryptionType encryptionType = EncryptionType.None;
            if (RoomOptions.E2eeOptions != null)
            {
                switch (RoomOptions.E2eeOptions.encryptionType)
                {
                    case EncryptionType.None:
                        encryptionType = EncryptionType.None;
                        break;
                    case EncryptionType.Gcm:
                        encryptionType = EncryptionType.Gcm;
                        break;
                    case EncryptionType.Custom:
                        encryptionType = EncryptionType.Custom;
                        break;
                }
            }

            // Send request to add track
            await _signalClient.SendAddTrack(
                cid: cid,
                name: name,
                type: kind,
                source: source,
                dimensions: dimensions,
                dtx: dtx,
                videoLayers: videoLayers,
                encryptionType: encryptionType,
                simulcastCodecs: simulcastCodecs,
                sid: sid
            );

            // Wait for response, or timeout
            var e = await signalListener.WaitFor<SignalLocalTrackPublishedEvent>(
                filter: (x) => x.Cid == cid,
                duration: ConnectOptions.Timeouts.Publish,
                onTimeout:
                () => throw new TrackPublishException()
            );

            return e.Track;
        }

        public async UniTask Negotiate(float delay = 0.1f)
        {
            if (publisher == null)
            {
                return;
            }

            _hasPublished = true;
            try
            {
                await publisher.NegotiateDebounce(delay);
            }
            catch (Exception error)
            {
                if (error is NegotiationError)
                {
                    fullReconnect = true;
                }

                await handleDisconnect(ClientDisconnectReason.NegotiationFailed);
            }
        }

        public async UniTask SendDataPacket(DataPacket packet)
        {
            var reliability = packet.Kind.ToSDKType();

            var message = packet.ToByteArray();
            if (_subscriberPrimary)
            {
                // Make sure publisher transport is connected
                var connectionState = publisher?.pc.ConnectionState;
                if (connectionState != RTCPeerConnectionState.Connected)
                {
                    this.Log("Publisher is not connected...");

                    // Start negotiation
                    if (publisher?.pc.ConnectionState != RTCPeerConnectionState.Connecting)
                    {
                        await Negotiate();
                    }

                    this.Log("Waiting for publisher to ice-connect...");
                    await events.WaitFor<EnginePublisherPeerStateUpdatedEvent>(
                        eventObj => eventObj.State.IsConnected(),
                        ConnectOptions.Timeouts.PeerConnection
                    );
                }

                // Wait for data channel to open (if not already)
                var dataChannelState = publisherDataChannelState(packet.Kind.ToSDKType());
                if (dataChannelState != RTCDataChannelState.Open)
                {
                    this.Log($"Waiting for data channel {reliability} to open...");
                    await events.WaitFor<PublisherDataChannelStateUpdatedEvent>(
                        eventObj => eventObj.Type == reliability,
                        ConnectOptions.Timeouts.Connection
                    );
                }
            }

            // Choose data channel
            var channel = publisherDataChannel(reliability);

            if (channel == null)
            {
                throw new UnexpectedStateException($"Data channel for {packet.Kind.ToSDKType()} is null");
            }

            this.Log($"sendDataPacket(label: {channel.Label})");
            channel.Send(message);
        }

        public RTCConfiguration BuildRtcConfiguration(ClientConfigSetting serverResponseForceRelay
            , List<RTCIceServer> serverProvidedIceServers)
        {
            var rtcConfiguration = ConnectOptions.RtcConfiguration;

            // The server provided iceServers are only used if
            // the client's iceServers are not set.
            if (rtcConfiguration.iceServers == null && serverProvidedIceServers.Count > 0)
            {
                rtcConfiguration = new RTCConfiguration { iceServers = serverProvidedIceServers.ToArray() };
            }

            // Set forceRelay if server response is enabled
            if (serverResponseForceRelay == ClientConfigSetting.Enabled)
            {
                rtcConfiguration.iceTransportPolicy = RTCIceTransportPolicy.Relay;
            }

            // if (Application.platform == RuntimePlatform.WebGLPlayer && RoomOptions.E2eeOptions != null)
            // {
            // rtcConfiguration.EncodedInsertableStreams = true;
            // }

            return rtcConfiguration;
        }

        public async UniTask CreatePeerConnections(RTCConfiguration rtcConfiguration)
        {
            this.Log("Creating peer connections...");
            publisher = await Transport.Create(_peerConnectionCreate, rtcConfiguration, ConnectOptions);
            subscriber = await Transport.Create(_peerConnectionCreate, rtcConfiguration, ConnectOptions);

            publisher.pc.OnIceCandidate = async (candidate) =>
            {
                this.Log("publisher onIceCandidate");
                await _signalClient.SendIceCandidate(candidate, SignalTarget.Publisher);
            };

            publisher.pc.OnIceConnectionChange = async (state) =>
            {
                this.Log($"publisher iceConnectionState: {state}");
                if (state == RTCIceConnectionState.Connected)
                {
                    await handleGettingConnectedServerAddress(publisher.pc);
                }
            };

            subscriber.pc.OnIceCandidate = async (candidate) =>
            {
                this.Log("subscriber onIceCandidate");
                await _signalClient.SendIceCandidate(candidate, SignalTarget.Subscriber);
            };

            subscriber.pc.OnIceConnectionChange = async (state) =>
            {
                this.Log($"subscriber iceConnectionState: {state}");
                if (state == RTCIceConnectionState.Connected)
                {
                    await handleGettingConnectedServerAddress(subscriber.pc);
                }
            };

            publisher.onOffer = async (offer) =>
            {
                this.Log("publisher onOffer");
                await _signalClient.SendOffer(offer);
            };

            // In subscriber primary mode, server-side opens sub data channels.
            if (_subscriberPrimary)
            {
                subscriber.pc.OnDataChannel = onDataChannel;
            }

            subscriber.pc.OnConnectionStateChange = (state) =>
            {
                events.Emit(new EngineSubscriberPeerStateUpdatedEvent(state, _subscriberPrimary));
            };

            publisher.pc.OnConnectionStateChange = (state) =>
            {
                events.Emit(new EnginePublisherPeerStateUpdatedEvent(state, !_subscriberPrimary));
            };

            events.On<EnginePeerStateUpdatedEvent>(async (eventObj) =>
            {
                if (eventObj.State.IsDisconnectedOrFailed())
                {
                    await handleDisconnect(ClientDisconnectReason.Reconnect);
                }
                else if (eventObj.State.IsClosed())
                {
                    await handleDisconnect(ClientDisconnectReason.PeerConnectionClosed);
                }
            });


            // subscriber.pc.OnRemoveTrack = r => { this.Log($"[WebRTC] pc.onRemoveTrack {r} {r.Track}"); };
            // subscriber.pc.OnTrack = e => { subscriber.AddTrack(e.Track); };

            subscriber.pc.OnTrack = e =>
            {
                // subscriber.AddTrack(e.Track);
                var stream = e.Streams.FirstOrDefault();
                if (stream == null)
                {
                    this.LogWarning($"[WebRTC] pc.onTrack {e.Track} has no stream");
                    return;
                }

                this.Log($"[WebRTC] stream.OnAddTrack {e.Track}");
                switch (ConnectionState)
                {
                    case ConnectionState.Reconnecting or ConnectionState.Connecting:
                    {
                        var track = e.Track;
                        var transceiver = e.Transceiver;
                        events.On<EngineConnectionStateUpdatedEvent>(async _ =>
                        {
                            await UniTask.Delay(10);
                            events.Emit(new EngineTrackAddedEvent(track, stream, transceiver));
                        });
                        return;
                    }
                    case ConnectionState.Disconnected:
                        this.LogWarning("Skipping incoming track after Room disconnected");
                        return;
                    default:
                        events.Emit(new EngineTrackAddedEvent(e.Track, stream, e.Transceiver));
                        break;
                }
            };

            // Doesn't get called reliably, doesn't work on Mac
            // subscriber.pc.onRemoveTrack = (stream, track) => { this.Log($"[WebRTC] {track.Id} pc.onRemoveTrack"); };

            // Also handle messages over the pub channel, for backward compatibility
            try
            {
                var lossyInit = new RTCDataChannelInit
                {
                    ordered = true, maxRetransmits = 0,
                    // protocol = "binary", 
                };
                _lossyDCPub = publisher.pc.CreateDataChannel(_lossyDCLabel, lossyInit);
                _lossyDCPub.OnMessage = onDCMessage;
                _lossyDCPub.OnOpen = () =>
                {
                    events.Emit(new PublisherDataChannelStateUpdatedEvent(!_subscriberPrimary,
                        Reliability.Lossy, _lossyDCPub.ReadyState));
                };
                _lossyDCPub.OnClose = () =>
                {
                    events.Emit(new PublisherDataChannelStateUpdatedEvent(!_subscriberPrimary,
                        Reliability.Lossy, _lossyDCPub.ReadyState));
                };
            }
            catch (Exception ex)
            {
                this.LogError($"CreateDataChannel() threw an exception: {ex}");
            }

            try
            {
                var reliableInit = new RTCDataChannelInit
                {
                    ordered = true,
                    // protocol = "binary", 
                };
                _reliableDCPub = publisher.pc.CreateDataChannel(_reliableDCLabel, reliableInit);
                _reliableDCPub.OnMessage = onDCMessage;
                _reliableDCPub.OnOpen = () =>
                {
                    events.Emit(new PublisherDataChannelStateUpdatedEvent(!_subscriberPrimary,
                        Reliability.Reliable, _reliableDCPub.ReadyState));
                };
                _reliableDCPub.OnClose = () =>
                {
                    events.Emit(new PublisherDataChannelStateUpdatedEvent(!_subscriberPrimary,
                        Reliability.Reliable, _reliableDCPub.ReadyState));
                };
            }
            catch (Exception ex)
            {
                this.LogError($" CreateDataChannel() threw an exception: {ex}");
            }
        }


        private void onDataChannel(RTCDataChannel dc)
        {
            switch (dc.Label)
            {
                case _reliableDCLabel:
                    this.Log($"Server opened DC label: {dc.Label}");
                    _reliableDCSub = dc;
                    _reliableDCSub.OnMessage = onDCMessage;

                    _reliableDCSub.OnOpen = () =>
                    {
                        _reliableDCPub.OnOpen = () =>
                        {
                            events.Emit(new SubscriberDataChannelStateUpdatedEvent(
                                _subscriberPrimary,
                                Reliability.Reliable,
                                _reliableDCPub.ReadyState));
                        };
                        _reliableDCPub.OnClose = () =>
                        {
                            events.Emit(new SubscriberDataChannelStateUpdatedEvent(
                                _subscriberPrimary,
                                Reliability.Reliable,
                                _reliableDCPub.ReadyState));
                        };
                    };

                    _reliableDCSub.OnClose = () =>
                    {
                        _reliableDCPub.OnOpen = () =>
                        {
                            events.Emit(new SubscriberDataChannelStateUpdatedEvent(
                                _subscriberPrimary,
                                Reliability.Reliable,
                                _reliableDCPub.ReadyState));
                        };
                        _reliableDCPub.OnClose = () =>
                        {
                            events.Emit(new SubscriberDataChannelStateUpdatedEvent(
                                _subscriberPrimary,
                                Reliability.Reliable,
                                _reliableDCPub.ReadyState));
                        };
                    };
                    break;
                case _lossyDCLabel:
                    this.Log($"Server opened DC label: {dc.Label}");
                    _lossyDCSub = dc;
                    _lossyDCSub.OnMessage = onDCMessage;

                    _lossyDCSub.OnOpen = () =>
                    {
                        _reliableDCPub.OnOpen = () =>
                        {
                            events.Emit(new SubscriberDataChannelStateUpdatedEvent(
                                _subscriberPrimary,
                                Reliability.Lossy,
                                _reliableDCPub.ReadyState));
                        };
                        _reliableDCPub.OnClose = () =>
                        {
                            events.Emit(new SubscriberDataChannelStateUpdatedEvent(
                                _subscriberPrimary,
                                Reliability.Lossy,
                                _reliableDCPub.ReadyState));
                        };
                    };
                    _lossyDCSub.OnClose = () =>
                    {
                        _reliableDCPub.OnOpen = () =>
                        {
                            events.Emit(new SubscriberDataChannelStateUpdatedEvent(
                                _subscriberPrimary,
                                Reliability.Lossy,
                                _reliableDCPub.ReadyState));
                        };
                        _reliableDCPub.OnClose = () =>
                        {
                            events.Emit(new SubscriberDataChannelStateUpdatedEvent(
                                _subscriberPrimary,
                                Reliability.Lossy,
                                _reliableDCPub.ReadyState));
                        };
                    };
                    break;
                default:
                    this.LogWarning($"Unknown DC label: {dc.Label}");
                    break;
            }
        }

        private async UniTask handleGettingConnectedServerAddress(RTCPeerConnection pc)
        {
            try
            {
                var remoteAddress = await getConnectedAddress(publisher?.pc);
                this.Log($"Connected address: {remoteAddress}");
                if (_connectedServerAddress == null || !_connectedServerAddress.Equals(remoteAddress))
                {
                    _connectedServerAddress = remoteAddress;
                }
            }
            catch (Exception e)
            {
                this.LogWarning($"Could not get connected server address: {e.ToString()}");
            }
        }

        private void onDCMessage(byte[] data)
        {
            DataPacket dp = DataPacket.Parser.ParseFrom(data);

            // Check the type of the packet
            switch (dp.ValueCase)
            {
                case DataPacket.ValueOneofCase.Speaker:
                    // Speaker packet
                    events.Emit(new EngineActiveSpeakersUpdateEvent(
                        speakers: dp.Speaker.Speakers.ToList()
                    ));
                    break;
                case DataPacket.ValueOneofCase.User:
                    // User packet
                    events.Emit(new EngineDataPacketReceivedEvent(
                        packet: dp.User,
                        kind: dp.Kind
                    ));
                    break;
            }
        }


        private async UniTask<string> getConnectedAddress(RTCPeerConnection pc)
        {
            return "<Not Implemented>";
            throw new NotImplementedException();
            this.LogError("Not Implemented");
            return "<Not Implemented>";
        }

        public async UniTask handleDisconnect(ClientDisconnectReason reason)
        {
            this.LogWarning($"onDisconnected state: {_connectionState} reason: {reason}");

            bool fullReconnect = false;
            if (!fullReconnect)
            {
                fullReconnect =
                    (_clientConfiguration?.ResumeConnection == ClientConfigSetting.Disabled)
                    || (reason == ClientDisconnectReason.NegotiationFailed)
                    || (reason == ClientDisconnectReason.PeerConnectionClosed)
                    || (reason == ClientDisconnectReason.LeaveReconnect);
            }

            if (_restarting ||
                (_connectionState == ConnectionState.Reconnecting && !fullReconnect))
            {
                this.Log("Already reconnecting...");
                return;
            }

            if (_connectionState == ConnectionState.Disconnected)
            {
                this.Log("Already disconnected... $reason");
                return;
            }

            this.Log("Should attempt reconnect sequence...");
            if (fullReconnect)
            {
                await restartConnection();
            }
            else
            {
                await resumeConnection();
            }
        }

        private async UniTask resumeConnection()
        {
            if (ConnectionState == ConnectionState.Disconnected)
            {
                this.Log("ResumeConnection: Already closed.");
                return;
            }

            if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(token))
            {
                throw new ConnectException("Could not resume connection without URL and token");
            }

            async UniTask<bool> Sequence()
            {
                try
                {
                    _signalClient.Connect(
                        url,
                        token,
                        ConnectOptions,
                        RoomOptions,
                        true,
                        _participantSid);
                    await UniTask.WaitUntil(() => _signalClient.IsConnected).Timeout(TimeSpan.FromSeconds(3));
                    await signalListener.WaitFor<SignalReconnectResponseEvent>(
                        filter: null,
                        duration: this.ConnectOptions.Timeouts.Connection,
                        onTimeout: () =>
                            throw new ConnectException("Timed out waiting for SignalReconnectResponseEvent"));

                    if (publisher == null || subscriber == null)
                    {
                        throw new UnexpectedStateException("Publisher or subscribers are null");
                    }

                    subscriber.restartingIce = true;

                    if (_hasPublished)
                    {
                        this.Log("ResumeConnection: Negotiating publisher...");
                        await publisher.CreateAndSendOffer(new RTCOfferOptions(true));
                    }

                    var iceConnected = Primary.pc.ConnectionState == RTCPeerConnectionState.Connected;

                    this.Log($"ResumeConnection: IceConnected: {iceConnected}");

                    if (!iceConnected)
                    {
                        this.Log("ResumeConnection: Waiting for primary to connect...");

                        await events.WaitFor<EnginePeerStateUpdatedEvent>(
                            filter: (e) => e.IsPrimary
                                           && e.State == RTCPeerConnectionState.Connected,
                            duration: ConnectOptions.Timeouts.IceRestart,
                            onTimeout: () => throw new ConnectException());
                    }

                    return true;
                }
                catch (Exception e)
                {
                    this.LogException(e);
                    return false;
                }
            }

            try
            {
                updateConnectionState(ConnectionState.Reconnecting);
                var successReconnect = false;
                for (int i = 0; i < 3; i++)
                {
                    this.Log($"Retrying connect sequence, remaining {i} tries...");
                    successReconnect = await Sequence();
                    if (successReconnect)
                    {
                        break;
                    }
                }

                updateConnectionState(successReconnect ? ConnectionState.Connected : ConnectionState.Disconnected);
            }
            catch (Exception error)
            {
                updateConnectionState(ConnectionState.Disconnected);
            }
        }

        public async UniTask restartConnection(bool signalEvents = false)
        {
            if (_restarting)
            {
                this.Log("RestartConnection: Already restarting...");
                return;
            }

            _restarting = true;

            publisher?.Dispose();
            publisher = null;

            subscriber?.Dispose();
            subscriber = null;

            _reliableDCSub = null;
            _reliableDCPub = null;
            _lossyDCSub = null;
            _lossyDCPub = null;

            await signalListener.CancelAll();

            signalListener = _signalClient.CreateListener(synchronized: true);
            setUpSignalListeners();

            await Connect(
                url,
                token,
                ConnectOptions,
                RoomOptions,
                FastConnectOptions);

            if (_hasPublished)
            {
                await Negotiate();
                this.Log("RestartConnection: Waiting for publisher to ice-connect...");

                await events.WaitFor<EnginePublisherPeerStateUpdatedEvent>(
                    filter: (e) => e.State.IsConnected(),
                    duration: ConnectOptions.Timeouts.PeerConnection);
            }

            fullReconnect = false;
            _restarting = false;
        }

        public async UniTask SendSyncState(UpdateSubscription subscription,
            List<TrackPublishedResponse> publishTracks)
        {
            try
            {
                // Get the local description from the subscriber
                var answer = subscriber.pc.LocalDescription;
                // Convert the local description to a Protobuf object
                SessionDescription pbAnswer = answer.ToPBType();

                // Send the sync state to the signal client
                await _signalClient.SendSyncState(
                    pbAnswer,
                    subscription,
                    publishTracks,
                    dataChannelInfo: dataChannelInfo()
                );
            }
            catch (Exception e)
            {
                this.LogException(e);
            }
        }

        private void setUpEngineListeners()
        {
            events.On<EngineConnectionStateUpdatedEvent>(async (e) =>
            {
                if (e.DidReconnect)
                {
                    // Send queued requests if engine reconnected
                    await _signalClient.SendQueuedRequests();
                }
            });
        }

        private async void onSignalEvents(ISignalEvent eventObj)
        {
            if (eventObj == null)
            {
                return;
            }

            switch (eventObj)
            {
                case SignalJoinResponseEvent joinResponse:
                {
                    // create peer connections
                    _subscriberPrimary = joinResponse.Response.SubscriberPrimary;
                    _participantSid = joinResponse.Response.Participant.Sid;
                    var iceServersFromServer = joinResponse.Response.IceServers
                        .Select(e => e.ToSDKType())
                        .ToList();

                    if (iceServersFromServer.Any())
                    {
                        _serverProvidedIceServers = iceServersFromServer;
                    }

                    _clientConfiguration = joinResponse.Response.ClientConfiguration;

                    this.Log($"Has Join Response subscriberPrimary: {_subscriberPrimary}, " +
                             $"serverVersion: {joinResponse.Response.ServerVersion}, ");

                    var forceRelay = ClientConfigSetting.Unset;
                    if (joinResponse.Response.ClientConfiguration != null)
                    {
                        forceRelay = joinResponse.Response.ClientConfiguration.ForceRelay;
                    }

                    var rtcConfiguration = BuildRtcConfiguration(forceRelay, _serverProvidedIceServers);

                    if (publisher == null && subscriber == null)
                    {
                        await CreatePeerConnections(rtcConfiguration);
                    }

                    if (!_subscriberPrimary)
                    {
                        // For subscriberPrimary, we negotiate when necessary (lazy)
                        await Negotiate();
                    }
                }
                    break;
                case SignalReconnectResponseEvent reconnectResponse:
                {
                    var iceServersFromServer = reconnectResponse.Response.IceServers
                        .Select(e => e.ToSDKType())
                        .ToList();

                    if (iceServersFromServer.Any())
                    {
                        _serverProvidedIceServers = iceServersFromServer;
                    }

                    _clientConfiguration = reconnectResponse.Response.ClientConfiguration;

                    this.Log($"Handle ReconnectResponse: {reconnectResponse.Response}");

                    var rtcConfiguration = BuildRtcConfiguration(
                        _clientConfiguration?.ForceRelay ?? ClientConfigSetting.Unset
                        , _serverProvidedIceServers);

                    publisher.pc.SetConfiguration(ref rtcConfiguration);
                    subscriber.pc.SetConfiguration(ref rtcConfiguration);

                    if (!_subscriberPrimary)
                    {
                        await Negotiate();
                    }
                }
                    break;
                case SignalConnectionStateUpdatedEvent connectionStateUpdated:
                {
                    if (connectionStateUpdated.NewState == ConnectionState.Disconnected)
                    {
                        await handleDisconnect(ClientDisconnectReason.Signal);
                    }
                }
                    break;
                case SignalOfferEvent offerEvent:
                {
                    if (subscriber == null)
                    {
                        this.LogWarning($"subscriber is null");
                        return;
                    }

                    var signalingState = subscriber.pc.SignalingState;
                    this.Log($"Received server offer(type: {offerEvent.Sd.type} - {signalingState})");
                    this.Log($"sdp: {offerEvent.Sd.sdp}");

                    await subscriber.SetRemoteDescription(offerEvent.Sd);

                    try
                    {
                        var answer = await subscriber.CreatAnswerAsync();
                        // var answer = await subscriber.pc.CreateAnswer();
                        this.Log($"Created answer");
                        this.Log($"sdp: {answer.sdp}");
                        await subscriber.pc.SetLocalDescription(ref answer);
                        await _signalClient.SendAnswer(answer);
                    }
                    catch (Exception ex)
                    {
                        this.LogError($"Failed to createAnswer(): {ex}");
                    }
                }
                    break;
                case SignalAnswerEvent answerEvent:
                {
                    if (publisher == null)
                    {
                        return;
                    }

                    this.Log($"Received answer (type: {answerEvent.Sd.type})");
                    this.Log($"sdp: {answerEvent.Sd.sdp}");
                    await publisher.SetRemoteDescription(answerEvent.Sd);
                }
                    break;
                case SignalTrickleEvent trickleEvent:
                {
                    if (publisher == null || subscriber == null)
                    {
                        this.LogWarning(
                            $"Received {nameof(SignalTrickleEvent)} but publisher or subscriber was null.");
                        return;
                    }

                    this.Log("Got ICE candidate from peer");

                    if (trickleEvent.Target == SignalTarget.Subscriber)
                    {
                        await subscriber.AddIceCandidate(trickleEvent.Candidate);
                    }
                    else if (trickleEvent.Target == SignalTarget.Publisher)
                    {
                        await publisher.AddIceCandidate(trickleEvent.Candidate);
                    }
                }
                    break;
                case SignalTokenUpdatedEvent tokenUpdatedEvent:
                {
                    this.Log("Server refreshed the token");
                    token = tokenUpdatedEvent.Token;
                }
                    break;
                case SignalLeaveEvent leaveEvent:
                {
                    if (leaveEvent.CanReconnect)
                    {
                        fullReconnect = true;
                        // Reconnect immediately instead of waiting for the next attempt
                        _connectionState = ConnectionState.Reconnecting;
                        updateConnectionState(ConnectionState.Reconnecting);
                        await handleDisconnect(ClientDisconnectReason.LeaveReconnect);
                    }
                    else
                    {
                        if (_connectionState == ConnectionState.Reconnecting)
                        {
                            this.LogWarning("[Signal] Received Leave while engine is reconnecting, ignoring...");
                            return;
                        }

                        updateConnectionState(ConnectionState.Disconnected,
                            reason: leaveEvent.Reason.ToSDKType());

                        await CleanUp();
                    }
                }
                    break;
            }
        }

        private void setUpSignalListeners()
        {
            signalListener.On<ISignalEvent>(onSignalEvents);
        }


        private RTCDataChannelState publisherDataChannelState(Reliability reliability)
        {
            return publisherDataChannel(reliability)?.ReadyState ?? RTCDataChannelState.Closed;
        }

        private RTCDataChannel publisherDataChannel(Reliability reliability)
        {
            return reliability == Reliability.Reliable ? _reliableDCPub : _lossyDCPub;
        }


        private void updateConnectionState(ConnectionState newValue, DisconnectReason? reason = null)
        {
            if (_connectionState == newValue) return;

            this.Log($"Engine ConnectionState {_connectionState} -> {newValue}");

            bool didReconnect = _connectionState == ConnectionState.Reconnecting &&
                                newValue == ConnectionState.Connected;
            // update internal value
            var oldState = _connectionState;
            _connectionState = newValue;

            events.Emit(new EngineConnectionStateUpdatedEvent(
                _connectionState, oldState, didReconnect, fullReconnect, reason
            ));
        }

        private List<DataChannelInfo> dataChannelInfo()
        {
            var l = new List<DataChannelInfo>();
            if (_reliableDCPub != null)
            {
                l.Add(_reliableDCPub.ToLKInfoType());
            }

            if (_lossyDCPub != null)
            {
                l.Add(_lossyDCPub.ToLKInfoType());
            }


            return l;
        }

        public async UniTask<RTCRtpSender> CreateSimulcastTransceiverSender(
            LocalVideoTrack track,
            SimulcastTrackInfo simulcastTrack,
            List<RTCRtpEncodingParameters> encodings,
            LocalTrackPublication publication,
            string videoCodec)
        {
            if (publisher == null)
            {
                throw new Exception("publisher is closed");
            }

            RTCRtpTransceiverInit transceiverInit = new RTCRtpTransceiverInit
            {
                direction = RTCRtpTransceiverDirection.SendOnly
            };

            if (encodings != null)
            {
                transceiverInit.sendEncodings = encodings.ToArray();
            }

            var transceiver = publisher.pc.AddTransceiver(
                simulcastTrack.MediaStreamTrack,
                // RTCRtpMediaType.Video,
                transceiverInit
            );

            await SetPreferredCodec(transceiver, track.Kind == TrackType.Audio ? TrackKind.Audio : TrackKind.Video,
                videoCodec);

            return transceiver.Sender;
        }

        public RTCRtpCapabilities GetRtpReceiverCapabilities(TrackKind kind)
        {
            return RTCRtpReceiver.GetCapabilities(kind);
        }

        public RTCRtpCapabilities GetRtpSenderCapabilities(TrackKind kind)
        {
            return RTCRtpSender.GetCapabilities(kind);
        }

        public async UniTask SetPreferredCodec(
            RTCRtpTransceiver transceiver,
            TrackKind kind,
            string videoCodec)
        {
            RTCRtpCapabilities caps = GetRtpSenderCapabilities(kind);
            if (caps.codecs == null) return;

            this.Log("Get capabilities " + caps.codecs);

            List<RTCRtpCodecCapability> matched = new List<RTCRtpCodecCapability>();
            List<RTCRtpCodecCapability> partialMatched = new List<RTCRtpCodecCapability>();
            List<RTCRtpCodecCapability> unmatched = new List<RTCRtpCodecCapability>();

            foreach (RTCRtpCodecCapability c in caps.codecs)
            {
                string codec = c.mimeType.ToLower();
                if (codec == "audio/opus")
                {
                    matched.Add(c);
                    continue;
                }

                bool matchesVideoCodec = codec == "video/" + videoCodec.ToLower();
                if (!matchesVideoCodec)
                {
                    unmatched.Add(c);
                    continue;
                }

                // For h264 codecs that have sdpFmtpLine available, use only if the
                // profile-level-id is 42e01f for cross-browser compatibility
                if (videoCodec.ToLower() == "h264")
                {
                    if (c.sdpFmtpLine != null && c.sdpFmtpLine.Contains("profile-level-id=42e01f"))
                    {
                        matched.Add(c);
                    }
                    else
                    {
                        partialMatched.Add(c);
                    }

                    continue;
                }

                matched.Add(c);
            }

            matched.AddRange(partialMatched);
            matched.AddRange(unmatched);

            transceiver.SetCodecPreferences(matched.ToArray());
        }

        private bool isDisposed = false;

        public async void Dispose()
        {
            isDisposed = true;
            await CleanUp();
            await events.Dispose();
            await signalListener.Dispose();
        }

        public async UniTask CleanUp()
        {
            this.Log($"CleanUp()");

            publisher?.Dispose();
            publisher = null;
            _hasPublished = false;

            subscriber?.Dispose();
            subscriber = null;

            if (_signalClient != null)
                await _signalClient.CleanUp();

            updateConnectionState(ConnectionState.Disconnected);
        }

        public async UniTask Disconnect()
        {
            if (_signalClient != null)
            {
                await _signalClient.SendLeave();
            }

            publisher?.UnPublishAll();
        }
    }
}