using UnityEngine;

public class FPSManager : MonoBehaviour
{
    private const string PREF_KEY = "FPS_LIMIT";
    private readonly int defaultFPS = 60;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        int savedFPS = PlayerPrefs.GetInt(PREF_KEY, defaultFPS);

        Application.targetFrameRate = savedFPS;
        QualitySettings.vSyncCount = 0; // Asegura que Unity respeta targetFrameRate

        Debug.Log("[FPSManager] FPS aplicado al iniciar: " + savedFPS);
    }
}
