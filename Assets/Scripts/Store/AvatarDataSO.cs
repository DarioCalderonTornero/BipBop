using UnityEngine;

[CreateAssetMenu(fileName = "NewAvatar", menuName = "Store/Avatar Data")]
public class AvatarDataSO : ScriptableObject
{
    [Header("Datos del avatar")]
    public string id;            // ID único (por ejemplo "NormalAvatar", "Perro01")
    public string displayName;   // Nombre visible
    public Sprite sprite;        // Imagen del avatar
    public int price;            // Precio en monedas

    [Header("Desbloqueo por puntuación (opcional)")]
    public bool unlockByScore = false;

    [Tooltip("Clave del récord en PlayerPrefs (ej: MaxRecordColor, MaxRecord, MaxRecordGeometric...)")]
    public string requiredScoreKey;

    [Tooltip("Puntos necesarios para desbloquear este avatar")]
    public int requiredScoreValue = 0;

    [Header("Descripción de desbloqueo (texto que se mostrará cuando NO está conseguido)")]
    [TextArea]
    public string unlockDescription;
}
