using System.Collections;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class LanguageInitializer : MonoBehaviour
{
    private const string LanguageKey = "language"; // "es" o "en"
    private bool switching;

    private IEnumerator Start()
    {
        // Esperar a que Localization esté lista
        yield return LocalizationSettings.InitializationOperation;

        ApplySavedLanguage();
    }

    private void ApplySavedLanguage()
    {
        string saved = PlayerPrefs.GetString(LanguageKey, "es");
        var locales = LocalizationSettings.AvailableLocales.Locales;

        Locale target = locales.Find(l => l.Identifier.Code.StartsWith(saved));

        if (target != null)
            LocalizationSettings.SelectedLocale = target;
    }

    public void ToggleLanguage()
    {
        if (switching) return;
        StartCoroutine(ToggleCoroutine());
    }

    private IEnumerator ToggleCoroutine()
    {
        switching = true;

        yield return LocalizationSettings.InitializationOperation;

        string current = LocalizationSettings.SelectedLocale.Identifier.Code;
        bool isSpanish = current.StartsWith("es");

        string newLang = isSpanish ? "en" : "es";
        var locales = LocalizationSettings.AvailableLocales.Locales;

        Locale target = locales.Find(l => l.Identifier.Code.StartsWith(newLang));

        if (target != null)
        {
            LocalizationSettings.SelectedLocale = target;
            PlayerPrefs.SetString(LanguageKey, newLang);
            PlayerPrefs.Save();
        }

        switching = false;
    }
}
