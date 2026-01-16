using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class DodgeManager : MonoBehaviour
{
    public static DodgeManager Instance;

    public int score = 0;
    public float CurrentEnemySpeed = 2f;

    [SerializeField] private TextMeshProUGUI scoreText;

    [SerializeField] private TurboController turboController;

    public event EventHandler OnGameOver;
    public event EventHandler OnVideo;

    [Header("FX")]
    public GameObject[] asteroidExplosionPrefabs;
    public GameObject playerExplosionPrefab;

    [Header("Player")]
    [SerializeField] private Transform playerTransform;          
    [SerializeField] private Transform playerSpawnPoint;        
    [SerializeField] private float reviveInvulSeconds = 2f;      

    [Header("Revive Countdown UI")]
    [SerializeField] private ReviveCountdownUI reviveCountdownUI;

    [Header("Sounds")]
    [SerializeField] private AudioClip gameOverAudioClip;


    private bool isGameOver = false;

    // 1 revive máximo por partida
    private bool hasUsedReviveOffer = false;

    public enum DeathType
    {
        None,
        Video,
        GameOver
    }

    public DeathType deathType { get; private set; } = DeathType.None;

    private void Awake()
    {
        Instance = this;

        if (scoreText != null)
            scoreText.text = $"{score}";

        Enemy.GlobalFreeze = false;
        deathType = DeathType.None;

        // por seguridad
        isGameOver = false;
        hasUsedReviveOffer = false;
    }

    private void Start()
    {
        Time.timeScale = 1f;
        hasUsedReviveOffer = false;
    }

    // =========================
    //  Puntos / colisiones enemy
    // =========================
    public void EnemiesCollided(GameObject e1, GameObject e2)
    {
        if (isGameOver) return;
        if (deathType != DeathType.None) return; // si estás en Video/GameOver, no sumar

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
        // Si ya estamos en "muerte intermedia" o gameover, no repetir
        if (isGameOver) return;
        if (deathType != DeathType.None) return;

        StartCoroutine(SlowMotionAndThenDecide(killer));
    }

    private IEnumerator SlowMotionAndThenDecide(Enemy killer)
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

        Time.timeScale = prevTimeScale;
        Time.fixedDeltaTime = prevFixedDelta;

        DecideDeathType();
    }

    private void DisablePlayerForDeath()
    {
        if (playerTransform == null) return;

        // opción simple y robusta:
        // - desactiva el GO entero (se deja de mover, de colisionar, de renderizar)
        playerTransform.gameObject.SetActive(false);
    }

    private void EnablePlayerAfterRevive()
    {
        if (playerTransform == null) return;
        playerTransform.gameObject.SetActive(true);
    }

    // =========================
    //  Decide muerte (Video/GameOver)
    // =========================
    private void DecideDeathType()
    {
        if (deathType != DeathType.None) return;

        // Si ya salió un revive en esta partida -> GameOver directo
        if (hasUsedReviveOffer)
        {
            SetDeathType(DeathType.GameOver);
            return;
        }

        float p = UnityEngine.Random.Range(0, 10);
        if (p >= 1)
        {
            hasUsedReviveOffer = true;
            SetDeathType(DeathType.Video);
        }
        else
        {
            SetDeathType(DeathType.GameOver);
        }
    }

    public void SetDeathType(DeathType newType)
    {
        if (deathType == newType) return;

        deathType = newType;

        SoundManager.Instance.PlaySound(gameOverAudioClip, 1f);

        switch (deathType)
        {
            case DeathType.Video:
                OnVideo?.Invoke(this, EventArgs.Empty);
                break;

            case DeathType.GameOver:
                DestroyPlayerIfExists();
                DoGameOverLogic();
                break;
        }
    }

    private void DestroyPlayerIfExists()
    {
        if (playerTransform == null) return;

        Destroy(playerTransform.gameObject);
        playerTransform = null;
    }

    // Llamado por DodgeVideoGameOver cuando el rewarded termina
    public void StartReviveCountdown()
    {
        if (deathType != DeathType.Video) return;

        if (reviveCountdownUI == null)
        {
            ReviveNow();
            return;
        }

        reviveCountdownUI.Play(ReviveNow);
    }

    private void ReviveNow()
    {
        if (deathType != DeathType.Video) return;

        // Volvemos a jugar
        deathType = DeathType.None;
        isGameOver = false;

        // Reactivar gameplay
        Enemy.GlobalFreeze = false;

        EnablePlayerAfterRevive();
        ResetPlayerToSpawn();
        turboController.ResetTurbo();

        StartCoroutine(TemporaryInvulnerability());
    }


    private void ResetPlayerToSpawn()
    {
        if (playerTransform == null || playerSpawnPoint == null) return;

        playerTransform.position = playerSpawnPoint.position;
        playerTransform.rotation = playerSpawnPoint.rotation;

        // si tu PlayerController guarda target interno, resetealo para que no “salte”
        var pc = playerTransform.GetComponent<PlayerController>();
        if (pc != null)
            pc.ResetCruiseDirectionToForward();
    }

    private IEnumerator TemporaryInvulnerability()
    {
        if (reviveInvulSeconds <= 0f) yield break;

        // Placeholder: aquí activarías invulnerabilidad real si tu player la tiene
        yield return new WaitForSeconds(reviveInvulSeconds);
    }

    public void GameOver()
    {
        if (isGameOver) return;

        isGameOver = true;
        SetDeathType(DeathType.GameOver);
    }

    private void DoGameOverLogic()
    {
        Debug.Log("GAME OVER!");

        if (score > 20)
            AvatarUnlockHelper.UnlockAvatar("Desbloqueable");

        DodgeState.Instance.dodgeGameState = DodgeState.DodgeGameStateEnum.GameOver;
        OnGameOver?.Invoke(this, EventArgs.Empty);

        SaveRecordIfNeeded();

        if (PlayFabLoginManager.Instance != null && PlayFabLoginManager.Instance.IsLoggedIn)
            PlayFabScoreManager.Instance.SubmitScore("DodgeScore", score);

        int coinsEarned = score/3;

        CoinsRewardUI rewardUI = FindObjectOfType<CoinsRewardUI>(true);
        if (rewardUI != null)
        {
            rewardUI.ShowReward(coinsEarned);
        }
        else
        {
            CurrencyManager.Instance.AddCoins(coinsEarned);
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
