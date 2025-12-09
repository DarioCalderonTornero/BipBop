using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

public class ResetAvatarPrefs : MonoBehaviour
{
    [Header("Catálogo de avatares (el mismo que usas en inventario/tienda)")]
    [SerializeField] private AvatarCatalogSO avatarCatalog;

    [Header("ID del avatar por defecto (el que siempre debe estar comprado/equipado)")]
    [SerializeField] private string defaultAvatarId = "NormalAvatar";

    // --- 1. BORRAR SOLO PLAYERPREFS DE AVATARES ---

    [ContextMenu("AvatarDebug / Reset LOCAL avatar prefs")]
    public void ResetLocalAvatarPrefs()
    {
        if (avatarCatalog == null)
        {
            Debug.LogWarning("AvatarDebugTools: avatarCatalog no asignado.");
            return;
        }

        foreach (var av in avatarCatalog.avatarDataSO)
        {
            if (av == null) continue;

            string key = "AvatarPurchased_" + av.id;
            PlayerPrefs.DeleteKey(key);
        }

        PlayerPrefs.DeleteKey("EquippedAvatarId");

        PlayerPrefs.Save();
        Debug.Log("AvatarDebug: PlayerPrefs de avatares reseteados.");

        // ⚠️ IMPORTANTE: volvemos a dejar el avatar por defecto "comprado" y "equipado"
        PlayerPrefs.SetInt("AvatarPurchased_" + defaultAvatarId, 1);
        PlayerPrefs.SetString("EquippedAvatarId", defaultAvatarId);
        PlayerPrefs.Save();

        Debug.Log($"AvatarDebug: Marcado {defaultAvatarId} como comprado + equipado en PlayerPrefs.");
    }

    // --- 2. LOG LOCAL PARA VER CÓMO QUEDA ---

    [ContextMenu("AvatarDebug / Log LOCAL avatar prefs")]
    public void LogLocalAvatarPrefs()
    {
        if (avatarCatalog == null)
        {
            Debug.LogWarning("AvatarDebugTools: avatarCatalog no asignado.");
            return;
        }

        string equipped = PlayerPrefs.GetString("EquippedAvatarId", "(none)");
        Debug.Log($"AvatarDebug: EquippedAvatarId = {equipped}");

        foreach (var av in avatarCatalog.avatarDataSO)
        {
            if (av == null) continue;

            string key = "AvatarPurchased_" + av.id;
            int val = PlayerPrefs.GetInt(key, 0);
            Debug.Log($"AvatarDebug: {key} = {val}");
        }
    }

    // --- 3. BORRAR SOLO DATOS DE AVATARES EN PLAYFAB ---

    [ContextMenu("AvatarDebug / Reset REMOTE avatar data (PlayFab)")]
    public void ResetRemoteAvatarData()
    {
        if (PlayFabLoginManager.Instance == null || !PlayFabLoginManager.Instance.IsLoggedIn)
        {
            Debug.LogWarning("AvatarDebug: No logueado en PlayFab, no puedo resetear remoto.");
            return;
        }

        if (avatarCatalog == null)
        {
            Debug.LogWarning("AvatarDebugTools: avatarCatalog no asignado.");
            return;
        }

        var keysToRemove = new List<string>();

        foreach (var av in avatarCatalog.avatarDataSO)
        {
            if (av == null) continue;
            keysToRemove.Add("AvatarPurchased_" + av.id);
        }

        keysToRemove.Add("EquippedAvatarIdPublic");

        var request = new UpdateUserDataRequest
        {
            KeysToRemove = keysToRemove
        };

        PlayFabClientAPI.UpdateUserData(
            request,
            res => Debug.Log("AvatarDebug: Datos remotos de avatares reseteados en PlayFab."),
            err => Debug.LogWarning("AvatarDebug: Error al resetear datos remotos: " + err.GenerateErrorReport())
        );
    }

    // --- 4. LOG REMOTO (para ver qué tiene PlayFab) ---

    [ContextMenu("AvatarDebug / Log REMOTE avatar data (PlayFab)")]
    public void LogRemoteAvatarData()
    {
        if (PlayFabLoginManager.Instance == null || !PlayFabLoginManager.Instance.IsLoggedIn)
        {
            Debug.LogWarning("AvatarDebug: No logueado en PlayFab, no puedo leer remoto.");
            return;
        }

        PlayFabClientAPI.GetUserData(
            new GetUserDataRequest(),
            res =>
            {
                if (res.Data == null)
                {
                    Debug.Log("AvatarDebug: UserData vacío.");
                    return;
                }

                Debug.Log("AvatarDebug: --- UserData actual ---");

                foreach (var kvp in res.Data)
                {
                    if (kvp.Key.StartsWith("AvatarPurchased_") ||
                        kvp.Key == "EquippedAvatarIdPublic")
                    {
                        Debug.Log($"    {kvp.Key} = {kvp.Value.Value}");
                    }
                }
            },
            err => Debug.LogWarning("AvatarDebug: Error al leer UserData: " + err.GenerateErrorReport())
        );
    }
}
