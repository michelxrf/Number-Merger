using UnityEngine;

namespace NumberMerge.Managers
{
    // Vibration with a caller-chosen duration. Android uses the Vibrator service so the length is exact.
    // Other platforms fall back to Handheld.Vibrate, which has a fixed length set by the OS.
    public static class Haptics
    {
        public static void Vibrate(float seconds)
        {
            if (seconds <= 0f)
            {
                return;
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                VibrateAndroid(Mathf.RoundToInt(seconds * 1000f));
            }
            catch (System.Exception exception)
            {
                // Some devices or builds refuse the Vibrator call. Fall back to the OS vibration instead of failing.
                Debug.LogException(exception);
                Handheld.Vibrate();
            }
#else
            Handheld.Vibrate();
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static void VibrateAndroid(long milliseconds)
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
            {
                vibrator.Call("vibrate", milliseconds);
            }
        }
#endif
    }
}
