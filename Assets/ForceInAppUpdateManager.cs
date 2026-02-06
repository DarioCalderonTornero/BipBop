using System.Collections;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_ANDROID && !UNITY_EDITOR
using Google.Play.AppUpdate;
using Google.Play.Common;
#endif

public class ForceInAppUpdateManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject updatePanel;          // Panel bloqueante (desactivado por defecto)
    [SerializeField] private Animator updatePanelAnimator;     // Animator del panel (opcional)
    [SerializeField] private string popTriggerName = "Pop";    // Trigger del Animator para el “pop”
    [SerializeField] private Button openStoreButton;           // Botón "Ir a Play Store"

    [Header("Behavior")]
    [Tooltip("Si falla la comprobación (sin internet, etc.), ¿dejamos pasar? Para tu caso: true.")]
    [SerializeField] private bool allowIfCheckFails = true;

#if UNITY_ANDROID && !UNITY_EDITOR
    private AppUpdateManager appUpdateManager;
#endif

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        if (openStoreButton != null)
            openStoreButton.onClick.AddListener(OpenPlayStorePage);

        if (updatePanel != null)
            updatePanel.SetActive(false);

#if UNITY_ANDROID && !UNITY_EDITOR
        appUpdateManager = new AppUpdateManager();
#endif
    }

    private void Start()
    {
        StartCoroutine(CheckUpdateAndShowPanelIfNeeded());
    }

    private IEnumerator CheckUpdateAndShowPanelIfNeeded()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        var infoOp = appUpdateManager.GetAppUpdateInfo();
        yield return infoOp;

        if (!infoOp.IsSuccessful)
        {
            Debug.LogWarning($"[UpdateGatekeeper] GetAppUpdateInfo error: {infoOp.Error}");
            if (!allowIfCheckFails)
                ShowUpdatePanel();
            yield break;
        }

        var info = infoOp.GetResult();

        bool updateAvailable = info.UpdateAvailability == UpdateAvailability.UpdateAvailable;
        bool updateInProgress = info.UpdateAvailability == UpdateAvailability.DeveloperTriggeredUpdateInProgress;

        // Si hay una actualización disponible (o estaba a medias), mostramos panel.
        if (updateAvailable || updateInProgress)
            ShowUpdatePanel();

        yield break;
#else
        // Editor / iOS / otras plataformas: no hacemos nada
        yield break;
#endif
    }

    private void ShowUpdatePanel()
    {
        if (updatePanel == null) return;

        updatePanel.SetActive(true);

        // Lanza animación “pop” si la tienes
        if (updatePanelAnimator != null && !string.IsNullOrEmpty(popTriggerName))
        {
            updatePanelAnimator.ResetTrigger(popTriggerName);
            updatePanelAnimator.SetTrigger(popTriggerName);
        }
    }

    private static void OpenPlayStorePage()
    {
        string pkg = Application.identifier;

#if UNITY_ANDROID && !UNITY_EDITOR
        // Intenta abrir Play Store app
        Application.OpenURL("market://details?id=" + pkg);
#else
        // Fallback web
        Application.OpenURL("https://play.google.com/store/apps/details?id=" + pkg);
#endif
    }
}
