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

    private void Awake()
    {
        Instance = this;
        scoreText.text = $"Score: {score}";
    }

    public void EnemiesCollided(GameObject e1, GameObject e2)
    {
        Destroy(e1);
        Destroy(e2);

        score += 2;
        scoreText.text = $"Score: {score}";

        // Subir dificultad
        if (score >= 50) CurrentEnemySpeed = 4f;
        else if (score >= 30) CurrentEnemySpeed = 3.5f;
    }

    public void GameOver()
    {
        Debug.Log(" Game Over!");
        Time.timeScale = 0f;
    }
}
