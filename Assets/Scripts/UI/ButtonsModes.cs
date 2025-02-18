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

    void Awake()
    {
        ClassicButton.onClick.AddListener(() =>
        {
            SceneManager.LoadScene("GameScene");
        });

        GeometricButton.onClick.AddListener(() =>
        {
            SceneManager.LoadScene("GeometricScene");
        });

        ColorButton.onClick.AddListener(() =>
        {
            SceneManager.LoadScene("ColorScene");
        });
    }

}
