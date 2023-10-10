namespace LiveKitUnity.Runtime.Types
{
    [System.Serializable]
    public class ParticipantPermissions
    {
        public bool canSubscribe;
        public bool canPublish;
        public bool canPublishData;
        public bool hidden;
        public bool recorder;

        public ParticipantPermissions()
        {
            canSubscribe = false;
            canPublish = false;
            canPublishData = false;
            hidden = false;
            recorder = false;
        }

        public ParticipantPermissions(
            bool canSubscribe,
            bool canPublish,
            bool canPublishData,
            bool hidden,
            bool recorder)
        {
            this.canSubscribe = canSubscribe;
            this.canPublish = canPublish;
            this.canPublishData = canPublishData;
            this.hidden = hidden;
            this.recorder = recorder;
        }
    }

    public static class ParticipantPermissionExt
    {
        public static ParticipantPermissions ToLKType(this LiveKit.Proto.ParticipantPermission permission)
        {
            return new ParticipantPermissions
            {
                canSubscribe = permission.CanSubscribe,
                canPublish = permission.CanPublish,
                canPublishData = permission.CanPublishData,
                hidden = permission.Hidden,
                recorder = permission.Recorder
            };
        }

        public static LiveKit.Proto.ParticipantPermission ToPBType(this ParticipantPermissions permission)
        {
            return new LiveKit.Proto.ParticipantPermission
            {
                CanSubscribe = permission.canSubscribe,
                CanPublish = permission.canPublish,
                CanPublishData = permission.canPublishData,
                Hidden = permission.hidden,
                Recorder = permission.recorder
            };
        }
    }
}