using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Tienda/Fondo")]
public class BackgroundDataSO : ScriptableObject
{
    public string id;
    public Sprite sprite;
    public int price;
}
