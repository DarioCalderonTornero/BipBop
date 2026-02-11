using UnityEngine;

public class DontDestroyOnLoadGO : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
}
