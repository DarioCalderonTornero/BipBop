using System.Collections.Generic;
using UnityEngine;

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
    public string displayName;
    public Sprite sprite;
    public int price;

    [Header("Desbloqueo por puntuación (opcional)")]
    public bool unlockByScore = false;
    public string requiredScoreKey;
    public int requiredScoreValue = 0;

    [Header("Desbloqueo por colección (opcional)")]
    public bool unlockByOwningAvatars = false;

    [Tooltip("IDs de avatares que debes poseer para desbloquear este avatar")]
    public List<string> requiredOwnedAvatarIds = new List<string>();

    [Header("Descripción de desbloqueo")]
    [TextArea] public string unlockDescription;

    [Header("Efecto visual (shader)")]
    public bool hasShaderEffect = false;
    public AvatarShaderEffectType effectType = AvatarShaderEffectType.None;
    public Material effectMaterial;

    public AvatarFxSO fxPreset;   // opcional

    [Header("Mensajes personalizados")]
    [TextArea] public string lockedMessage;  
    [TextArea] public string ownedMessage;
}
