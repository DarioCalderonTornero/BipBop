using UnityEngine;

public class GameOverFlowManager : MonoBehaviour
{
    public static GameOverFlowManager Instance { get; private set; }

    [Header("Config")]
    [Range(0f, 1f)]
    [SerializeField] private float reviveOfferChance = 0.5f;

    [Tooltip("Si true: máximo 1 oferta por partida (por minijuego).")]
    [SerializeField] private bool limitOneOfferPerRun = true;

    [Header("UI Prefab (common)")]
    [SerializeField] private ReviveOfferUIController reviveOfferUIPrefab;

    [Header("Optional: parent for UI instance (Canvas)")]
    [SerializeField] private Transform uiParent;

    private ReviveOfferUIController uiInstance;

    private bool isHandlingFail;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Llamada única desde cualquier minijuego cuando ocurre un fail.
    /// (En la otra tanda lo conectamos a un minijuego real.)
    /// </summary>
    public void NotifyFail(IGameOverClient client)
    {
        if (client == null) return;
        if (isHandlingFail) return; // evita dobles triggers

        isHandlingFail = true;

        // Congela gameplay (si aplica)
        client.PauseOnFail();

        // Si limitamos a 1 por run
        if (limitOneOfferPerRun && client.HasUsedReviveOffer)
        {
            EndToFinal(client);
            return;
        }

        // Decide 50/50
        bool offer = Random.value < reviveOfferChance;

        if (!offer)
        {
            EndToFinal(client);
            return;
        }

        // Marca que ya se usó la oferta en esta run
        if (limitOneOfferPerRun)
            client.HasUsedReviveOffer = true;

        ShowOfferUI(client);
    }

    private void ShowOfferUI(IGameOverClient client)
    {
        EnsureUIInstance();

        // Abre la oferta y define callbacks
        uiInstance.Open(
            onTimeoutOrDecline: () => EndToFinal(client),
            onRewardedCompleted: () => EndToRevive(client)
        );
    }

    private void EndToFinal(IGameOverClient client)
    {
        CloseUIIfOpen();
        isHandlingFail = false;
        client.FinalGameOver();
    }

    private void EndToRevive(IGameOverClient client)
    {
        CloseUIIfOpen();
        isHandlingFail = false;
        client.Revive();
    }

    private void EnsureUIInstance()
    {
        if (uiInstance != null) return;

        if (reviveOfferUIPrefab == null)
        {
            Debug.LogError("[GameOverFlowManager] Missing reviveOfferUIPrefab reference.");
            return;
        }

        Transform parent = uiParent;

        // Si no asignas parent, intentamos usar un Canvas existente.
        if (parent == null)
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas != null) parent = canvas.transform;
        }

        uiInstance = Instantiate(reviveOfferUIPrefab, parent);
        uiInstance.gameObject.SetActive(false);
    }

    private void CloseUIIfOpen()
    {
        if (uiInstance != null)
            uiInstance.CloseImmediate();
    }
}
