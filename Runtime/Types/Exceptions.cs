using System;

namespace LiveKitUnity.Runtime.Types
{
    public abstract class LiveKitException : Exception
    {
        public LiveKitException(string message) : base(message)
        {
        }

        public override string ToString() => $"LiveKit Exception: [{GetType()}] {Message}";
    }

    public class ConnectException : LiveKitException
    {
        public ConnectException(string msg = "Failed to connect to server") : base(msg)
        {
        }
    }

    public class UnexpectedStateException : LiveKitException
    {
        public UnexpectedStateException(string msg = "Unexpected connection state") : base(msg)
        {
        }
    }

    public class NegotiationError : LiveKitException
    {
        public NegotiationError(string msg = "Negotiation Error") : base(msg)
        {
        }
    }

    public class TrackCreateException : LiveKitException
    {
        public TrackCreateException(string msg = "Failed to create track") : base(msg)
        {
        }
    }

    public class TrackPublishException : LiveKitException
    {
        public TrackPublishException(string msg = "Failed to publish track") : base(msg)
        {
        }
    }

    public class DataPublishException : LiveKitException
    {
        public DataPublishException(string msg = "Failed to publish data") : base(msg)
        {
        }
    }

    public class TimeoutException : LiveKitException
    {
        public TimeoutException(string msg = "Timeout") : base(msg)
        {
        }
    }

    public class LiveKitE2EEException : LiveKitException
    {
        public LiveKitE2EEException(string msg = "E2EE error") : base(msg)
        {
        }

        public override string ToString() => $"E2EE Exception: [{GetType()}] {Message}";
    }
}