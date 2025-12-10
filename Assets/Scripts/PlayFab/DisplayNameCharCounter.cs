using TMPro;
using UnityEngine;

public class DisplayNameCharCounter : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private TextMeshProUGUI counterText;

    [Header("Límite")]
    [SerializeField] private int maxChars = 12;

    [Header("DEBUG")]
    [SerializeField] private bool debugFillTestName = false;
    [SerializeField] private string debugName = "DebugName";

    private void Awake()
    {
        if (nameInput == null)
            nameInput = GetComponent<TMP_InputField>();

        if (nameInput != null)
        {
            nameInput.characterLimit = maxChars;
            nameInput.onValueChanged.AddListener(OnNameChanged);
        }

        UpdateCounter();
    }

    private void OnDestroy()
    {
        if (nameInput != null)
            nameInput.onValueChanged.RemoveListener(OnNameChanged);
    }

    private void OnNameChanged(string newValue)
    {
        if (newValue.Length > maxChars)
        {
            newValue = newValue.Substring(0, maxChars);
            nameInput.text = newValue;
        }

        UpdateCounter();
    }

    private void UpdateCounter()
    {
        if (nameInput == null || counterText == null) return;

        int len = nameInput.text.Length;
        counterText.text = $"{len} / {maxChars}";
    }

    private void Update()
    {
        if (debugFillTestName)
        {
            debugFillTestName = false;
            if (nameInput != null)
            {
                nameInput.text = debugName;
                UpdateCounter();
            }
        }
    }
}
