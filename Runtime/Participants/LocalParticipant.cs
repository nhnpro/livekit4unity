using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
//using System.Threading.Tasks;
using Google.Protobuf;
using LiveKit.Proto;
using LiveKitUnity.Runtime.Debugging;
using LiveKitUnity.Runtime.Events;
using LiveKitUnity.Runtime.TrackPublications;
using LiveKitUnity.Runtime.Tracks;
using LiveKitUnity.Runtime.Types;
using Room = LiveKitUnity.Runtime.Core.Room;
using TrackSource = LiveKitUnity.Runtime.Types.TrackSource;

namespace LiveKitUnity.Runtime.Participants
{
    public class LocalParticipant : Participant
    {
        public LocalParticipant(Room room, ParticipantInfo info)
            : base(room, info.Sid, info.Identity, info.Name)
        {
            UpdateFromInfo(info);
        }

        public async UniTask<LocalTrackPublication> PublishAudioTrack(
            LocalAudioTrack track, AudioPublishOptions publishOptions = null)
        {
            if (AudioTracks.Any(e => e.Track?.MediaStreamTrack?.Id == track?.MediaStreamTrack?.Id))
            {
                throw new TrackPublishException("Track already exists");
            }

            // SetTrackSubscriptionPermissions(true, new List<ParticipantTrackPermission>());

            // Use defaultPublishOptions if options is null
            publishOptions ??= room.RoomOptions.DefaultAudioPublishOptions;

            this.LogWarning($"PublishAudioTrack {track.Sid}");
            var trackInfo = await room.engine.AddTrack(
                track.GetCid(), publishOptions.Name ?? AudioPublishOptions.DefaultMicrophoneName,
                track.Kind, track.Source.ToPBType(), null, publishOptions.Dtx
                , null, null, null
            );

            await track.Start(this);


            // TR_ACuxo6HzqwVtx
            // AddTransceiver cannot pass in a kind parameter due to a bug in Flutter WebRTC (web)
            track.SenderRaw = room.engine.publisher.AddTrack(track.MediaStreamTrack);

            /*
            var transceiverInit = new RTCRtpTransceiverInit
            {
                direction = RTCRtpTransceiverDirection.SendOnly,
            };

            if (publishOptions.AudioBitrate > 0)
            {
                transceiverInit.sendEncodings = new[]
                {
                    new RTCRtpEncodingParameters
                    {
                        maxBitrate = (ulong?)publishOptions.AudioBitrate
                    }
                };

                this.Log($"Send Audio Track With BitRate {publishOptions.AudioBitrate}");
            }
            track.Transceiver = room.engine.publisher.pc.AddTransceiver(track.MediaStreamTrack, transceiverInit);
           */
            await room.engine.Negotiate();
            var pub = new LocalTrackPublication(this, trackInfo, track);
            AddTrackPublication(pub);

            // Did publish
            await track.OnPublish();
            await room.ApplyAudioSpeakerSettings();
            var newEvent = new LocalTrackPublishedEvent(this, pub);
            events.Emit(newEvent);
            room.events.Emit(newEvent);

            return pub;
        }

        public static bool IsSVCCodec(String codec)
        {
            return codec.ToLower().Equals("vp9") || codec.ToLower().Equals("av1");
        }

        public async UniTask<LocalTrackPublication> PublishVideoTrack(LocalVideoTrack track,
            VideoPublishOptions publishOptions = null)
        {
            throw new NotImplementedException();
            /*
            if (VideoTracks.Any(e => e.Track?.MediaStreamTrack.Id == track.MediaStreamTrack.Id))
            {
                throw new TrackPublishException("Track already exists");
            }

            // Use defaultPublishOptions if options is null
            publishOptions ??= room.RoomOptions.DefaultVideoPublishOptions;

            if (string.Compare(publishOptions.VideoCodec, publishOptions.VideoCodec,
                    StringComparison.OrdinalIgnoreCase) != 0)
            {
                publishOptions = publishOptions.CopyWith(
                    videoCodec: publishOptions.VideoCodec.ToLowerInvariant()
                    // Copy other properties here
                );
            }

            // Handle SVC publishing
            bool isSvc = IsSVCCodec(publishOptions.VideoCodec);
            if (isSvc)
            {
                if (!room.RoomOptions.Dynacast)
                {
                    room.engine.RoomOptions = room.RoomOptions.CopyWith(dynacast: true);
                }

                if (publishOptions.BackupCodec == null)
                {
                    publishOptions = publishOptions.CopyWith(
                        backupCodec: new BackupVideoCodec()
                    );
                }

                if (string.IsNullOrEmpty(publishOptions.ScalabilityMode))
                {
                    publishOptions =  publishOptions.CopyWith(
                        scalabilityMode: "L3T3_KEY"
                        // Copy other properties here
                    );
                }
            }

            // Use constraints passed to getUserMedia by default
            VideoDimensions dimensions = (track.CurrentOptions as VideoCaptureOptions)?.Params.dimensions;

            /*if (Application.isWebPlatform)
            {
                // getSettings() is only implemented for Web
                try
                {
                    // Try to use getSettings for more accurate resolution
                    MediaStreamTrackSettings settings = track.MediaStreamTrack.GetSettings();
                    if (settings.Width is int width)
                    {
                        dimensions = new VideoDimensions(width, dimensions.Height);
                    }

                    if (settings.Height is int height)
                    {
                        dimensions = new VideoDimensions(dimensions.Width, height);
                    }
                }
                catch (Exception)
                {
                    this.LogWarning("Failed to call `mediaStreamTrack.getSettings()`");
                }
            }#1#

            this.Log($"Compute encodings with resolution: {dimensions}, options: {publishOptions}");

            // Video encodings and simulcasts
            RTCRtpEncodingParameters[] encodings = Utils.ComputeVideoEncodings(
                isScreenShare: track.Source == TrackSource.ScreenShareVideo,
                dimensions: dimensions,
                options: publishOptions,
                codec: publishOptions.VideoCodec
            );

            this.Log($"Using encodings: {string.Join(", ", encodings.Select(e => e.ToMap()))}");

            RTCRtpCodecParameters[] simulcastCodecs;

            if (publishOptions.BackupCodec != null &&
                string.Compare(publishOptions.BackupCodec.Codec, publishOptions.VideoCodec,
                    StringComparison.OrdinalIgnoreCase) != 0)
            {
                simulcastCodecs = new RTCRtpCodecParameters[]
                {
                    new RTCRtpCodecParameters
                    {
                        Codec = publishOptions.VideoCodec,
                        Cid = track.GetCid(),
                        EnableSimulcastLayers = true
                    },
                    new rtc.RTCRtpCodecParameters
                    {
                        Codec = publishOptions.BackupCodec.Codec.ToLowerInvariant(),
                        Cid = "",
                        EnableSimulcastLayers = publishOptions.BackupCodec.Simulcast
                    }
                };
            }
            else
            {
                simulcastCodecs = new RTCRtpCodecParameters[]
                {
                    new RTCRtpCodecParameters
                    {
                        Codec = publishOptions.VideoCodec,
                        Cid = track.GetCid(),
                        EnableSimulcastLayers = publishOptions.Simulcast
                    }
                };
            }

            lk_rtc.TrackInfo trackInfo = await Room.Engine.AddTrack(new lk_rtc.AddTrackOptions
            {
                Cid = track.GetCid(),
                Name = publishOptions.Name ?? (track.Source == TrackSource.ScreenShareVideo
                    ? VideoPublishOptions.DefaultScreenShareName
                    : VideoPublishOptions.DefaultCameraName),
                Kind = track.Kind,
                Source = track.Source.ToPBType(),
                Dimensions = dimensions,
                VideoLayers = Utils.ComputeVideoLayers(dimensions, encodings, isSvc),
                SimulcastCodecs = simulcastCodecs
            });

            this.Log($"PublishVideoTrack AddTrack response: {trackInfo}");

            await track.Start();

            rtc.RTCRtpTransceiverInit transceiverInit = new rtc.RTCRtpTransceiverInit
            {
                Direction = rtc.TransceiverDirection.SendOnly,
                SendEncodings = encodings
            };

            this.Log($"PublishVideoTrack publisher: {Room.Engine.Publisher}");

            track.Transceiver = await Room.Engine.Publisher.PC.AddTransceiver(
                track.MediaStreamTrack,
                rtc.RTCRtpMediaType.Video,
                transceiverInit
            );

            if (Utils.LkBrowser() != BrowserType.Firefox)
            {
                await Room.Engine.SetPreferredCodec(
                    track.Transceiver,
                    "video",
                    publishOptions.VideoCodec
                );
                track.Codec = publishOptions.VideoCodec;
            }

            // Prefer to maintainResolution for screen share
            if (track.Source == TrackSource.ScreenShareVideo)
            {
                rtc.RTCSender sender = track.Transceiver.Sender;
                rtc.RTCRtpSendParameters parameters = sender.Parameters;
                parameters.DegradationPreference = rtc.RTCDegradationPreference.MaintainResolution;
                await sender.SetParameters(parameters);
            }

            await Room.Engine.Negotiate();

            LocalTrackPublication<LocalVideoTrack> pub = new LocalTrackPublication<LocalVideoTrack>(
                this,
                trackInfo,
                track
            );
            AddTrackPublication(pub);
            pub.BackupVideoCodec = publishOptions.BackupCodec;

            // Did publish
            await track.OnPublish();

            Events.Emit(new LocalTrackPublishedEvent(this, pub));

            return pub;*/
        }

        public async UniTask UnpublishMicrophoneTrack()
        {
            var micTrack = AudioTracks.Find((t) => t.Source == TrackSource.Microphone);
            if (micTrack != null)
            {
                await UnpublishTrack(micTrack.Sid);
            }
        }

        public override async UniTask UnpublishTrack(string trackSid, bool notify = true)
        {
            this.Log($"Unpublish track sid: {trackSid}, notify: {notify}");
            if (!TrackPublications.TryGetValue(trackSid, out var pub))
            {
                this.LogWarning($"Publication not found {trackSid}");
                return;
            }

            RemoveTrackPublication(trackSid);
            // await pub.Dispose();

            var track = pub.Track;
            if (track != null)
            {
                var sender = track.SenderRaw;
                if (sender != null)
                {
                    try
                    {
                        var eType = room.engine.publisher.RemoveTrack(sender);
                        // var eType = room.engine.publisher.pc.RemoveTrack(sender);
                        this.Log($"Remove Track For sender {sender} {eType}");
                        if (track is LocalVideoTrack localVideoTrack)
                        {
                            foreach (var simulcastTrack in localVideoTrack.SimulcastCodecs.Values)
                            {
                                eType = room.engine.publisher.pc.RemoveTrack(simulcastTrack.Sender);
                                this.Log($"Remove Track For sender simulcast: {simulcastTrack.Sender} {eType}");
                            }
                        }

                        if (room.RoomOptions.StopLocalTrackOnUnpublish)
                        {
                            await track.Stop();
                        }
                    }
                    catch (Exception ex)
                    {
                        this.LogWarning($"rtc.removeTrack() threw an exception: {ex}");
                    }

                    // Doesn't make sense to negotiate if already disposed
                    if (!IsDisposed)
                    {
                        // Manual negotiation since track changed
                        await room.engine.Negotiate(0.2f);
                    }
                }

                // Did unpublish
                await track.OnUnpublish();
                await room.ApplyAudioSpeakerSettings();

                // SetTrackSubscriptionPermissions(false,
                // new List<ParticipantTrackPermission>());
            }

            if (notify)
            {
                var e = new LocalTrackUnpublishedEvent(this, pub as LocalTrackPublication);
                events.Emit(e);
                room.events.Emit(e);
            }

            await pub.Dispose();
        }

        public async UniTask RePublishAllTracks()
        {
            var tracks = TrackPublications.Select(keyValuePair => keyValuePair.Value).ToList();
            TrackPublications.Clear();
            foreach (var track in tracks)
            {
                switch (track.Track)
                {
                    case LocalAudioTrack audioTrack:
                        await PublishAudioTrack(audioTrack);
                        break;
                    case LocalVideoTrack videoTrack:
                        await PublishVideoTrack(videoTrack);
                        break;
                }
            }
        }

        public async UniTask PublishData(byte[] data, Reliability reliability = Reliability.Reliable,
            List<string> destinationSids = null, string topic = null)
        {
            var packet = new DataPacket
            {
                Kind = reliability.ToPBType(),
                User = new UserPacket
                {
                    Payload = ByteString.CopyFrom(data),
                    ParticipantSid = Sid,
                    DestinationSids = { destinationSids },
                    Topic = topic
                }
            };

            await room.engine.SendDataPacket(packet);
        }

        public async void SetMetadata(string metadata)
        {
            await room.engine.Client.SendUpdateLocalMetadata(new UpdateParticipantMetadata
            {
                Name = Name,
                Metadata = metadata
            });
        }

        public async void SetName(string name)
        {
            UpdateName(name);
            await room.engine.Client.SendUpdateLocalMetadata(new UpdateParticipantMetadata
            {
                Name = name,
                Metadata = Metadata
            });
        }

        public async UniTask<LocalTrackPublication> SetCameraEnabled(bool enabled,
            CameraCaptureOptions cameraCaptureOptions = null)
        {
            return await SetSourceEnabled(TrackSource.Camera, enabled,
                cameraCaptureOptions: cameraCaptureOptions);
        }


        public async UniTask<LocalTrackPublication> SetMicrophoneEnabled(bool enabled,
            AudioCaptureOptions audioCaptureOptions = null, int micIndex = 0)
        {
            return await SetSourceEnabled(TrackSource.Microphone, enabled,
                audioCaptureOptions: audioCaptureOptions, microphoneIndex: micIndex);
        }

        public async UniTask<LocalTrackPublication> SetScreenShareEnabled(bool enabled, bool captureScreenAudio = false,
            ScreenShareCaptureOptions screenShareCaptureOptions = null)
        {
            return await SetSourceEnabled(TrackSource.ScreenShareVideo
                , enabled, captureScreenAudio: captureScreenAudio,
                screenShareCaptureOptions: screenShareCaptureOptions);
        }

        public async UniTask<LocalTrackPublication> SetSourceEnabled(
            TrackSource source, bool enabled, bool captureScreenAudio = false,
            AudioCaptureOptions audioCaptureOptions = null,
            CameraCaptureOptions cameraCaptureOptions = null,
            ScreenShareCaptureOptions screenShareCaptureOptions = null,
            int microphoneIndex = 0)
        {
            this.Log($"SetSourceEnabled(source: {source}, enabled: {enabled})");
            var pub = getTrackPublicationBySource(source);
            if (pub is LocalTrackPublication publication)
            {
                if (enabled)
                {
                    await publication.Unmute();
                }
                else
                {
                    if (source == TrackSource.ScreenShareVideo)
                    {
                        await UnpublishTrack(publication.Sid);
                    }
                    else
                    {
                        await publication.Mute();
                    }
                }

                await room.ApplyAudioSpeakerSettings();
                return publication;
            }

            if (enabled)
            {
                if (source == TrackSource.Camera)
                {
                    var captureOptions = cameraCaptureOptions ?? room.RoomOptions.DefaultCameraCaptureOptions;
                    var track = await VideoManager.Instance.CreateWebcamTrack(captureOptions);
                    if (track != null) return await PublishVideoTrack(track);
                    this.LogError("Failed to create webcam track");
                    return null;
                }

                if (source == TrackSource.Microphone)
                {
                    var captureOptions = audioCaptureOptions ?? room.RoomOptions.DefaultAudioCaptureOptions;
                    var track = await AudioManager.Instance.CreateMicrophoneTrack(this, captureOptions,
                        microphoneIndex);
                    if (track != null) return await PublishAudioTrack(track);
                    this.LogError("Failed to create microphone track");
                    return null;
                }

                if (source == TrackSource.ScreenShareVideo)
                {
                    var captureOptions = screenShareCaptureOptions ?? room.RoomOptions.DefaultScreenShareCaptureOptions;

                    if (captureScreenAudio)
                    {
                        captureOptions = captureOptions.CopyWith(captureScreenAudio: true);
                        var tracks = await LocalVideoTrack.CreateScreenShareTracksWithAudioAsync(captureOptions);
                        LocalTrackPublication publication2 = null;

                        foreach (var track in tracks)
                        {
                            switch (track)
                            {
                                case LocalVideoTrack videoTrack:
                                    publication2 = await PublishVideoTrack(videoTrack);
                                    break;
                                case LocalAudioTrack audioTrack:
                                    await PublishAudioTrack(audioTrack);
                                    break;
                            }
                        }

                        return publication2;
                    }

                    var screenShareTrack = await LocalVideoTrack.CreateScreenShareTrackAsync(captureOptions);
                    return await PublishVideoTrack(screenShareTrack);
                }
            }

            return null;
        }

        private bool _allParticipantsAllowed = true;
        private List<ParticipantTrackPermission> _participantTrackPermissions = new List<ParticipantTrackPermission>();

        // Control who can subscribe to LocalParticipant's published tracks.
        public async void SetTrackSubscriptionPermissions(bool allParticipantsAllowed,
            List<ParticipantTrackPermission> trackPermissions)
        {
            _allParticipantsAllowed = allParticipantsAllowed;
            _participantTrackPermissions = trackPermissions;
            await SendTrackSubscriptionPermissions();
        }

        public async UniTask SendTrackSubscriptionPermissions()
        {
            if (room.engine.ConnectionState != ConnectionState.Connected)
            {
                return;
            }

            // Map the _participantTrackPermissions to their corresponding PB types.
            List<TrackPermission> pbTrackPermissions = new();
            foreach (var p in _participantTrackPermissions)
            {
                pbTrackPermissions.Add(new TrackPermission
                {
                    ParticipantSid = p.ParticipantIdentity,
                    ParticipantIdentity = p.ParticipantIdentity,
                    AllTracks = p.AllTracksAllowed,
                    TrackSids = { p.AllowedTrackSids }
                });
            }

            await room.engine.Client.SendUpdateSubscriptionPermissions(
                _allParticipantsAllowed, pbTrackPermissions
            );
        }

        public List<TrackPublishedResponse> PublishedTracksInfo()
        {
            return TrackPublications.Values.Select(e => (e as LocalTrackPublication)?.toPBTrackPublishedResponse())
                .ToList();
        }

        public override ParticipantPermissions SetPermissions(ParticipantPermissions newValue)
        {
            ParticipantPermissions oldValue = base.SetPermissions(newValue);
            if (oldValue != null)
            {
                // Notify about the permissions update.
                var r = new ParticipantPermissionsUpdatedEvent(this, newValue, oldValue);
                events.Emit(r);
                room.engine.events.Emit(r);
            }

            return oldValue;
        }


        public async UniTask PublishAdditionalCodecForPublication(TrackPublication publication, string codec)
        {
            throw new NotImplementedException();
        }
    }
}