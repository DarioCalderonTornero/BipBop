using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }
    public event EventHandler OnSoundVolumeChanged;

    private const int SOUND_VOLUME_MAX = 10;
    private static int soundVolume = 6;

    [SerializeField] private AudioClip onColorGameModePoint;


    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        //ColorGamePuntos.Instance.OnColorAddScore += ColorGamePuntos_OnColorAddScore;
        AudioSource.PlayClipAtPoint(onColorGameModePoint, Camera.main.transform.position, GetSoundVolumeNormalized());
    }

    private void ColorGamePuntos_OnColorAddScore(object sender, EventArgs e)
    {

    }

    public void ChangeSoundVolume()
    {
        soundVolume = (soundVolume + 1) % SOUND_VOLUME_MAX;
        OnSoundVolumeChanged?.Invoke(this, EventArgs.Empty);
    }

    public int GetSoundVolume()
    {
        return soundVolume;
    }

    public float GetSoundVolumeNormalized()
    {
        return ((float)soundVolume) / SOUND_VOLUME_MAX;
    }
}
