using System.Collections.Generic;
using UnityEngine;

// Definimos las direcciones
public enum SwipeDirection
{
    Up, Down, Left, Right
}

// Estructura para el Inspector
[System.Serializable]
public class CategoryData
{
    public string categoryName;
    public SwipeDirection correctDirection;
    public Sprite categoryIcon;
    public List<Sprite> validSprites;
}