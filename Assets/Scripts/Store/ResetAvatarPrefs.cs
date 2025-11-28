using UnityEngine;

public class ResetAvatarPrefs : MonoBehaviour
{
    [Header("Todos los AvatarDataSO del juego")]
    public AvatarDataSO[] allAvatars;

    private void Awake()
    {
        ResetAvatarData();
    }

    public void ResetAvatarData()
    {
        // Borrar el equipado
        PlayerPrefs.DeleteKey("EquippedAvatarId");

        // Borrar compras
        foreach (var avatar in allAvatars)
        {
            string key = "AvatarPurchased_" + avatar.id;
            PlayerPrefs.DeleteKey(key);
        }

        PlayerPrefs.Save();
        Debug.Log("PlayerPrefs de avatares borradas correctamente.");
    }
}
