using System;
using System.Collections.Generic;
using System.Linq;
// //using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using LiveKitUnity.Runtime.Debugging;
using LiveKitUnity.Runtime.Internal;
using LiveKitUnity.Runtime.Types;
using Unity.WebRTC;

namespace LiveKitUnity.Runtime.Core
{
    public delegate void TransportOnOffer(RTCSessionDescription offer);

    public delegate UniTask<RTCPeerConnection> PeerConnectionCreate(RTCConfiguration configuration,
        Dictionary<string, dynamic> constraints = null);

    public class Transport : IDisposable
    {
        public readonly RTCPeerConnection pc;
        private readonly List<RTCIceCandidate> _pendingCandidates = new List<RTCIceCandidate>();
        public bool restartingIce = false;
        private bool renegotiate = false;
        public TransportOnOffer onOffer;
        private Action _cancelDebounce;
        private ConnectOptions connectOptions;
        private bool IsDisposed = false;

        private Transport(RTCPeerConnection pc, ConnectOptions connectOptions)
        {
            this.pc = pc;
            this.connectOptions = connectOptions;
        }

        public void Dispose()
        {
            IsDisposed = true;
            _cancelDebounce?.Invoke();
            _cancelDebounce = null;

            if (pc != null)
            {
                pc.OnTrack = null;
                pc.OnConnectionStateChange = null;
                pc.OnNegotiationNeeded = null;
                pc.OnIceCandidate = null;
                pc.OnIceConnectionChange = null;
                pc.OnIceGatheringStateChange = null;
                pc.OnDataChannel = null;

                var senders = new RTCRtpSender[] { };
                try
                {
                    senders = pc.GetSenders().ToArray();
                }
                catch
                {
                    this.LogWarning("getSenders() failed with error");
                }

                foreach (var sender in senders)
                {
                    try
                    {
                        var errType = pc.RemoveTrack(sender);
                        if (errType != RTCErrorType.None)
                        {
                            this.LogWarning($"removeTrack() for sender {sender} failed with error {errType}");
                        }
                    }
                    catch
                    {
                        this.LogWarning("removeTrack() failed with error");
                    }
                }

                pc.Close();
                pc.Dispose();
            }
        }

        public static async UniTask<Transport> Create(PeerConnectionCreate peerConnectionCreate,
            RTCConfiguration? rtcConfig, ConnectOptions connectOptions)
        {
            rtcConfig ??= new RTCConfiguration();
            // this.Log("[PCTransport] creating " + rtcConfig);
            var pc = await peerConnectionCreate(rtcConfig.Value);
            return new Transport(pc, connectOptions);
        }

        public async UniTask NegotiateDebounce(float timeouts = 0.1f)
        {
            await UniTask.WaitForSeconds(timeouts);
            await CreateAndSendOffer();
        }

        public async UniTask SetRemoteDescription(RTCSessionDescription sd)
        {
            if (IsDisposed)
            {
                this.LogWarning("setRemoteDescription() already disposed");
                return;
            }

            await pc.SetRemoteDescription(ref sd);

            foreach (var candidate in _pendingCandidates)
            {
                pc.AddIceCandidate(candidate);
            }

            _pendingCandidates.Clear();
            restartingIce = false;

            if (renegotiate)
            {
                renegotiate = false;
                await CreateAndSendOffer();
            }
        }

        public async UniTask CreateAndSendOffer(RTCOfferOptions options = null)
        {
            if (IsDisposed)
            {
                this.LogWarning("createAndSendOffer() already disposed");
                return;
            }

            if (onOffer == null)
            {
                this.LogWarning("onOffer is null");
                return;
            }

            if (options?.IceRestart ?? false)
            {
                this.Log("restarting ICE");
                restartingIce = true;
            }

            if (pc.SignalingState == RTCSignalingState.HaveLocalOffer)
            {
                var currentSD = await GetRemoteDescription();
                if ((options?.IceRestart ?? false) && currentSD.HasValue)
                {
                    var temp = currentSD.Value;
                    await pc.SetRemoteDescription(ref temp);
                }
                else
                {
                    renegotiate = true;
                    return;
                }
            }

            if (restartingIce) // && !lkPlatformIs(PlatformType.Web))
            {
                // await pc.RestartIce();
                pc.RestartIce(); //Todo await
            }

            this.Log("starting to negotiate");
            var offer = await createOfferAsync();
            try
            {
                var localDesc = await setLocalDescription(offer);
                onOffer?.Invoke(localDesc);
            }
            catch (Exception e)
            {
                throw new NegotiationError(e.ToString());
            }
        }

        private async UniTask<RTCSessionDescription> setLocalDescription(RTCSessionDescription sd)
        {
            try
            {
                await pc.SetLocalDescription(ref sd);
                return sd;
            }
            catch (Exception error)
            {
                throw new Exception("Failed to set local description", error);
            }
        }

        private async UniTask<RTCSessionDescription> createOfferAsync()
        {
            try
            {
                var sd = pc.CreateOffer();
                await UniTask.WaitUntil(() => !string.IsNullOrEmpty(sd.Desc.sdp));
                this.Log($"Transport CreateOffer Session SDP - {sd.Desc.sdp} - {pc.SignalingState}");
                return sd.Desc;
            }
            catch (Exception error)
            {
                throw new Exception("Failed to create offer", error);
            }
        }

        public async UniTask AddIceCandidate(RTCIceCandidate candidate)
        {
            if (IsDisposed)
            {
                this.LogWarning("addIceCandidate() already disposed");
                return;
            }

            var desc = await GetRemoteDescription();

            if (desc != null && !restartingIce)
            {
                pc.AddIceCandidate(candidate);
                return;
            }

            _pendingCandidates.Add(candidate);
        }

        public async UniTask<RTCSessionDescription?> GetRemoteDescription()
        {
            if (IsDisposed)
            {
                this.LogWarning("getRemoteDescription() already disposed");
                return null;
            }

            try
            {
                var result = pc.RemoteDescription;
                this.Log("pc.GetRemoteDescription " + result);
                return result;
            }
            catch
            {
                this.LogWarning("pc.GetRemoteDescription failed with error");
                return null;
            }
        }

        public async UniTask<RTCSessionDescription> CreatAnswerAsync()
        {
            try
            {
                var sd = pc.CreateAnswer();
                await UniTask.WaitUntil(() => !string.IsNullOrEmpty(sd.Desc.sdp));

                this.Log($"Transport CreateAnswer Session SDP - {sd.Desc.sdp}");

                return sd.Desc;
            }
            catch (Exception error)
            {
                throw new Exception("failed to create answer", error);
            }
        }

        public void UnPublishAll()
        {
            var senders = pc.GetSenders();
            foreach (var rtcRtpSender in senders)
            {
                pc.RemoveTrack(rtcRtpSender);
            }
        }

        public void DebugPrint()
        {
            var r = pc.GetReceivers();
            foreach (var rtcRtpReceiver in r)
            {
                this.Log($"Receivers: {rtcRtpReceiver.ToString()}");
            }
        }

        public RTCRtpSender AddTrack(MediaStreamTrack trackMediaStreamTrack)
        {
            return pc.AddTrack(trackMediaStreamTrack);
        }

        public RTCErrorType RemoveTrack(RTCRtpSender sender)
        {
            return pc.RemoveTrack(sender);
        }
    }
}