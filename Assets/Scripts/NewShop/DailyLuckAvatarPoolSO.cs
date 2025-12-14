using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "DailyLuck/Avatar Pool")]
public class DailyLuckAvatarPoolSO : ScriptableObject
{
    public List<AvatarDataSO> possibleAvatars = new List<AvatarDataSO>();

    [Tooltip("Solo afecta al efecto visual del spin (si no quieres repetidos visuales).")]
    public bool allowDuplicatesInSpin = true;
}
