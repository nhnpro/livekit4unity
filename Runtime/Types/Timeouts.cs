using System;

namespace LiveKitUnity.Runtime.Types
{
    public class Timeouts
    {
        public TimeSpan Connection { get; }
        public TimeSpan Debounce { get; }
        public TimeSpan Publish { get; }
        public TimeSpan PeerConnection { get; }
        public TimeSpan IceRestart { get; }

        public Timeouts(TimeSpan connection, TimeSpan debounce, TimeSpan publish, TimeSpan peerConnection,
            TimeSpan iceRestart)
        {
            Connection = connection;
            Debounce = debounce;
            Publish = publish;
            PeerConnection = peerConnection;
            IceRestart = iceRestart;
        }

        public static Timeouts DefaultTimeouts { get; } = new Timeouts(
            connection: TimeSpan.FromSeconds(10),
            debounce: TimeSpan.FromMilliseconds(100),
            publish: TimeSpan.FromSeconds(10),
            peerConnection: TimeSpan.FromSeconds(10),
            iceRestart: TimeSpan.FromSeconds(10)
        );
    }
}