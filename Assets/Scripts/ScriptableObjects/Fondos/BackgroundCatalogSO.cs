using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Background/Catalog")]
public class BackgroundCatalogSO : ScriptableObject
{
    public List<BackgroundDataSO> backgroundDataSO;
}
