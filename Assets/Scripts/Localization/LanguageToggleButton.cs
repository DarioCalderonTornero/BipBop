using System.Collections;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class LanguageToggleButton : MonoBehaviour
{
    bool isSwitching;

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

        Locale target = null;

        string currentCode = current.Identifier.Code;

        if (currentCode == "es")
        {
            target = locales.Find(l => l.Identifier.Code == "en");
        }
        else
        {
            target = locales.Find(l => l.Identifier.Code == "es");
        }

        LocalizationSettings.SelectedLocale = target;

        isSwitching = false;
    }
}
