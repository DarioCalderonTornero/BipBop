using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonAnimation : MonoBehaviour
{
    private Button animButton;
    private Vector3 normalScale;
    private Vector3 upScale = new Vector3(1.2f, 1.2f, 1.0f);

    private void Awake()
    {
        normalScale = transform.localScale;
        animButton = GetComponent<Button>();
        animButton.onClick.AddListener(Anim);
    }

    private void Anim()
    {
        LeanTween.scale(gameObject, upScale, 0.1f).setEase(LeanTweenType.easeOutBack);
        LeanTween.scale(gameObject, normalScale, 0.1f).setDelay(0.2f).setEase(LeanTweenType.easeInOutQuad);
    }
}


