using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Avatar/Catalog")]
public class AvatarCatalogSO : ScriptableObject
{
    public List<AvatarDataSO> avatarDataSO;  // Lista de todos los avatares disponibles
}
