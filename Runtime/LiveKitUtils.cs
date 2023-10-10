using System;
using System.Collections.Generic;
using System.Linq;
//using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using LiveKitUnity.Runtime.Types;
using UnityEngine;
using UnityEngine.Networking;

namespace LiveKitUnity.Runtime
{
    public static class LiveKitUtils
    {
        //TODO list
        //1. Ping Handle
        //2. Reconnect Handle
        //3. Reconnect Timeout Handle
        //4. Reconnect Retry Handle
        //5. Event Emitter queueing
        //6. Event Listener queueing
        public static string Version = "0.1.0";
        public static string NetworkType = Application.internetReachability.ToString();
        public static string OsType = SystemInfo.operatingSystemFamily.ToString();
        public static string OsVersion = SystemInfo.operatingSystem;
        public static string DeviceModel = SystemInfo.deviceModel;
        public static string Browser = "chrome";
        public static string BrowserVersion = "94.0.4606.81";

        public static async UniTask<UnityWebRequest> HTTPGet(Uri uri)
        {
            UnityWebRequest webRequest = UnityWebRequest.Get(uri);
            // Request and wait for the desired page.
            await webRequest.SendWebRequest();
            return webRequest;
        }

        public static async UniTask<Uri> BuildUri(
            string uriString, string token,
            ConnectOptions connectOptions, RoomOptions roomOptions,
            bool reconnect = false, string sid = null
            , bool validate = false, bool forceSecure = false)
        {
            if (uriString.EndsWith("/"))
            {
                uriString = uriString.Substring(0, uriString.Length - 1);
            }

            Uri uri = new Uri(uriString);
            bool useSecure = IsSecureScheme(uri.Scheme) || forceSecure;
            string httpScheme = useSecure ? "https" : "http";
            string wsScheme = useSecure ? "wss" : "ws";
            List<string> lastSegments = validate
                ? new List<string> { "rtc", "validate" }
                : new List<string> { "rtc" };

            List<string> pathSegments = new List<string>(uri.Segments);
            pathSegments.RemoveAll((s) => string.IsNullOrEmpty(s) || s == "/");
            pathSegments.AddRange(lastSegments);

            UriBuilder uriBuilder = new UriBuilder(uri)
            {
                Scheme = validate ? httpScheme : wsScheme,
                Path = string.Join("/", pathSegments),
            };

            var queryParameters = new Dictionary<string, string>
            {
                { "access_token", token },
                { "auto_subscribe", connectOptions.AutoSubscribe ? "1" : "0" },
                { "adaptive_stream", roomOptions.AdaptiveStream ? "1" : "0" }
            };

            if (reconnect)
            {
                queryParameters["reconnect"] = "1";
                if (!string.IsNullOrEmpty(sid))
                {
                    queryParameters["sid"] = sid;
                }
            }

            queryParameters["protocol"] = connectOptions.ProtocolVersion.ToStringValue();
            queryParameters["sdk"] = "unity";

            // Assuming you're using Unity
            //TODO: add this later
            queryParameters["version"] = Version; // Make sure to replace with your actual version
            queryParameters["network"] = NetworkType;
            queryParameters["os"] = OsType;
            queryParameters["os_version"] = OsVersion;
            queryParameters["device_model"] = DeviceModel;
            if (IsWebPlatform())
            {
                queryParameters["browser"] = Browser;
                queryParameters["browser_version"] = BrowserVersion;
            }

            uriBuilder.Query = string.Join("&",
                queryParameters.Select(kv
                    => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));

            return uriBuilder.Uri;
        }

        public static bool IsSecureScheme(string uriScheme)
        {
            return uriScheme is "wss" or "https";
        }

        public static bool IsDesktopPlatform()
        {
            return Application.platform == RuntimePlatform.WindowsEditor ||
                   Application.platform == RuntimePlatform.WindowsPlayer ||
                   Application.platform == RuntimePlatform.LinuxPlayer ||
                   Application.platform == RuntimePlatform.WebGLPlayer ||
                   Application.platform == RuntimePlatform.OSXEditor ||
                   Application.platform == RuntimePlatform.OSXPlayer;
        }

        public static bool IsiOSPlatform()
        {
            return Application.platform == RuntimePlatform.IPhonePlayer;
        }

        public static bool IsWebPlatform()
        {
            return Application.platform == RuntimePlatform.WebGLPlayer;
        }

        public static void PrintArray<T>(string prefix, IEnumerable<T> data, bool skipZero = false)
        {
            var str = $"{prefix}[";
            var count = 0;
            foreach (var item in data)
            {
                if (!item.Equals(default(T)) || !skipZero)
                {
                    str += item + ",";
                    count++;
                }
            }

            if (count == 0)
            {
                str += "All Zero";
            }

            str = str.TrimEnd(',') + "]";
            Debug.Log(str);
        }

        public static T ConvertJson<T>(string jsonString)
        {
            return JsonUtility.FromJson<T>(jsonString);
        }

        public static string ToJson(object x)
        {
            return JsonUtility.ToJson(x);
        }

        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
            var component = gameObject.GetComponent<T>();
            if (component == null)
            {
                component = gameObject.AddComponent<T>();
            }

            return component;
        }
    }
}