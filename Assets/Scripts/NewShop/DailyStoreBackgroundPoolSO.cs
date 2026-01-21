using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Daily Store Background Pool")]
public class DailyStoreBackgroundPoolSO : ScriptableObject
{
    public List<BackgroundDataSO> possibleBackgrounds = new List<BackgroundDataSO>();
}
