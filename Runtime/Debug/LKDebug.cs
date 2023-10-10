using System;

namespace LiveKitUnity.Runtime.Debugging
{
    public static class LKDebugExtensions
    {
        public static void Log(this object obj, string message)
        {
            LKDebug.Log(obj.GetType().Name, message);
        }

        public static void LogError(this object obj, string message)
        {
            LKDebug.LogError(obj.GetType().Name, message);
        }

        public static void LogWarning(this object obj, string message)
        {
            LKDebug.LogWarning(obj.GetType().Name, message);
        }

        public static void LogException(this object obj, Exception e)
        {
            LKDebug.LogException(obj.GetType().Name, e);
        }

        public static void LogException(this object obj, string e)
        {
            LKDebug.LogException(obj.GetType().Name, e);
        }
    }

    public static class LKDebug
    {
        public const string DefaultTag = "LiveKitUnity";

        public static void Log(string tag, string message)
        {
            UnityEngine.Debug.Log($"[{tag}] {message}");
        }

        public static void Log(string message)
        {
            Log(DefaultTag, message);
        }

        public static void LogError(string tag, string message)
        {
            UnityEngine.Debug.LogError($"[{tag}] {message}");
        }

        public static void LogError(string message)
        {
            LogError(DefaultTag, message);
        }

        public static void LogWarning(string tag, string message)
        {
            UnityEngine.Debug.LogWarning($"[{tag}] {message}");
        }

        public static void LogWarning(string message)
        {
            LogWarning(DefaultTag, message);
        }

        public static void LogException(string tag, string e)
        {
            UnityEngine.Debug.LogError($"[{tag}] {e}");
        }

        public static void LogException(string tag, Exception e)
        {
            UnityEngine.Debug.LogError($"[{tag}] {e.Message}");
            UnityEngine.Debug.LogException(e);
        }

        public static void LogException(Exception e)
        {
            LogException(DefaultTag, e);
        }
    }
}