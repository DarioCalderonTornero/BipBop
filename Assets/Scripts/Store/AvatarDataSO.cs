using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

public enum AvatarShaderEffectType
{
    None,
    Wave,
    Rotate,
    Distorsion,
}

[CreateAssetMenu(fileName = "NewAvatar", menuName = "Store/Avatar Data")]
public class AvatarDataSO : ScriptableObject
{
    [Header("Datos del avatar")]
    public string id;

    [Header("Nombre (Localización)")]
    public LocalizedString displayNameLS;
    [Tooltip("Fallback por si no hay entry/localización")]
    public string displayNameFallback = "Avatar";

    public Sprite sprite;
    public int price;

    [Header("Desbloqueo por puntuación (opcional)")]
    public bool unlockByScore = false;
    public string requiredScoreKey;
    public int requiredScoreValue = 0;

    [Header("Desbloqueo por colección (opcional)")]
    public bool unlockByOwningAvatars = false;
    public List<string> requiredOwnedAvatarIds = new List<string>();

    [Header("Descripción de desbloqueo (Localización opcional)")]
    public LocalizedString unlockDescriptionLS;
    [TextArea] public string unlockDescriptionFallback;

    [Header("Mensajes personalizados (Localización opcional)")]
    public LocalizedString lockedMessageLS;
    [TextArea] public string lockedMessageFallback;

    public LocalizedString ownedMessageLS;
    [TextArea] public string ownedMessageFallback;

    [Header("Efecto visual (shader)")]
    public bool hasShaderEffect = false;
    public AvatarShaderEffectType effectType = AvatarShaderEffectType.None;
    public Material effectMaterial;

    public AvatarFxSO fxPreset;

    // ---- Helpers (sync) ----
    public string GetDisplayName()
    {
        if (!displayNameLS.IsEmpty)
        {
            string s = displayNameLS.GetLocalizedString();
            if (!string.IsNullOrEmpty(s)) return s;
        }
        return string.IsNullOrEmpty(displayNameFallback) ? id : displayNameFallback;
    }

    public string GetLockedMessage()
    {
        if (!lockedMessageLS.IsEmpty)
        {
            string s = lockedMessageLS.GetLocalizedString();
            if (!string.IsNullOrEmpty(s)) return s;
        }
        return lockedMessageFallback;
    }

    public string GetOwnedMessage()
    {
        if (!ownedMessageLS.IsEmpty)
        {
            string s = ownedMessageLS.GetLocalizedString();
            if (!string.IsNullOrEmpty(s)) return s;
        }
        return ownedMessageFallback;
    }

    public string GetUnlockDescription()
    {
        if (!unlockDescriptionLS.IsEmpty)
        {
            string s = unlockDescriptionLS.GetLocalizedString();
            if (!string.IsNullOrEmpty(s)) return s;
        }
        return unlockDescriptionFallback;
    }
}
