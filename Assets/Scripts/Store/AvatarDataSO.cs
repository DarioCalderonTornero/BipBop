using UnityEngine;

[CreateAssetMenu(fileName = "NewAvatar", menuName = "Store/Avatar Data")]
public class AvatarDataSO : ScriptableObject
{
    [Header("Datos del avatar")]
    public string id;            // ID único (por ejemplo "NormalAvatar", "Perro01")
    public string displayName;   // Nombre visible ("Avatar básico", "Perro feliz")
    public Sprite sprite;        // Imagen del avatar
    public int price;            // Precio en monedas
}
