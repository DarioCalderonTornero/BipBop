using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Daily Store Avatar Pool")]
public class DailyStoreAvatarPoolSO : ScriptableObject
{
    public List<AvatarDataSO> possibleAvatars = new List<AvatarDataSO>();
}
