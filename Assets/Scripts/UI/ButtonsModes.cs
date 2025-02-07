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
    }

}
