using System.Collections;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class LanguageToggleButton : MonoBehaviour
{
    const string LanguageKey = "language";   
    bool isSwitching;

    private IEnumerator Start()
    {
        yield return LocalizationSettings.InitializationOperation;

        var locales = LocalizationSettings.AvailableLocales.Locales;
        if (locales == null || locales.Count == 0)
            yield break;

        string savedLang = PlayerPrefs.GetString(LanguageKey, "es");

        // Buscamos un Locale cuyo código empiece por "es" o "en"
        Locale target = locales.Find(l => l.Identifier.Code.StartsWith(savedLang));

        if (target != null)
        {
            LocalizationSettings.SelectedLocale = target;
        }

        Locale current = LocalizationSettings.SelectedLocale;

        string code = current.Identifier.Code;

        bool isSpanish = code.StartsWith("es");
        bool isEnglish = code.StartsWith("en");
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

        string currentCode = current != null ? current.Identifier.Code : "es";

        bool isSpanish = currentCode.StartsWith("es");   
        string targetPrefix = isSpanish ? "en" : "es";

        Locale target = locales.Find(l => l.Identifier.Code.StartsWith(targetPrefix));

        if (target != null)
        {
            LocalizationSettings.SelectedLocale = target;

            PlayerPrefs.SetString(LanguageKey, targetPrefix);
            PlayerPrefs.Save();
        }

        isSwitching = false;
    }

    public void ForceSpanish()
    {
        StartCoroutine(ForceSpanishCoroutine());
    }

    private IEnumerator ForceSpanishCoroutine()
    {
        yield return LocalizationSettings.InitializationOperation;

        var locales = LocalizationSettings.AvailableLocales.Locales;

        if (locales == null || locales.Count == 0)
            yield break;

        string currentCode = LocalizationSettings.SelectedLocale.Identifier.Code;
        if (currentCode.StartsWith("es"))
            yield break;

        Locale target = locales.Find(l => l.Identifier.Code.StartsWith("es"));

        if (target != null)
        {
            LocalizationSettings.SelectedLocale = target;

            PlayerPrefs.SetString("language", "es");
            PlayerPrefs.Save();
        }
    }

}
