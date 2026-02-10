using System.Collections;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class LocaleBootStrap : MonoBehaviour
{
    private const string LanguageKey = "SelectedLocale";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoCreate()
    {
        var go = new GameObject("[LocaleBootstrap]");
        DontDestroyOnLoad(go);
        go.AddComponent<LocaleBootStrap>();
    }

    private IEnumerator Start()
    {
        yield return LocalizationSettings.InitializationOperation;
        ApplySavedLocale();
    }

    public static void SaveAndApply(Locale locale)
    {
        if (locale == null) return;

        PlayerPrefs.SetString(LanguageKey, locale.Identifier.Code);
        PlayerPrefs.Save();

        LocalizationSettings.SelectedLocale = locale;
    }

    public static void ApplySavedLocale()
    {
        string savedCode = PlayerPrefs.GetString(LanguageKey, "");
        if (string.IsNullOrEmpty(savedCode) || LocalizationSettings.AvailableLocales == null) return;

        Locale target = null;
        foreach (var l in LocalizationSettings.AvailableLocales.Locales)
        {
            if (l.Identifier.Code.StartsWith(savedCode))
            {
                target = l;
                break;
            }
        }

        if (target != null && LocalizationSettings.SelectedLocale != target)
            LocalizationSettings.SelectedLocale = target;
    }
}
