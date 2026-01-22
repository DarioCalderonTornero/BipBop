using UnityEngine;
using UnityEngine.UI;

public class ButtonClickUI : MonoBehaviour
{
    [SerializeField] private AudioClip buttonClickUIAudioClip;

    private void Start()
    {
        Button[] buttons = FindObjectsOfType<Button>();

        foreach (Button button in buttons)
        {
            Debug.Log("Button Clicked");
            button.onClick.AddListener(ButtonSound);
        }
    }

    private void ButtonSound()
    {
        SoundManager.Instance.PlaySound(buttonClickUIAudioClip, 0.7f);
    }
}
