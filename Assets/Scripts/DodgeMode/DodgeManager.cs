using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DodgeManager : MonoBehaviour
{
    public static DodgeManager Instance;

    public int score = 0;
    public float CurrentEnemySpeed = 2f;

    [SerializeField] private TextMeshProUGUI scoreText;

    public event EventHandler OnGameOver;

    [Header("FX")]
    public GameObject[] asteroidExplosionPrefabs; 
    public GameObject playerExplosionPrefab;
    public Transform playerTransform;

    private bool isGameOver = false;

    private void Awake()
    {
        Instance = this;
        scoreText.text = $"{score}";

        // Por si acaso, al entrar en escena nos aseguramos de que no haya freeze antiguo
        Enemy.GlobalFreeze = false;
    }

    private void Start()
    {
        Time.timeScale = 1f;
    }

    public void EnemiesCollided(GameObject e1, GameObject e2)
    {
        if (isGameOver)
            return;

        Enemy enemy1 = e1.GetComponent<Enemy>();
        Enemy enemy2 = e2.GetComponent<Enemy>();

        if (enemy1 != null) SpawnExplosion(enemy1);
        if (enemy2 != null) SpawnExplosion(enemy2);

        // Shake global cuando dos meteoritos explotan
        if (MeteorCameraShake.Instance != null)
            MeteorCameraShake.Instance.Shake(0.18f, 0.35f);

        Destroy(e1);
        Destroy(e2);

        score += 1;
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
        if (prefab == null)
            return;

        GameObject fx = Instantiate(
            prefab,
            enemy.transform.position,
            Quaternion.identity
        );

        // Escala de la explosión igual que el meteorito
        fx.transform.localScale = enemy.transform.localScale;

        Destroy(fx, 3f);
    }

    // =========================
    //  NUEVO: jugador golpeado
    // =========================
    public void PlayerHit(Enemy killer)
    {
        if (isGameOver)
            return;

        StartCoroutine(SlowMotionAndGameOver(killer));
    }

    private IEnumerator SlowMotionAndGameOver(Enemy killer)
    {
        isGameOver = true;
        Debug.Log("GAME OVER (slow motion)!");

        float prevTimeScale = Time.timeScale;
        float prevFixedDelta = Time.fixedDeltaTime;

        // Cámara lenta
        Time.timeScale = 0.1f;
        Time.fixedDeltaTime = 0.01f * Time.timeScale;

        // Congelar movimiento de enemigos
        Enemy.GlobalFreeze = true;

        // 1) Parpadeo rojo del meteorito que ha golpeado
        if (killer != null)
        {
            yield return killer.FlashRedCoroutine(3, 0.15f);
        }
        else
        {
            yield return new WaitForSecondsRealtime(0.9f);
        }

        // 2) Explosión del meteorito (normal)
        if (killer != null)
        {
            SpawnExplosion(killer);
            Destroy(killer.gameObject);
        }

        // Shake también en la muerte de la nave
        if (MeteorCameraShake.Instance != null)
            MeteorCameraShake.Instance.Shake(0.2f, 0.4f);

        // 3) Explosión de la nave (azul)
        if (playerExplosionPrefab != null && playerTransform != null)
        {
            GameObject fxPlayer = Instantiate(
                playerExplosionPrefab,
                playerTransform.position,
                Quaternion.identity
            );

            // NO tocar useUnscaledTime -> respeta la cámara lenta

            Destroy(fxPlayer, 2.5f); // ojo: en slowmo se verá 10x más largo, ajusta si hace falta
        }

        // 4) Destruir la nave (después de spawnear la explosión)
        if (playerTransform != null)
        {
            Destroy(playerTransform.gameObject);
        }

        // 5) Pequeña pausa para que se vea todo el FX en cámara lenta
        yield return new WaitForSecondsRealtime(0.6f);

        // Restaurar tiempo normal
        Time.timeScale = prevTimeScale;
        Time.fixedDeltaTime = prevFixedDelta;

        // Lógica normal de GameOver
        DoGameOverLogic();
    }

    // =========================
    //  GameOver normal (sin cámara lenta)
    // =========================
    public void GameOver()
    {
        if (isGameOver)
            return;

        isGameOver = true;
        DoGameOverLogic();
    }

    // Extraemos aquí la lógica que ya tenías en GameOver
    private void DoGameOverLogic()
    {
        Debug.Log("GAME OVER!");

        if (score > 20)
        {
            AvatarUnlockHelper.UnlockAvatar("Desbloqueable");
        }

        DodgeState.Instance.dodgeGameState = DodgeState.DodgeGameStateEnum.GameOver;
        OnGameOver?.Invoke(this, EventArgs.Empty);

        // Guardar récord máximo
        SaveRecordIfNeeded();

        // Guardar puntuación en PlayFab
        if (PlayFabLoginManager.Instance != null && PlayFabLoginManager.Instance.IsLoggedIn)
            PlayFabScoreManager.Instance.SubmitScore("DodgeScore", score);

        // Guardar monedas ganadas (1 cada 15 puntos)
        int coinsEarned = score;
        int totalCoins = PlayerPrefs.GetInt("CoinCount", 0);
        totalCoins += coinsEarned;
        PlayerPrefs.SetInt("CoinCount", totalCoins);
        PlayerPrefs.Save();

        // Mostrar animación de recompensa
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

    public int GetScore()
    {
        return score;
    }
}
