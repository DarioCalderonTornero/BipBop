using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DodgeManager : MonoBehaviour
{
    public static DodgeManager Instance;

    public int score = 0;
    public float CurrentEnemySpeed = 3f;

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

        score += 2;
        scoreText.text = $"Score: {score}";

#if UNITY_ANDROID || UNITY_IOS
        Haptics.TryVibrate(); // Usa tu propio sistema de vibración
#endif

        // Incrementar dificultad
        if (score >= 50) CurrentEnemySpeed = 4f;
        else if (score >= 30) CurrentEnemySpeed = 3.5f;
    }

    public void GameOver()
    {
        if (isGameOver)
            return;

        isGameOver = true;
        Debug.Log("GAME OVER!");

        Time.timeScale = 0f;

        // Guardar puntuación en PlayFab
        PlayFabScoreManager.Instance.SubmitScore("DodgeScore", score);

        // Guardar monedas ganadas (por ejemplo 1 cada 15 puntos)
        int coinsEarned = score / 15;
        int totalCoins = PlayerPrefs.GetInt("CoinCount", 0);
        totalCoins += coinsEarned;
        PlayerPrefs.SetInt("CoinCount", totalCoins);
        PlayerPrefs.Save();

        // Mostramos la animación de recompensa si existe el panel en la escena
        CoinsRewardUI rewardUI = FindObjectOfType<CoinsRewardUI>(true);
        if (rewardUI != null)
        {
            rewardUI.ShowReward(coinsEarned);
        }
        else
        {
            // Fallback: suma directa sin animación
            CurrencyManager.Instance.AddCoins(coinsEarned);
        }

        // Invocar evento para mostrar UI, reiniciar, etc.
        OnGameOver?.Invoke(this, EventArgs.Empty);
    }
}
