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

    private bool isGameOver = false;

    private void Awake()
    {
        Instance = this;
        scoreText.text = $"Score: {score}";

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

        Destroy(e1);
        Destroy(e2);

        score += 1;
        scoreText.text = $"Score: {score}";

#if UNITY_ANDROID || UNITY_IOS
        Haptics.TryVibrate();
#endif
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

        // Guardar tiempos originales
        float prevTimeScale = Time.timeScale;
        float prevFixedDelta = Time.fixedDeltaTime;

        // Cámara lenta
        Time.timeScale = 0.2f;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        // Congelar a todos los enemigos (veremos esto en Enemy)
        Enemy.GlobalFreeze = true;

        // Parpadeo del enemigo que ha dado al jugador
        if (killer != null)
        {
            yield return killer.FlashRedCoroutine(3, 0.15f);
        }
        else
        {
            // Fallback por si no tenemos referencia
            yield return new WaitForSecondsRealtime(0.9f);
        }

        // Restaurar tiempo normal
        Time.timeScale = prevTimeScale;
        Time.fixedDeltaTime = prevFixedDelta;

        // Ejecutar la lógica normal de GameOver (menú, monedas, PlayFab…)
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
