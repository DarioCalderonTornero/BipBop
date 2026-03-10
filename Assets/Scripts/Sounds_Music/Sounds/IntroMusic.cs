using UnityEngine;
using UnityEngine.Localization.SmartFormat.Utilities;

public class IntroMusic : MonoBehaviour
{

    [SerializeField] private AudioSource introMusicAudioSource;
    [SerializeField] private AudioClip introAudioClip;
    [SerializeField] private AudioClip secondIntroAudioClip;
    [SerializeField] private AudioClip musicIntroAudioClip;

    private void Awake()
    {
        introMusicAudioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        Invoke("PlayIntroSound", .5f);
        

        Invoke("PlaySecondIntroSound", 1.5f);

        Invoke("PlayIntroMusic", introAudioClip.length);
    }

    void PlayIntroSound()
    {
        float volume = 1.0f;
        introMusicAudioSource.PlayOneShot(introAudioClip, volume);
    }

    void PlaySecondIntroSound()
    {
        float volume = 1.0f;
        introMusicAudioSource.PlayOneShot(secondIntroAudioClip, volume);
    }

    private void PlayIntroMusic()
    {
        float volume = 1.0f;
        introMusicAudioSource.PlayOneShot(musicIntroAudioClip, volume);  
    }

    
}
