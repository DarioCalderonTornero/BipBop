using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

public static class AvatarCollectionUnlocker
{
    public static bool IsOwned(string avatarId)
        => PlayerPrefs.GetInt("AvatarPurchased_" + avatarId, 0) == 1;

    public static bool TryUnlock(AvatarDataSO avatar, bool syncPlayFabIfLoggedIn)
    {
        if (avatar == null) return false;
        if (!avatar.unlockByOwningAvatars) return false;

        // ya lo tiene
        if (IsOwned(avatar.id)) return false;

        // requisitos
        if (avatar.requiredOwnedAvatarIds == null || avatar.requiredOwnedAvatarIds.Count == 0)
            return false;

        for (int i = 0; i < avatar.requiredOwnedAvatarIds.Count; i++)
        {
            string req = avatar.requiredOwnedAvatarIds[i];
            if (string.IsNullOrEmpty(req) || !IsOwned(req))
                return false;
        }

        // ✅ desbloquear
        PlayerPrefs.SetInt("AvatarPurchased_" + avatar.id, 1);
        PlayerPrefs.Save();

        // (opcional) sync PlayFab igual que haces en AvatarItem
        if (syncPlayFabIfLoggedIn && PlayFabLoginManager.Instance != null && PlayFabLoginManager.Instance.IsLoggedIn)
        {
            var data = new Dictionary<string, string>
            {
                { "AvatarPurchased_" + avatar.id, "1" }
            };

            var request = new UpdateUserDataRequest
            {
                Data = data,
                Permission = UserDataPermission.Public
            };

            PlayFabClientAPI.UpdateUserData(
                request,
                r => Debug.Log($"[AvatarCollectionUnlocker] Enviado a PlayFab AvatarPurchased_{avatar.id}=1"),
                e => Debug.LogWarning("[AvatarCollectionUnlocker] Error PlayFab: " + e.GenerateErrorReport())
            );
        }

        return true;
    }

    public static int EvaluateCatalog(List<AvatarDataSO> allAvatars, bool syncPlayFabIfLoggedIn)
    {
        int unlocked = 0;
        if (allAvatars == null) return 0;

        foreach (var a in allAvatars)
            if (TryUnlock(a, syncPlayFabIfLoggedIn))
                unlocked++;

        return unlocked;
    }
}
