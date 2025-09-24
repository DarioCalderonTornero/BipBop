using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MainGamePoints : MonoBehaviour
{
    public static MainGamePoints Instance { get; private set; }
    [SerializeField] private TextMeshProUGUI scoreText;
    private int score = -1;

    private void Awake()
    {
        Instance = this;    
    }

    private void Start()
    {
        scoreText.text = "Puntos: 0";
    }

    public int GetScore()
    {
        return score;
    }

    public int AddScore()
    {
        return score++;
    }

    public void ShowScore()
    {
        scoreText.text = "Puntos " + score;
    }

    public void SafeRecordIfNeeded()
    {
        int currentRecord = PlayerPrefs.GetInt("MaxRecord", 0);

        if (score > currentRecord)
        {
            PlayerPrefs.SetInt("MaxRecord", score);
            PlayerPrefs.Save();
        }
    }
}
