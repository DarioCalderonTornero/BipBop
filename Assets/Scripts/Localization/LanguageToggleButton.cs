using System.Collections;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class LanguageToggleButton : MonoBehaviour
{
    const string LanguageKey = "language";   // PlayerPrefs key
    bool isSwitching;

    private IEnumerator Start()
    {
        // Al entrar al juego, aplicamos el idioma guardado
        yield return LocalizationSettings.InitializationOperation;

        var locales = LocalizationSettings.AvailableLocales.Locales;
        if (locales == null || locales.Count == 0)
            yield break;

        // "es" por defecto si no hay nada guardado
        string savedLang = PlayerPrefs.GetString(LanguageKey, "es");

        // Buscamos un Locale cuyo código empiece por "es" o "en"
        Locale target = locales.Find(l => l.Identifier.Code.StartsWith(savedLang));

        if (target != null)
        {
            LocalizationSettings.SelectedLocale = target;
        }
    }

    public void ToggleLanguage()
    {
        if (isSwitching) return;
        StartCoroutine(SwitchCoroutine());
    }

    IEnumerator SwitchCoroutine()
    {
        isSwitching = true;

        yield return LocalizationSettings.InitializationOperation;

        var current = LocalizationSettings.SelectedLocale;
        var locales = LocalizationSettings.AvailableLocales.Locales;

        if (locales == null || locales.Count == 0)
        {
            isSwitching = false;
            yield break;
        }

        // Si por lo que sea es null, asumimos español actual
        string currentCode = current != null ? current.Identifier.Code : "es";

        bool isSpanish = currentCode.StartsWith("es");   // cubre "es", "es-ES", etc.
        string targetPrefix = isSpanish ? "en" : "es";

        Locale target = locales.Find(l => l.Identifier.Code.StartsWith(targetPrefix));

        if (target != null)
        {
            LocalizationSettings.SelectedLocale = target;

            // Guardamos SOLO el prefijo ("es" o "en")
            PlayerPrefs.SetString(LanguageKey, targetPrefix);
            PlayerPrefs.Save();
        }

        isSwitching = false;
    }
}
