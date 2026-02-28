using System.Collections.Generic;
using UnityEngine;

// Definimos las direcciones
public enum SwipeDirection
{
    Up, Down, Left, Right
}

[System.Serializable]
public class CategoryData
{
    public SwipeDirection correctDirection;
    public Sprite categoryIcon;
    public List<Sprite> validSprites;
}