using System.Collections.Generic;

namespace LiveKitUnity.Runtime.Internal
{
    public class RTCOfferOptions
    {
        public bool IceRestart { get; }

        public RTCOfferOptions(bool iceRestart = false)
        {
            IceRestart = iceRestart;
        }

        public Dictionary<string, dynamic> ToDictionary()
        {
            var dict = new Dictionary<string, dynamic>();
            if (IceRestart)
            {
                dict["iceRestart"] = true;
            }

            return dict;
        }
    }
}