using UnityEngine;
using UnityEngine.InputSystem;
using GMTK.PlatformerToolkit;

//Centralizes all player input so movement/jump scripts can simply query state.
public class characterInputManager : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] movementLimiter moveLimit;

    [Header("Input Settings")]
    [SerializeField, Tooltip("Ignore analogue drift smaller than this magnitude")] private float inputDeadzone = 0.15f;

    [Header("Current Input State")]
    public Vector2 moveInput;
    public bool jumpPressed;
    public bool isPressingJump;
    public bool umbrellaActionPressed;
    public bool isUmbrellaActionHeld;
    public bool umbrellaDashPressed;
    public bool interactPressed;
    public bool isInteractHeld;

    public void OnMove(InputAction.CallbackContext context)
    {
        if (moveLimit != null && !moveLimit.characterCanMove)
        {
            moveInput = Vector2.zero;
            return;
        }

        Vector2 rawInput = context.ReadValue<Vector2>();
        moveInput = rawInput.sqrMagnitude < inputDeadzone * inputDeadzone ? Vector2.zero : rawInput;
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (moveLimit != null && !moveLimit.characterCanMove)
        {
            isPressingJump = false;
            return;
        }

        if (context.started)
        {
            jumpPressed = true;
            isPressingJump = true;
        }

        if (context.canceled)
        {
            isPressingJump = false;
        }
    }

    public bool ConsumeJumpInput()
    {
        if (jumpPressed)
        {
            jumpPressed = false;
            return true;
        }

        return false;
    }

    public void ResetInputs()
    {
        moveInput = Vector2.zero;
        jumpPressed = false;
        isPressingJump = false;
        umbrellaActionPressed = false;
        isUmbrellaActionHeld = false;
        umbrellaDashPressed = false;
        interactPressed = false;
        isInteractHeld = false;
    }

    public void OnUmbrellaAction(InputAction.CallbackContext context)
    {
        if (moveLimit != null && !moveLimit.characterCanMove)
        {
            umbrellaActionPressed = false;
            isUmbrellaActionHeld = false;
            return;
        }

        if (context.started)
        {
            umbrellaActionPressed = true;
            isUmbrellaActionHeld = true;
        }

        if (context.canceled)
        {
            isUmbrellaActionHeld = false;
        }
    }

    public bool ConsumeUmbrellaAction()
    {
        if (umbrellaActionPressed)
        {
            umbrellaActionPressed = false;
            return true;
        }

        return false;
    }

    public void OnUmbrellaDash(InputAction.CallbackContext context)
    {
        if (moveLimit != null && !moveLimit.characterCanMove)
        {
            umbrellaDashPressed = false;
            return;
        }

        if (context.started)
        {
            umbrellaDashPressed = true;
        }
    }

    public bool ConsumeUmbrellaDash()
    {
        if (umbrellaDashPressed)
        {
            umbrellaDashPressed = false;
            return true;
        }

        return false;
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (moveLimit != null && !moveLimit.characterCanMove)
        {
            interactPressed = false;
            isInteractHeld = false;
            return;
        }

        if (context.started)
        {
            interactPressed = true;
            isInteractHeld = true;
        }

        if (context.canceled)
        {
            isInteractHeld = false;
        }
    }

    public bool ConsumeInteract()
    {
        if (interactPressed)
        {
            interactPressed = false;
            return true;
        }

        return false;
    }
}
