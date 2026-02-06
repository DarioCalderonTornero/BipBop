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

    private TMP_Text tmp;
    private Coroutine initCo;
    private bool subscribed;

    private void Awake()
    {
        tmp = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        if (initCo != null) StopCoroutine(initCo);
        initCo = StartCoroutine(InitAndApply());
    }

    private IEnumerator InitAndApply()
    {
        yield return LocalizationSettings.InitializationOperation;

        ApplySize(LocalizationSettings.SelectedLocale);

        if (!subscribed)
        {
            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
            subscribed = true;
        }
    }

    private void OnDisable()
    {
        if (subscribed)
        {
            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
            subscribed = false;
        }
    }

    private void OnLocaleChanged(Locale newLocale)
    {
        ApplySize(newLocale);
    }

    private void ApplySize(Locale locale)
    {
        if (locale == null) return;

        string code = locale.Identifier.Code;

        if (code.StartsWith("es")) tmp.fontSize = spanishSize;
        else if (code.StartsWith("en")) tmp.fontSize = englishSize;
        else tmp.fontSize = englishSize;

        tmp.ForceMeshUpdate();
    }
}
