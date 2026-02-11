using UnityEngine;

public class FPSManager : MonoBehaviour
{
    public const string PREF_KEY = "FPS_LIMIT";

    private const int FPS_AUTO = -1;
    private static readonly int[] Allowed = { 60, 90, 120, FPS_AUTO };

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        int saved = PlayerPrefs.GetInt(PREF_KEY, FPS_AUTO);
        ApplyFPS(saved);
    }

    public static void SetFPS(int fps)
    {
        if (System.Array.IndexOf(Allowed, fps) < 0)
            fps = FPS_AUTO;

        PlayerPrefs.SetInt(PREF_KEY, fps);
        PlayerPrefs.Save();

        ApplyFPS(fps);
    }

    private static void ApplyFPS(int fps)
    {
        QualitySettings.vSyncCount = 0;

        if (fps == FPS_AUTO)
        {
            int deviceRate = Screen.currentResolution.refreshRateRatio.value > 0
                ? (int)Screen.currentResolution.refreshRateRatio.value
                : 60;

            int target = 60;

            if (deviceRate >= 120) target = 120;
            else if (deviceRate >= 90) target = 90;

            Application.targetFrameRate = target;

            Debug.Log($"[FPSManager] AUTO → {target} FPS (Device: {deviceRate})");
        }
        else
        {
            Application.targetFrameRate = fps;
            Debug.Log($"[FPSManager] Manual → {fps} FPS");
        }
    }

    public static int GetSavedFPS()
    {
        return PlayerPrefs.GetInt(PREF_KEY, FPS_AUTO);
    }

    public static string GetFPSDisplay()
    {
        int saved = GetSavedFPS();
        return saved == FPS_AUTO ? "AUTO" : saved.ToString();
    }
}
