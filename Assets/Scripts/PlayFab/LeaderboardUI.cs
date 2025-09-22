using System.Collections.Generic;
using PlayFab.ClientModels;
using TMPro;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.UI;

public class LeaderboardUI : MonoBehaviour
{
    public static LeaderboardUI Instance { get; private set; }

    [Header("UI References")]
    public GameObject panel;             // Panel principal del leaderboard
    public Transform contentParent;      // Content del ScrollView
    public GameObject playerRowPrefab;   // Prefab con Texts para Rank, Name, Score
    public Button closeButton;           // Botón para cerrar

    private Button openButton;           // Botón para abrir el ranking (por nombre)

    private void Awake()
    {
        // Singleton
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // Persistir entre escenas
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Seguridad: desactivar panel al inicio
        if (panel != null) panel.SetActive(false);

        // Asignar listener de cierre (ya asignado manualmente en inspector)
        if (closeButton != null)
            closeButton.onClick.AddListener(() => panel.SetActive(false));

        // Buscar el botón de abrir ranking por nombre en la escena
        GameObject openButtonObj = GameObject.Find("ButtonLeaderScore"); // <- reemplaza con el nombre real del botón
        if (openButtonObj != null)
        {
            openButton = openButtonObj.GetComponent<Button>();
            if (openButton != null)
            {
                openButton.onClick.AddListener(OnOpenButtonClicked);
            }
            else
            {
                Debug.LogWarning("LeaderboardUI: No se encontró componente Button en BotonRanking.");
            }
        }
        else
        {
            Debug.LogWarning("LeaderboardUI: No se encontró objeto con nombre 'BotonRanking'.");
        }
    }

    private void OnOpenButtonClicked()
    {
        ShowLeaderboard("HighScore", 10); // Cambia el nombre de la estadística si hace falta
    }

    /// <summary>
    /// Abre el panel y carga el top N del leaderboard
    /// </summary>
    public void ShowLeaderboard(string statisticName, int top = 10)
    {
        if (panel == null || contentParent == null || playerRowPrefab == null)
        {
            Debug.LogWarning("LeaderboardUI: Falta asignar referencias en el inspector.");
            return;
        }

        panel.SetActive(true);

        // Limpiar filas anteriores
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        // Pedir leaderboard a PlayFab
        PlayFabScoreManager.Instance.GetLeaderboard(statisticName, top, leaderboard =>
        {
            for (int i = 0; i < leaderboard.Count; i++)
            {
                var entry = leaderboard[i];
                GameObject row = Instantiate(playerRowPrefab, contentParent);
                var texts = row.GetComponentsInChildren<TextMeshProUGUI>();

                if (texts.Length >= 3)
                {
                    texts[0].text = (entry.Position + 1).ToString();        // Rank
                    texts[1].text = entry.DisplayName ?? "Player";          // Nombre
                    texts[2].text = entry.StatValue.ToString();            // Score
                }
            }
        });
    }

    /// <summary>
    /// Método opcional para cerrar desde código
    /// </summary>
    public void CloseLeaderboard()
    {
        if (panel != null)
            panel.SetActive(false);
    }
}
