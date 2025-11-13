using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

[RequireComponent(typeof(TMP_Text))]

public class LocaleFontSize : MonoBehaviour
{
    [Header("Tamaños por idioma")]
    public float spanishSize = 40f;
    public float englishSize = 32f;

    TMP_Text tmp;

    void Awake()
    {
        tmp = GetComponent<TMP_Text>();
    }

    IEnumerator Start()
    {
        // Esperar a que el sistema de Localization esté listo
        yield return LocalizationSettings.InitializationOperation;

        // Aplicar tamaño inicial según el idioma actual
        ApplySize(LocalizationSettings.SelectedLocale);

        // Escuchar futuros cambios de idioma
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    }

    void OnDestroy()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    void OnLocaleChanged(Locale newLocale)
    {
        ApplySize(newLocale);
    }

    void ApplySize(Locale locale)
    {
        if (locale == null) return;

        // Código del locale: "es", "es-ES", "en", "en-US", etc.
        string code = locale.Identifier.Code;

        if (code.StartsWith("es"))
        {
            tmp.fontSize = spanishSize;
        }
        else if (code.StartsWith("en"))
        {
            tmp.fontSize = englishSize;
        }
        else
        {
            // Idioma por defecto si añades otros
            tmp.fontSize = englishSize;
        }
    }
}
