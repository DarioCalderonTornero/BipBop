using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ColorGamePuntos : MonoBehaviour
{
    
    public static ColorGamePuntos Instance { get; private set; }
    [SerializeField] private TextMeshProUGUI scoreText;
    private int score = 0;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        scoreText.text = "Puntos: 0";
    }

    private void Update()
    {
        //scoreText.text = "Puntos" + score;  
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
        scoreText.text = "Puntos:  " + score;
    }

    public void SafeRecordIfNeeded()
    {
        int maxRecordColor = PlayerPrefs.GetInt("MaxRecordColor", 0);
        if (score > maxRecordColor)
        {
            PlayerPrefs.SetInt("MaxRecordColor", score);
            PlayerPrefs.Save();
        }
    }
}

