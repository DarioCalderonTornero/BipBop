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
        Haptics.TryVibrate(); // Vibración opcional
#endif

        // Incrementar dificultad
        if (score >= 50) CurrentEnemySpeed = 3.5f;
        else if (score >= 30) CurrentEnemySpeed = 2.5f;
    }

    public void GameOver()
    {
        if (isGameOver)
            return;

        isGameOver = true;
        Debug.Log("GAME OVER!");

        Time.timeScale = 0f;

        // Guardar récord máximo
        SaveRecordIfNeeded();

        // Guardar puntuación en PlayFab
        if (PlayFabLoginManager.Instance != null && PlayFabLoginManager.Instance.IsLoggedIn)
            PlayFabScoreManager.Instance.SubmitScore("DodgeScore", score);

        // Guardar monedas ganadas (1 cada 15 puntos)
        int coinsEarned = score / 15;
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

        // Notificar fin de partida
        OnGameOver?.Invoke(this, EventArgs.Empty);
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
}
