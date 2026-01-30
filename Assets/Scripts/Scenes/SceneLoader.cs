using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneLoader
{

    public enum Scene
    {
        Menu,
        GameScene,
        ColorScene,
        GeometricScene,
        DodgeScene,
        GridScene,
        DifferentScene
    }
    public static void LoadScene(Scene scene)
    {
        SceneManager.LoadScene(scene.ToString());
    }
}
