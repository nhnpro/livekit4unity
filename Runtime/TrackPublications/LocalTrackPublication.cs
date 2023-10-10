//using System.Threading.Tasks;

using Cysharp.Threading.Tasks;
using LiveKit.Proto;
using LiveKitUnity.Runtime.Participants;
using LiveKitUnity.Runtime.Tracks;

namespace LiveKitUnity.Runtime.TrackPublications
{
    public class LocalTrackPublication : TrackPublication
    {
        public override Participant Participant { get; set; }


        public LocalTrackPublication(LocalParticipant localParticipant, TrackInfo trackInfo, LocalTrack localTrack)
            : base(trackInfo)
        {
            this.Participant = localParticipant;
            UpdateTrack(localTrack);
        }

        public override async UniTask Mute()
        {
            if (Track is LocalTrack localTrack)
                await localTrack.Mute();
        }

        public override async UniTask Unmute()
        {
            if (Track is LocalTrack localTrack)
                await localTrack.Unmute();
        }

        public TrackPublishedResponse toPBTrackPublishedResponse()
        {
            return new TrackPublishedResponse
            {
                Cid = Track?.MediaStreamTrack.Id,
                Track = this.latestInfo,
            };
        }
    }
}