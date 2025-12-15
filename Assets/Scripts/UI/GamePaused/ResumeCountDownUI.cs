using TMPro;
using UnityEngine;
using System;
using System.Collections;

public class ResumeCountDownUI : MonoBehaviour
{
    public event Action Finished;

    [SerializeField] private Animator animator;      
    [SerializeField] private TextMeshProUGUI label;

    [Header("Timing")]
    [SerializeField] private int startFrom = 3;
    [SerializeField] private float stepSeconds = 1f;

    private Coroutine routine;

    private void Awake()
    {
        if (animator != null)
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;

        label.gameObject.SetActive(false);
    }

    public void Play()
    {
        if (routine != null) StopCoroutine(routine);
        label.gameObject.SetActive(true);
        routine = StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        for (int i = startFrom; i >= 1; i--)
        {
            Show(i.ToString());
            yield return new WaitForEndOfFrame();              
            yield return new WaitForSecondsRealtime(stepSeconds);
        }

        Show("GO!");
        yield return new WaitForEndOfFrame();
        yield return new WaitForSecondsRealtime(stepSeconds);

        label.gameObject.SetActive(false);
        routine = null;
        Finished?.Invoke();
    }


    private void Show(string text)
    {
        if (label != null) label.text = text;

        if (animator != null)
        {
            animator.ResetTrigger("Play");
            animator.SetTrigger("Play");
        }
    }
}