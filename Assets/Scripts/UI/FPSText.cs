using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FPSText : MonoBehaviour
{
    public TextMeshProUGUI fpsText;
    private float timer = 0f;
    private float updateInterval = 0.5f;

    void Update()
    {
        // Application.targetFrameRate = 60;

        timer += Time.deltaTime;
        if (timer >= updateInterval)
        {
            float fps = 1f / Time.deltaTime;
            fpsText.text = "FPS: " + Mathf.RoundToInt(fps).ToString();
            timer = 0f;
        }
    }
}
