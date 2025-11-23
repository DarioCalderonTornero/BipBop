using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FPSButtonController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button fpsButton;
    [SerializeField] private TextMeshProUGUI fpsText;

    // Valores permitidos
    private readonly int[] fpsValues = new int[] { 30, 45, 60 };

    private int currentIndex = 0;
    private const string PREF_KEY = "FPS_LIMIT";

    private void Awake()
    {
        // Cargar FPS guardado
        int savedFPS = PlayerPrefs.GetInt(PREF_KEY, 60); // 60 por defecto

        // Buscar su índice en la lista
        for (int i = 0; i < fpsValues.Length; i++)
        {
            if (fpsValues[i] == savedFPS)
            {
                currentIndex = i;
                break;
            }
        }

        ApplyFPS();

        // Listener del botón
        fpsButton.onClick.AddListener(ChangeFPS);
    }

    private void ChangeFPS()
    {
        currentIndex++;

        if (currentIndex >= fpsValues.Length)
            currentIndex = 0;

        ApplyFPS();
    }

    private void ApplyFPS()
    {
        int newFPS = fpsValues[currentIndex];

        // Cambiar en Unity
        Application.targetFrameRate = newFPS;

        // Actualizar UI
        fpsText.text = "" + newFPS;

        // Guardar
        PlayerPrefs.SetInt(PREF_KEY, newFPS);
        PlayerPrefs.Save();
    }
}
