using UnityEngine;

public class Settings : MonoBehaviour
{
    [SerializeField] private LanguageToggleButton languageToggleButton;

    void Start()
    {
        
    }

    public void ResetSettings()
    {
        if (languageToggleButton != null)
        {
            // languageToggleButton.ForceSpanish();
        }

        //Reset Haptics
        Haptics.SetEnabled(true);

        if (SoundManager.Instance.GetSoundVolumeNormalized() == 0)
        {
            //Reset sound
            SoundManager.Instance.RestoreVolumeTo(5);
        }

        if (MusicManager.Instance.GetMusicVolumeNormalized() == 0)
        {
            //Reset music
            MusicManager.Instance.RestoreVolumeTo(5);
        }     
    }
}
