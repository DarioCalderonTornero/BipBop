using UnityEngine;
using UnityEngine.Localization;

[CreateAssetMenu(menuName = "Tienda/Fondo")]
public class BackgroundDataSO : ScriptableObject
{
    public string id;
    public Sprite sprite;
    public int price;

    [Header("Localization")]
    public LocalizedString displayNameLS;        // Nombre
    public LocalizedString unlockDescriptionLS;  // Texto si NO está conseguido
    public LocalizedString ownedDescriptionLS;   // Texto si SÍ está conseguido

    // Helpers simples (sync)
    public string GetDisplayName(string fallback = "Fondo")
        => displayNameLS.IsEmpty ? fallback : displayNameLS.GetLocalizedString();

    public string GetUnlockDescription(string fallback = "")
        => unlockDescriptionLS.IsEmpty ? fallback : unlockDescriptionLS.GetLocalizedString();

    public string GetOwnedDescription(string fallback = "¡Conseguido!")
        => ownedDescriptionLS.IsEmpty ? fallback : ownedDescriptionLS.GetLocalizedString();
}
