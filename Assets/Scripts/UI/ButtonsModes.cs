using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ButtonsModes : MonoBehaviour
{
    [SerializeField] private Button ClassicButton;
    [SerializeField] private Button GeometricButton;
    [SerializeField] private Button ColorButton;
    [SerializeField] private Button DodgeButton;

    void Awake()
    {
        ClassicButton.onClick.AddListener(() =>
        {
            SceneManager.LoadScene("GameScene");
            DailyMissionManager.Instance.AddProgress("juega_1_partida", 1);
            DailyMissionManager.Instance.AddProgress("juega_3_partidas", 1);
        });

        GeometricButton.onClick.AddListener(() =>
        {
            SceneManager.LoadScene("GeometricScene");
            DailyMissionManager.Instance.AddProgress("juega_1_partida", 1);
            DailyMissionManager.Instance.AddProgress("juega_3_partidas", 1);
        });

        ColorButton.onClick.AddListener(() =>
        {
            SceneManager.LoadScene("ColorScene");
            DailyMissionManager.Instance.AddProgress("juega_1_partida", 1);
            DailyMissionManager.Instance.AddProgress("juega_3_partidas", 1);
        });

        DodgeButton.onClick.AddListener(() =>
        {
            SceneManager.LoadScene("DodgeScene");
            DailyMissionManager.Instance.AddProgress("juega_1_partida", 1);
            DailyMissionManager.Instance.AddProgress("juega_3_partidas", 1);
        });
    }

}
