using UnityEngine;

public class ThrusterSound : MonoBehaviour
{
    [SerializeField] private AudioSource thrusterAudioSource;
    [SerializeField] private TurboController turboController;

    private bool _isPlaying;

    private void Awake()
    {
        if (thrusterAudioSource != null)
        {
            thrusterAudioSource.loop = true;
            thrusterAudioSource.playOnAwake = false;
        }
    }

    private void OnEnable()
    {
        if (turboController != null)
            turboController.OnStateChanged += OnTurboStateChanged;

        Refresh();
    }

    private void OnDisable()
    {
        if (turboController != null)
            turboController.OnStateChanged -= OnTurboStateChanged;
    }

    private void OnTurboStateChanged(TurboController.TurboState state)
    {
        Refresh();
    }

    private void Refresh()
    {
        if (thrusterAudioSource == null || turboController == null) return;

        bool shouldPlay =
            turboController.IsHeld &&
            !turboController.IsExploded; 

        if (shouldPlay)
        {
            if (!_isPlaying)
            {
                thrusterAudioSource.UnPause();
                if (!thrusterAudioSource.isPlaying)
                    thrusterAudioSource.Play();

                _isPlaying = true;
            }
        }
        else
        {
            if (_isPlaying)
            {
                thrusterAudioSource.Pause();
                _isPlaying = false;
            }

            if (turboController.IsExploded)
            {
                thrusterAudioSource.Stop();
            }
        }
    }
}
