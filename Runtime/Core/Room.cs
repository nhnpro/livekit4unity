using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
// //using System.Threading.Tasks;
using LiveKit.Proto;
using LiveKitUnity.Runtime.Debugging;
using LiveKitUnity.Runtime.Events;
using LiveKitUnity.Runtime.Participants;
using LiveKitUnity.Runtime.TrackPublications;
using LiveKitUnity.Runtime.Tracks;
using LiveKitUnity.Runtime.Types;


namespace LiveKitUnity.Runtime.Core
{
    public class Room : EventsEmittable, IDisposable
    {
        // Expose engine's params
        public ConnectionState ConnectionState => engine.ConnectionState;
        public ConnectOptions ConnectOptions => engine.ConnectOptions;
        public RoomOptions RoomOptions => engine.RoomOptions;

        // Map of SID to RemoteParticipant
        public Dictionary<string, RemoteParticipant> Participants => _participants;
        private Dictionary<string, RemoteParticipant> _participants = new();

        // The current participant
        public LocalParticipant LocalParticipant => _localParticipant;
        private LocalParticipant _localParticipant;

        // Name of the room
        public string Name => _name;
        private string _name;

        // SID of the room
        public string Sid => _sid;
        private string _sid;

        // Metadata of the room
        public string Metadata => _metadata;
        private string _metadata;

        // Server version
        public string ServerVersion => _serverVersion;
        private string _serverVersion;

        // Server region
        public string ServerRegion => _serverRegion;
        private string _serverRegion;

        public E2EEManager E2EEManager => _e2eeManager;
        private E2EEManager _e2eeManager;

        public bool IsRecording => _isRecording;
        private bool _isRecording;

        private bool _audioEnabled = true;
        public bool AudioEnabled => _audioEnabled;

        // A list of participants that are actively speaking, including the local participant
        public List<Participant> ActiveSpeakers => _activeSpeakers;
        private List<Participant> _activeSpeakers = new();

        public Engine engine;
        private EventsListener _engineListener;
        private EventsListener _signalListener;
        private IDisposable _appCloseSubscription;

        public Room(Engine engine = null, ConnectOptions connectOptions = null
            , RoomOptions roomOptions = null)
        {
            this.engine = engine ?? new Engine(connectOptions, roomOptions);

            this._engineListener = this.engine.CreateListener(true);
            setUpEngineListeners();

            //TODO: 1B Handle App Close TO Disconnect
            /*if (Application.platform != RuntimePlatform.WebGLPlayer)
            {
                Application.quitting += async () => { await Disconnect(); };
            }*/

            _signalListener = this.engine.Client.CreateListener(true);
            setUpSignalListeners();

            // Any event emitted will trigger ChangeNotifier
            events.Listen(e =>
            {
                this.Log($"{e}, will notifyListeners()");
                notifyListeners();
            });
        }

        public async void Dispose()
        {
            // clean up routine
            await CleanUp();
            // dispose events
            await events.Dispose();
            // dispose local participant
            if (_localParticipant != null)
            {
                await _localParticipant.Dispose();
            }

            if (_signalListener != null)
            {
                await _signalListener.Dispose();
            }

            if (_engineListener != null)
            {
                await _engineListener.Dispose();
            }

            engine?.Dispose();

            // TODO: 1B dispose the app state listener
            // await _appCloseSubscription?.Cancel();
        }


        public async UniTask Connect(
            string url,
            string token,
            ConnectOptions connectOptions = null,
            RoomOptions roomOptions = null,
            FastConnectOptions fastConnectOptions = null)
        {
            if (roomOptions == null)
            {
                roomOptions = this.RoomOptions;
            }

            if (roomOptions.E2eeOptions != null)
            {
                if (!E2EEManager.IsPlatformSupportsE2EE())
                {
                    throw new LiveKitE2EEException("E2EE is not supported on this platform");
                }

                _e2eeManager = new E2EEManager(roomOptions.E2eeOptions.KeyProvider);
                _e2eeManager.Setup(this);
            }

            await engine.Connect(url, token, connectOptions, roomOptions, fastConnectOptions);
        }

        private void setUpSignalListeners()
        {
            _signalListener.On<ISignalEvent>(onSignalEvent);
        }

        private async void onSignalEvent(ISignalEvent evt)
        {
            switch (evt)
            {
                case SignalJoinResponseEvent eventData:
                {
                    _sid = eventData.Response.Room.Sid;
                    _name = eventData.Response.Room.Name;
                    _metadata = eventData.Response.Room.Metadata;
                    _serverVersion = eventData.Response.ServerVersion;
                    _serverRegion = eventData.Response.ServerRegion;

                    if (_isRecording != eventData.Response.Room.ActiveRecording)
                    {
                        _isRecording = eventData.Response.Room.ActiveRecording;
                        emitWhenConnected(new RoomRecordingStatusChanged(_isRecording));
                    }

                    this.Log($"[Engine] Received JoinResponse, serverVersion: {eventData.Response.ServerVersion}");

                    if (_localParticipant == null)
                    {
                        _localParticipant = new LocalParticipant(this, eventData.Response.Participant);
                    }
                    /*else
                    {
                        await _localParticipant.UpdateFromInfo(eventData.Response.Participant);
                    }*/

                    if (engine.FullReconnect)
                    {
                        if (_localParticipant != null)
                        {
                            _localParticipant.UpdateFromInfo(eventData.Response.Participant);
                        }
                    }

                    if (ConnectOptions.ProtocolVersion >= ProtocolVersion.V8 &&
                        engine.FastConnectOptions != null && !engine.FullReconnect)
                    {
                        var options = engine.FastConnectOptions;

                        var audio = options.Microphone;
                        if (audio.Enabled is true)
                        {
                            if (audio.Track != null)
                            {
                                await _localParticipant.PublishAudioTrack(audio.Track,
                                    RoomOptions.DefaultAudioPublishOptions);
                            }
                            else
                            {
                                await _localParticipant.SetMicrophoneEnabled(true,
                                    RoomOptions.DefaultAudioCaptureOptions);
                            }
                        }

                        var video = options.Camera;
                        if (video.Enabled is true)
                        {
                            if (video.Track != null)
                            {
                                await _localParticipant.PublishVideoTrack(video.Track as LocalVideoTrack,
                                    RoomOptions.DefaultVideoPublishOptions);
                            }
                            else
                            {
                                await _localParticipant.SetCameraEnabled(true,
                                    RoomOptions.DefaultCameraCaptureOptions);
                            }
                        }

                        var screen = options.Screen;
                        if (screen.Enabled is true)
                        {
                            if (screen.Track != null)
                            {
                                await _localParticipant.PublishVideoTrack(
                                    screen.Track as LocalVideoTrack,
                                    RoomOptions.DefaultVideoPublishOptions);
                            }
                            else
                            {
                                await _localParticipant.SetScreenShareEnabled(true, false,
                                    RoomOptions.DefaultScreenShareCaptureOptions);
                            }
                        }
                    }

                    foreach (var info in eventData.Response.OtherParticipants)
                    {
                        this.Log($"Creating RemoteParticipant: {info.Sid}({info.Identity})");
                        getOrCreateRemoteParticipant(info.Sid, info);
                    }

                    this.Log("Room Connect completed");
                }
                    break;
                case SignalParticipantUpdateEvent eventData:
                    await onParticipantUpdateEvent(eventData.Participants);
                    break;
                case SignalSpeakersChangedEvent eventData:
                    onSignalSpeakersChangedEvent(eventData.Speakers);
                    break;
                case SignalConnectionQualityUpdateEvent eventData:
                    OnSignalConnectionQualityUpdateEvent(eventData.Updates);
                    break;
                case SignalStreamStateUpdatedEvent eventData:
                    onSignalStreamStateUpdateEvent(eventData.Updates);
                    break;
                case SignalSubscribedQualityUpdatedEvent eventData:
                {
                    if (!RoomOptions.Dynacast || _serverVersion == "0.15.1")
                    {
                        this.Log(
                            "Received subscribed quality update but Dynacast is off or server version is not supported.");
                        return;
                    }

                    TrackPublication pub = null;
                    LocalParticipant?.TrackPublications.TryGetValue(eventData.TrackSid, out pub);
                    if (pub != null && eventData.SubscribedCodecs.Count > 0)
                    {
                        if (pub.Track is LocalVideoTrack videoTrack)
                        {
                            var newCodecs = await videoTrack.SetPublishingCodecs(
                                eventData.SubscribedCodecs, videoTrack);

                            foreach (var codec in newCodecs)
                            {
                                if (IsBackupCodec(codec))
                                {
                                    this.Log("publishing backup codec " + codec + " for " + videoTrack.Sid);
                                    await LocalParticipant.PublishAdditionalCodecForPublication(
                                        pub, codec);
                                }
                            }
                        }
                    }
                    else if (pub != null && eventData.SubscribedQualities.Count > 0)
                    {
                        if (pub.Track is LocalVideoTrack videoTrack)
                        {
                            await videoTrack.UpdatePublishingLayers(videoTrack, eventData.SubscribedQualities);
                        }
                    }
                }
                    break;
                case SignalSubscriptionPermissionUpdateEvent eventData:
                {
                    this.Log("SignalSubscriptionPermissionUpdateEvent " +
                             "participantSid:" + eventData.ParticipantSid + " " +
                             "trackSid:" + eventData.TrackSid + " " +
                             "allowed:" + eventData.Allowed);

                    var participant = _participants.GetValueOrDefault(eventData.ParticipantSid);
                    var publication = participant?.TrackPublications.GetValueOrDefault(eventData.TrackSid);
                    if (publication != null)
                    {
                        if (!eventData.Allowed)
                        {
                            await participant.UnpublishTrack(publication.Sid);
                        }

                        await publication.UpdateSubscriptionAllowed(eventData.Allowed);
                        emitWhenConnected(new TrackSubscriptionPermissionChangedEvent(
                            participant, publication as RemoteTrackPublication, publication.SubscriptionState
                        ));
                    }
                }
                    break;
                case SignalRoomUpdateEvent eventData:
                {
                    _metadata = eventData.Room.Metadata;
                    emitWhenConnected(new RoomMetadataChangedEvent(metadata: eventData.Room.Metadata));
                    if (_isRecording != eventData.Room.ActiveRecording)
                    {
                        _isRecording = eventData.Room.ActiveRecording;
                        emitWhenConnected(new RoomRecordingStatusChanged(activeRecording: _isRecording));
                    }
                }
                    break;
                case SignalConnectionStateUpdatedEvent eventData:
                {
                    if (eventData.NewState == ConnectionState.Reconnecting)
                    {
                        this.Log("Sending syncState");
                        await SendSyncState();
                    }
                }
                    break;
                case SignalRemoteMuteTrackEvent eventData:
                {
                    var publication = _localParticipant?.TrackPublications.GetValueOrDefault(eventData.Sid);
                    if (publication != null)
                    {
                        if (eventData.Muted)
                        {
                            await publication.Mute();
                        }
                        else
                        {
                            await publication.Unmute();
                        }
                    }
                }
                    break;
                case SignalTrackUnpublishedEvent eventData:
                {
                    if (_localParticipant != null)
                    {
                        await _localParticipant.UnpublishTrack(eventData.TrackSid);
                    }
                }
                    break;
            }
        }


        private void setUpEngineListeners()
        {
            _engineListener.On<IEngineEvent>(onEngineEvent);
        }

        private async void onEngineEvent(IEngineEvent evt)
        {
            switch (evt)
            {
                case EngineConnectionStateUpdatedEvent eventData:
                {
                    if (eventData.NewState == ConnectionState.Connected)
                    {
                        events.Emit(new RoomConnectedEvent());
                    }

                    if (eventData.DidReconnect)
                    {
                        events.Emit(new RoomReconnectedEvent());
                        // Re-send track permissions
                        if (_localParticipant != null)
                        {
                            await _localParticipant.SendTrackSubscriptionPermissions();
                        }
                    }
                    else if (eventData.FullReconnect && eventData.NewState == ConnectionState.Connecting)
                    {
                        events.Emit(new RoomRestartingEvent());
                        // Clean up RemoteParticipants
                        foreach (var participant in _participants.Values)
                        {
                            events.Emit(new ParticipantDisconnectedEvent(participant: participant));
                            await participant.Dispose();
                        }

                        _participants.Clear();
                        _activeSpeakers.Clear();
                        // Reset parameters
                        _name = null;
                        _sid = null;
                        _metadata = null;
                        _serverVersion = null;
                        _serverRegion = null;
                    }
                    else if (eventData.FullReconnect && eventData.NewState == ConnectionState.Connected)
                    {
                        events.Emit(new RoomRestartedEvent());
                        await HandlePostReconnect(eventData.FullReconnect);
                    }
                    else
                        switch (eventData.NewState)
                        {
                            case ConnectionState.Reconnecting:
                                events.Emit(new RoomReconnectingEvent());
                                break;
                            case ConnectionState.Disconnected:
                            {
                                if (!eventData.FullReconnect)
                                {
                                    await CleanUp();
                                    events.Emit(new RoomDisconnectedEvent(reason: eventData.DisconnectReason));
                                }

                                break;
                            }
                        }

                    // Always notify ChangeNotifier
                    notifyListeners();
                }
                    break;
                case EngineActiveSpeakersUpdateEvent eventData:
                    onEngineActiveSpeakersUpdateEvent(eventData.Speakers);
                    break;
                case EngineDataPacketReceivedEvent eventData:
                    onDataMessageEvent(eventData);
                    break;
                case AudioPlaybackStarted evenData:
                    handleAudioPlaybackStarted(evenData);
                    break;
                case AudioPlaybackFailed evenData:
                    handleAudioPlaybackFailed(evenData);
                    break;
                case EngineTrackAddedEvent eventData:
                {
                    this.Log($"EngineTrackAddedEvent trackSid:{eventData.Track.Id} vs {eventData.Stream.Id}");

                    var idParts = eventData.Stream.Id.Split('|');
                    var participantSid = idParts[0];
                    var trackSid = idParts.ElementAtOrDefault(1) ?? eventData.Track.Id;
                    var participant = getOrCreateRemoteParticipant(participantSid, null);
                    try
                    {
                        if (string.IsNullOrEmpty(trackSid))
                        {
                            throw new Exception("tracksid null when addtrack");
                        }

                        await participant.AddSubscribedMediaTrack(
                            eventData.Track,
                            eventData.Stream,
                            trackSid,
                            eventData.Transceiver.Receiver,
                            audioOutputOptions: RoomOptions.DefaultAudioOutputOptions
                        );
                    }
                    catch (Exception exceptionEvent)
                    {
                        this.LogError("AddSubscribedMediaTrack() threw " + exceptionEvent);
                        // events.Emit(exceptionEvent);
                    }
                }
                    break;
            }
        }

        private bool CanPlaybackAudio => _audioEnabled;

        private void handleAudioPlaybackStarted(AudioPlaybackStarted evenData)
        {
            if (CanPlaybackAudio)
            {
                return;
            }

            _audioEnabled = true;
            events.Emit(new AudioPlaybackStatusChanged(true));
        }

        private void handleAudioPlaybackFailed(AudioPlaybackFailed evenData)
        {
            if (!CanPlaybackAudio)
            {
                return;
            }

            _audioEnabled = false;
            events.Emit(new AudioPlaybackStatusChanged(false));
        }

        private void onDataMessageEvent(EngineDataPacketReceivedEvent dataPacketEvent)
        {
            // Participant may be null if data is sent from Server-API
            string senderSid = dataPacketEvent.Packet.ParticipantSid;
            RemoteParticipant senderParticipant = null;

            if (!string.IsNullOrEmpty(senderSid))
            {
                if (Participants.TryGetValue(senderSid, out senderParticipant))
                {
                    // Handle data received
                    // senderParticipant.Delegate?.OnDataReceived(senderParticipant, dataPacketEvent.Packet.Payload);

                    var eventData = new DataReceivedEvent(
                        senderParticipant, dataPacketEvent.Packet.Payload.ToByteArray(), dataPacketEvent.Packet.Topic);

                    senderParticipant.events.Emit(eventData);
                    events.Emit(eventData);
                }
            }
        }

        private void onEngineActiveSpeakersUpdateEvent(List<SpeakerInfo> speakers)
        {
            List<Participant> activeSpeakers = new List<Participant>();
            // Local participant & remote participants
            Dictionary<string, Participant> allParticipants = new Dictionary<string, Participant>();

            if (_localParticipant != null)
            {
                allParticipants[_localParticipant.Sid] = _localParticipant;
            }

            foreach (var participant in _participants.Values)
            {
                allParticipants[participant.Sid] = participant;
            }

            foreach (var speaker in speakers)
            {
                if (allParticipants.TryGetValue(speaker.Sid, out var p))
                {
                    p.AudioLevel = speaker.Level;
                    p.IsSpeaking = true;
                    activeSpeakers.Add(p);
                }
            }

            // Clear audio levels and speaking flags for participants not in the speakers list
            HashSet<string> speakerSids = new HashSet<string>(speakers.Select(e => e.Sid));
            foreach (var p in allParticipants.Values)
            {
                if (!speakerSids.Contains(p.Sid))
                {
                    p.AudioLevel = 0;
                    p.IsSpeaking = false;
                }
            }

            _activeSpeakers = activeSpeakers;
            emitWhenConnected(new ActiveSpeakersChangedEvent(activeSpeakers));
        }


        private async UniTask HandlePostReconnect(bool isFullReconnect)
        {
            if (isFullReconnect)
            {
                // Re-publish all tracks
                if (_localParticipant != null)
                {
                    await _localParticipant.RePublishAllTracks();
                }
            }

            foreach (var pub in Participants.Values.SelectMany(participant => participant.TrackPublications.Values))
            {
                if (pub is RemoteTrackPublication { Subscribed: true } rp)
                {
                    await rp.SendUpdateTrackSettings();
                }
            }
        }

        public async UniTask Disconnect()
        {
            await engine.Disconnect();
            await CleanUp();
        }


        private RemoteParticipant getOrCreateRemoteParticipant(string sid, ParticipantInfo info)
        {
            RemoteParticipant participant = null;
            if (_participants.TryGetValue(sid, out var participant1))
            {
                participant = participant1;
                if (info != null)
                {
                    participant.UpdateFromInfo(info);
                }
            }
            else
            {
                if (info == null)
                {
                    this.LogWarning("RemoteParticipant.info is null trackSid: " + sid);
                    participant = new RemoteParticipant(this, sid, "", "");
                }
                else
                {
                    participant = new RemoteParticipant(this, info);
                }

                _participants[sid] = participant;
            }

            return participant;
        }

        private async UniTask onParticipantUpdateEvent(List<ParticipantInfo> updates)
        {
            // Trigger change notifier only if the list of participants' membership is changed
            bool hasChanged = false;

            foreach (ParticipantInfo info in updates)
            {
                if (_localParticipant != null && _localParticipant.Sid == info.Sid)
                {
                    _localParticipant.UpdateFromInfo(info);
                    continue;
                }

                if (info.State == ParticipantInfo.Types.State.Disconnected)
                {
                    hasChanged = true;
                    await handleParticipantDisconnect(info.Sid);
                    continue;
                }

                var isNew = !_participants.ContainsKey(info.Sid);
                var participant = getOrCreateRemoteParticipant(info.Sid, info);

                if (isNew)
                {
                    hasChanged = true;
                    // Fire connected event
                    emitWhenConnected(new ParticipantConnectedEvent(participant));
                }
                else
                {
                    participant.UpdateFromInfo(info);
                }
            }

            if (hasChanged)
            {
                notifyListeners();
            }
        }

        private void emitWhenConnected(IRoomEvent roomEvent)
        {
            if (ConnectionState == ConnectionState.Connected)
            {
                events.Emit(roomEvent);
            }
        }

        private void onSignalSpeakersChangedEvent(List<SpeakerInfo> speakers)
        {
            Dictionary<string, Participant> lastSpeakers = new Dictionary<string, Participant>();

            foreach (var p in _activeSpeakers)
            {
                lastSpeakers[p.Sid] = p;
            }

            foreach (SpeakerInfo speaker in speakers)
            {
                Participant p = _participants.TryGetValue(speaker.Sid, out var participant) ? participant : null;
                if (speaker.Sid == _localParticipant?.Sid)
                {
                    p = _localParticipant;
                }

                if (p == null)
                {
                    continue;
                }

                p.AudioLevel = speaker.Level;
                p.IsSpeaking = speaker.Active;
                if (speaker.Active)
                {
                    lastSpeakers[speaker.Sid] = p;
                }
                else
                {
                    lastSpeakers.Remove(speaker.Sid);
                }
            }

            List<Participant> activeSpeakers = lastSpeakers.Values.ToList();
            activeSpeakers.Sort((a, b) => b.AudioLevel.CompareTo(a.AudioLevel));
            _activeSpeakers = activeSpeakers;
            emitWhenConnected(new ActiveSpeakersChangedEvent(activeSpeakers));
        }

        private async void onSignalStreamStateUpdateEvent(List<StreamStateInfo> updates)
        {
            foreach (var update in updates)
            {
                // Try to find RemoteParticipant
                if (_participants.TryGetValue(update.ParticipantSid, out var participant))
                {
                    // Try to find RemoteTrackPublication
                    if (participant.TrackPublications.TryGetValue(update.TrackSid, out var trackPublication))
                    {
                        // Update the stream state
                        trackPublication.UpdateStreamState(update.State);
                        emitWhenConnected(new TrackStreamStateUpdatedEvent(
                            participant, trackPublication as RemoteTrackPublication, update.State.ToLKType()
                        ));
                    }
                }
            }
        }

        private async UniTask SendSyncState()
        {
            bool sendUnSub = ConnectOptions.AutoSubscribe;
            var participantTracks = Participants.Values.Select(e => e.ParticipantTracks());
            var participantTracksEnumerable = participantTracks as ParticipantTracks[] ?? participantTracks.ToArray();
            var updateSub = new UpdateSubscription
            {
                ParticipantTracks = { participantTracksEnumerable },
                Subscribe = !sendUnSub,
                // Deprecated
            };
            updateSub.TrackSids.AddRange(participantTracksEnumerable.SelectMany(e => e.TrackSids));
            await engine.SendSyncState(updateSub, _localParticipant?.PublishedTracksInfo());
        }


        private void OnSignalConnectionQualityUpdateEvent(List<ConnectionQualityInfo> updates)
        {
            foreach (var entry in updates)
            {
                Participant participant = null;

                if (entry.ParticipantSid == LocalParticipant?.Sid)
                {
                    participant = LocalParticipant;
                }
                else
                {
                    _participants.TryGetValue(entry.ParticipantSid, out var remoteP);
                    participant = remoteP;
                }

                // Update the connection quality if the participant is found
                participant?.UpdateConnectionQuality(entry.Quality.ToLKType());
            }
        }

        private async UniTask SetE2EEEnabled(bool enabled)
        {
            if (_e2eeManager != null)
            {
                await _e2eeManager.SetEnabled(enabled);
            }
            else
            {
                throw new LiveKitE2EEException("_e2eeManager not setup!");
            }
        }

        private void notifyListeners()
        {
            this.LogWarning("Notify Listener Not Implemented Yet!");
        }

        private async UniTask CleanUp()
        {
            this.Log("cleanUp()");

            // Clean up RemoteParticipants
            var participants = _participants.Values.ToList();
            foreach (var participant in participants)
            {
                // RemoteParticipant is responsible for disposing resources
                await participant.Dispose();
            }

            _participants.Clear();

            // Clean up LocalParticipant
            if (_localParticipant != null)
            {
                await _localParticipant.UnpublishAllTracks();
            }

            _activeSpeakers.Clear();

            // Clean up engine
            await engine.CleanUp();

            // Reset params
            _name = null;
            _sid = null;
            _metadata = null;
            _serverVersion = null;
            _serverRegion = null;
        }

        public async UniTask SendSimulateScenario(
            int speakerUpdate,
            bool nodeFailure,
            bool migration,
            bool serverLeave,
            bool switchCandidate,
            bool signalReconnect)
        {
            if (signalReconnect)
            {
                await engine.Client.CleanUp();
                return;
            }

            await engine.Client.SendSimulateScenario(
                speakerUpdate: speakerUpdate,
                nodeFailure: nodeFailure,
                migration: migration,
                serverLeave: serverLeave,
                switchCandidate: switchCandidate);
        }

        public async UniTask ApplyAudioSpeakerSettings()
        {
            if (RoomOptions.DefaultAudioOutputOptions.SpeakerOn != null)
            {
                await AudioManager.Instance.SetSpeakerOn(RoomOptions.DefaultAudioOutputOptions.SpeakerOn.Value);
            }
        }

        private bool IsBackupCodec(string codec)
        {
            //'vp8', 'h264'
            return codec.ToLower().Equals("vp8") || codec.ToLower().Equals("h264");
        }

        private async UniTask handleParticipantDisconnect(string sid)
        {
            if (!_participants.TryGetValue(sid, out var participant))
            {
                return;
            }

            _participants.Remove(sid);

            await participant.UnpublishAllTracks(notify: true);
            emitWhenConnected(new ParticipantDisconnectedEvent(participant: participant));
        }
    }
}