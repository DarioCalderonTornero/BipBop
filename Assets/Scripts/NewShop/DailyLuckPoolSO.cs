using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Store/Daily Luck Pool")]
public class DailyLuckPoolSO : ScriptableObject
{
    [Tooltip("Fondos que pueden tocar (lista personalizada).")]
    public List<BackgroundDataSO> possibleBackgrounds = new List<BackgroundDataSO>();

    [Tooltip("Si quieres que la lista se repita en el spin para que se vea más variado.")]
    public bool allowDuplicatesInSpin = true;
}
