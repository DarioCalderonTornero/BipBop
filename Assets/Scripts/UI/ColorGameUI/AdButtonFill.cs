using UnityEngine;
using UnityEngine.UI;

public class AdButtonFill : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [SerializeField] private float duration = 5f;

    private float timer;

    private void OnEnable()
    {
        timer = duration;
        fillImage.fillAmount = 1f;
    }

    private void Update()
    {
        timer -= Time.deltaTime;

        fillImage.fillAmount = timer / duration;

        if (timer <= 0f)
        {
            gameObject.SetActive(false);
        }
    }
}
