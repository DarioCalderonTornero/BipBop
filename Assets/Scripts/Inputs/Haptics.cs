using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Haptics
{
     private const string key = "HAPTICS_ENABLED";
    public static bool enabled { private set; get; }

    static Haptics()
    {
        enabled = PlayerPrefs.GetInt(key, 1) == 1;
    }

    public static void SetEnabled(bool value)
    {
        enabled = value;
        PlayerPrefs.SetInt(key, value ? 1 : 0);
        PlayerPrefs.Save();
    }

    public static void TryVibrate()
    {
        if (!enabled)
        {
            return;
        }
        if (enabled)
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
        Handheld.Vibrate();
#endif
        }
    }
}
