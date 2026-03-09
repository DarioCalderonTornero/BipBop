using UnityEngine;
using UnityEngine.Localization.SmartFormat.Utilities;

public class IntroMusic : MonoBehaviour
{

    [SerializeField] private AudioSource introMusicAudioSource;
    [SerializeField] private AudioClip introAudioClip;
    [SerializeField] private AudioClip musicIntroAudioClip;

    private void Awake()
    {
        introMusicAudioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        Invoke("PlayIntroSound", .5f);
        

        Invoke("PlayIntroMusic", introAudioClip.length);
    }

    private void PlayIntroMusic()
    {
        float volume = 1.0f;
        introMusicAudioSource.PlayOneShot(musicIntroAudioClip, volume);  
    }

    void PlayIntroSound()
    {
        float volume = 1.0f;
        introMusicAudioSource.PlayOneShot(introAudioClip, volume);
    }
}
