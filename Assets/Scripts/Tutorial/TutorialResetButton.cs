using UnityEngine;

public class TutorialResetButton : MonoBehaviour
{
    public void ResetTutorial()
    {
        PlayerPrefs.DeleteKey("TutorialCompleted");
        PlayerPrefs.Save();

        Debug.Log("Tutorial resetado. Volverá a salir al darle a Play.");
    }
}
