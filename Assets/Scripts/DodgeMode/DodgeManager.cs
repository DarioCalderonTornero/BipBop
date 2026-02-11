// DodgeManager.cs
using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class DodgeManager : MonoBehaviour, IGameOverClient
{
    public static DodgeManager Instance;

    public int score = 0;
    public float CurrentEnemySpeed = 2f;

    [SerializeField] private TextMeshProUGUI scoreText;

    [SerializeField] private TurboController turboController;

    public event EventHandler OnGameOver;

    [Header("FX")]
    public GameObject[] asteroidExplosionPrefabs;
    public GameObject playerExplosionPrefab;

    [Header("Player")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform playerSpawnPoint;
    [SerializeField] private float reviveInvulSeconds = 2f;

    [Header("Sounds")]
    [SerializeField] private AudioClip gameOverAudioClip;

    private bool isGameOver = false;

    // =========================
    // ADS / GameOverFlow (NEW)
    // =========================
    public bool HasUsedReviveOffer { get; set; } = false;
    private bool isPausedByOffer = false;

    [Header("Tutorial Panel")]
    [SerializeField] private TutorialPanelUI tutorialPrefab;
    [SerializeField] private Transform tutorialParent;

    private const string ShowTutorialKey = "ShowTutorialOnStart";
    private TutorialPanelUI tutorialInstance;

    private bool hasStarted = false;
    private bool gameplayEnabled = false;

    private void Awake()
    {
        Instance = this;

        if (scoreText != null)
            scoreText.text = $"{score}";

        Enemy.GlobalFreeze = true; // arrancamos congelado hasta Playing

        isGameOver = false;
        HasUsedReviveOffer = false;
        isPausedByOffer = false;

        gameplayEnabled = false;
        hasStarted = false;
    }

    private void Start()
    {
        Time.timeScale = 1f;

        bool showTutorial = PlayerPrefs.GetInt(ShowTutorialKey, 1) == 1;

        if (showTutorial && tutorialPrefab != null)
        {
            ShowTutorial();
        }
        else
        {
            HideAnyExistingTutorialPanel();
            BeginAfterTutorial();
        }
    }

    private void HideAnyExistingTutorialPanel()
    {
        var existing = FindObjectOfType<TutorialPanelUI>(true);
        if (existing != null)
            existing.gameObject.SetActive(false);
    }

    private void ShowTutorial()
    {
        if (tutorialInstance != null) return;

        var existing = FindObjectOfType<TutorialPanelUI>(true);
        if (existing != null)
        {
            tutorialInstance = existing;
            tutorialInstance.gameObject.SetActive(true);
        }
        else
        {
            Transform parent = tutorialParent;
            if (parent == null)
            {
                Canvas c = FindObjectOfType<Canvas>();
                parent = (c != null) ? c.transform : transform;
            }

            tutorialInstance = Instantiate(tutorialPrefab, parent);
        }

        tutorialInstance.OnClosed -= HandleTutorialClosed;
        tutorialInstance.OnClosed += HandleTutorialClosed;

        // Mientras tutorial: no gameplay
        Enemy.GlobalFreeze = true;
        gameplayEnabled = false;
    }

    private void HandleTutorialClosed()
    {
        if (tutorialInstance != null)
            tutorialInstance.OnClosed -= HandleTutorialClosed;

        tutorialInstance = null;

        BeginAfterTutorial();
    }

    private void BeginAfterTutorial()
    {
        StartGame();
    }

    private void StartGame()
    {
        if (hasStarted) return;
        hasStarted = true;

        // Reset partida
        score = 0;
        if (scoreText != null) scoreText.text = $"{score}";

        isGameOver = false;

        // NEW: reset revive per run
        HasUsedReviveOffer = false;
        isPausedByOffer = false;

        gameplayEnabled = false;
        Enemy.GlobalFreeze = true; // hasta Playing

        // Arranca countdown desde State (NO en DodgeState.Start)
        if (DodgeState.Instance != null)
            DodgeState.Instance.StartCountdown();
        else
            EnableGameplayNow_Fallback();
    }

    // Lo llama DodgeState cuando acaba el countdown y entra en Playing
    public void EnableGameplayNow()
    {
        gameplayEnabled = true;
        Enemy.GlobalFreeze = false;

        // Reset player al empezar
        EnablePlayerAfterRevive();
        ResetPlayerToSpawn();

        if (turboController != null)
            turboController.ResetTurbo();
    }

    private void EnableGameplayNow_Fallback()
    {
        gameplayEnabled = true;
        Enemy.GlobalFreeze = false;
    }

    // =========================
    //  Puntos / colisiones enemy
    // =========================
    public void EnemiesCollided(GameObject e1, GameObject e2)
    {
        if (isGameOver) return;

        // NEW: si offer está abierto, no sumar ni procesar
        if (isPausedByOffer) return;

        Enemy enemy1 = e1.GetComponent<Enemy>();
        Enemy enemy2 = e2.GetComponent<Enemy>();

        if (enemy1 != null) SpawnExplosion(enemy1);
        if (enemy2 != null) SpawnExplosion(enemy2);

        if (MeteorCameraShake.Instance != null)
            MeteorCameraShake.Instance.Shake(0.18f, 0.35f);

        Destroy(e1);
        Destroy(e2);

        score += 1;
        if (scoreText != null)
            scoreText.text = $"{score}";

        PlayerLevelManager.Instance.AddXP(15);

#if UNITY_ANDROID || UNITY_IOS
        Haptics.TryVibrate();
#endif
    }

    private void SpawnExplosion(Enemy enemy)
    {
        if (enemy == null || asteroidExplosionPrefabs == null || asteroidExplosionPrefabs.Length == 0)
            return;

        int index = Mathf.Clamp(enemy.explosionIndex, 0, asteroidExplosionPrefabs.Length - 1);
        GameObject prefab = asteroidExplosionPrefabs[index];
        if (prefab == null) return;

        GameObject fx = Instantiate(prefab, enemy.transform.position, Quaternion.identity);
        fx.transform.localScale = enemy.transform.localScale;
        Destroy(fx, 3f);
    }

    // =========================
    //  Jugador golpeado
    // =========================
    public void PlayerHit(Enemy killer)
    {
        if (isGameOver) return;
        if (isPausedByOffer) return;

        StartCoroutine(SlowMotionAndThenFail(killer));
    }

    private IEnumerator SlowMotionAndThenFail(Enemy killer)
    {
        isGameOver = true;

        float prevTimeScale = Time.timeScale;
        float prevFixedDelta = Time.fixedDeltaTime;

        // Cámara lenta
        Time.timeScale = 0.1f;
        Time.fixedDeltaTime = 0.01f * Time.timeScale;

        // Congelar enemigos
        Enemy.GlobalFreeze = true;

        // 1) Flash killer
        if (killer != null)
            yield return killer.FlashRedCoroutine(3, 0.15f);
        else
            yield return new WaitForSecondsRealtime(0.9f);

        // 2) Explosión meteorito killer
        if (killer != null)
        {
            SpawnExplosion(killer);
            Destroy(killer.gameObject);
        }

        if (MeteorCameraShake.Instance != null)
            MeteorCameraShake.Instance.Shake(0.2f, 0.4f);

        // 3) Explosión nave (visual)
        if (playerExplosionPrefab != null && playerTransform != null)
        {
            GameObject fxPlayer = Instantiate(playerExplosionPrefab, playerTransform.position, Quaternion.identity);
            Destroy(fxPlayer, 2.5f);
        }

        DisablePlayerForDeath();

        yield return new WaitForSecondsRealtime(0.6f);

        // Restaurar time (antes de abrir offer)
        Time.timeScale = prevTimeScale;
        Time.fixedDeltaTime = prevFixedDelta;

        TriggerFail(); // NEW: centralizado
    }

    private void TriggerFail()
    {
        // Evitar dobles entradas
        if (isPausedByOffer) return;

        if (GameOverFlowManager.Instance != null)
            GameOverFlowManager.Instance.NotifyFail(this);
        else
            FinalGameOver(); // fallback
    }

    private void DisablePlayerForDeath()
    {
        if (playerTransform == null) return;
        playerTransform.gameObject.SetActive(false);
    }

    private void EnablePlayerAfterRevive()
    {
        if (playerTransform == null) return;
        playerTransform.gameObject.SetActive(true);
    }

    private void ResetPlayerToSpawn()
    {
        if (playerTransform == null || playerSpawnPoint == null) return;

        playerTransform.position = playerSpawnPoint.position;
        playerTransform.rotation = playerSpawnPoint.rotation;

        var pc = playerTransform.GetComponent<PlayerController>();
        if (pc != null)
            pc.ResetCruiseDirectionToForward();
    }

    private IEnumerator TemporaryInvulnerability()
    {
        if (reviveInvulSeconds <= 0f) yield break;
        yield return new WaitForSeconds(reviveInvulSeconds);
    }

    // =========================
    // IGameOverClient (NEW)
    // =========================
    public void PauseOnFail()
    {
        // Esto lo llamará el flow cuando decida mostrar Offer
        isPausedByOffer = true;

        // Congelamos enemigos
        Enemy.GlobalFreeze = true;

        // OJO: aquí NO toques Time.timeScale (lo gestiona tu slowmo y el flow)
    }

    public void Revive()
    {
        // Revive centralizado: reactivamos gameplay
        isPausedByOffer = false;
        isGameOver = false;

        // Reanudar enemigos
        Enemy.GlobalFreeze = false;

        EnablePlayerAfterRevive();
        ResetPlayerToSpawn();

        if (turboController != null)
            turboController.ResetTurbo();

        StartCoroutine(TemporaryInvulnerability());
    }

    public void FinalGameOver()
    {
        // Aquí hacemos exactamente tu GameOver real
        SoundManager.Instance.PlaySound(gameOverAudioClip, 1f);
        DoGameOverLogic();
    }

    // =========================
    // GameOver real (sin anuncio)
    // =========================
    private void DoGameOverLogic()
    {
        Debug.Log("GAME OVER!");

        if (score > 20)
            AvatarUnlockHelper.UnlockAvatar("Desbloqueable");

        if (DodgeState.Instance != null)
            DodgeState.Instance.dodgeGameState = DodgeState.DodgeGameStateEnum.GameOver;

        OnGameOver?.Invoke(this, EventArgs.Empty);

        SaveRecordIfNeeded();

        if (PlayFabLoginManager.Instance != null && PlayFabLoginManager.Instance.IsLoggedIn)
            PlayFabScoreManager.Instance.SubmitScore("DodgeScore", score);

        int coinsEarned = score / 3;

        CoinsRewardUI rewardUI = FindObjectOfType<CoinsRewardUI>(true);
        if (rewardUI != null)
        {
            rewardUI.ShowReward(coinsEarned);
        }
        else
        {
            CurrencyManager.Instance.AddCoins(coinsEarned);
        }

        if (DailyMissionManager.Instance != null)
        {
            DailyMissionManager.Instance.AddProgress("juega_1_partida", 1);
            DailyMissionManager.Instance.AddProgress("juega_3_partidas", 1);
            DailyMissionManager.Instance.AddProgress("juega_8_partidas", 1);
            DailyMissionManager.Instance.AddProgress("juega_10_partidas", 1);
            DailyMissionManager.Instance.AddProgress("juega_1_partida_nave", 1);
        }
    }

    private void SaveRecordIfNeeded()
    {
        int currentRecord = PlayerPrefs.GetInt("MaxRecordDodge", 0);
        if (score > currentRecord)
        {
            PlayerPrefs.SetInt("MaxRecordDodge", score);
            PlayerPrefs.Save();
        }
    }

    public int GetScore() => score;
}
