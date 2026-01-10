using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }
    private InputActions inputActions;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        inputActions = new InputActions();
        inputActions.Enable();
    }

    public Vector2 GetDodgePlayerMovement()
    {
        return inputActions.Player.DodgeMovement.ReadValue<Vector2>();
    }

    private void OnDestroy()
    {
        //inputActions.Disable();
    }
}
