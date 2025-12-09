using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

public class AvatarUnlockHelper : MonoBehaviour
{
    /// <summary>
    /// Desbloquea un avatar (local + PlayFab) y, si era nuevo, muestra el popup.
    /// </summary>
    public static void UnlockAvatar(string avatarId)
    {
        string key = "AvatarPurchased_" + avatarId;

        // ¿Ya lo teníamos?
        bool alreadyOwned = PlayerPrefs.GetInt(key, 0) == 1;

        // Si ya era nuestro, no hacemos nada (ni popup)
        if (alreadyOwned)
            return;

        // Marcar como comprado localmente
        PlayerPrefs.SetInt(key, 1);
        PlayerPrefs.Save();

        Debug.Log("[AvatarUnlockHelper] Avatar desbloqueado localmente: " + avatarId);

        // Mostrar popup (si hay instancia en la escena)
        AvatarUnlockPopup.TryShow(avatarId);

        // Mandar a PlayFab si estamos logueados
        if (PlayFabLoginManager.Instance != null &&
            PlayFabLoginManager.Instance.IsLoggedIn)
        {
            var data = new Dictionary<string, string>
            {
                { key, "1" }
            };

            var request = new UpdateUserDataRequest
            {
                Data = data,
                Permission = UserDataPermission.Public
            };

            PlayFabClientAPI.UpdateUserData(
                request,
                res => Debug.Log("[AvatarUnlockHelper] Enviado a PlayFab: " + key + " = 1"),
                err => Debug.LogWarning("[AvatarUnlockHelper] Error al sincronizar avatar: " + err.GenerateErrorReport())
            );
        }
    }
}
