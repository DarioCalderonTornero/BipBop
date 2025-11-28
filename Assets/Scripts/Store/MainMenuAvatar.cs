using UnityEngine;
using UnityEngine.UI;

public class MainMenuAvatar : MonoBehaviour
{
    [Header("UI")]
    public Image avatarImage; // La imagen del menú donde se muestra el avatar equipado

    [Header("Datos")]
    public AvatarDataSO[] avatarDatabase; // Arrastrar TODOS los SO de avatares aquí

    private void Start()
    {
        LoadEquippedAvatar();
    }

    public void LoadEquippedAvatar()
    {
        // Leer el ID del avatar equipado
        string equippedId = PlayerPrefs.GetString("EquippedAvatarId", "NormalAvatar");

        // Buscar ese avatar en la lista
        AvatarDataSO data = GetAvatarById(equippedId);

        if (data != null)
        {
            avatarImage.sprite = data.sprite;
        }
        else
        {
            Debug.LogWarning("No se encontró avatar equipado, usando NormalAvatar");
            AvatarDataSO fallback = GetAvatarById("NormalAvatar");
            if (fallback != null)
                avatarImage.sprite = fallback.sprite;
        }
    }

    private AvatarDataSO GetAvatarById(string id)
    {
        foreach (var avatar in avatarDatabase)
        {
            if (avatar.id == id)
                return avatar;
        }
        return null;
    }
}
