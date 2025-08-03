using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class InputReader : MonoBehaviour, PlayerInputActions.IPlayerActions
{
    private PlayerInputActions inputActions;

    //Discrete Event
    public event UnityAction<bool> Sprint = delegate { };
    public event UnityAction<bool> Jump = delegate { };
    public event UnityAction<bool> Attack = delegate { };
    public event UnityAction<bool> Defend = delegate { };
    public event UnityAction<bool> Interact = delegate { };

    //Contionus Event
    public Vector2 Move { get { return inputActions.Player.Move.ReadValue<Vector2>(); } }

    private void Awake() {
        if(inputActions == null) {
            inputActions = new PlayerInputActions();
            inputActions.Player.SetCallbacks(this);
        }
    }

    private void OnEnable() {
        inputActions.Player.Enable();
    }

    private void OnDisable() {
        inputActions.Player.Disable();
    }

    public void OnMove(InputAction.CallbackContext context) {
        // noop
    }

    public void OnSprint(InputAction.CallbackContext context) {
        if(context.started) {
            Sprint.Invoke(true);
        }

        else if(context.canceled) {
            Sprint.Invoke(false);
        }
    }

    public void OnLook(InputAction.CallbackContext context) {
        // noop
    }

    public void OnJump(InputAction.CallbackContext context) {
        if (context.started) {
            Jump.Invoke(true);
        }

        else if (context.canceled) {
            Jump.Invoke(false);
        }
    }

    public void OnAttack(InputAction.CallbackContext context) {
        if (context.started) {
            Attack.Invoke(true);
        }
        else if (context.canceled) {
            Attack.Invoke(false);
        }
    }

    public void OnDefend(InputAction.CallbackContext context) {
        if (context.started) {
            Defend.Invoke(true);
        }

        else if (context.canceled) {
            Defend.Invoke(false);
        }
    }

    public void OnInteract(InputAction.CallbackContext context) {
        if (context.started) {
            Interact.Invoke(true);
        }

        else if (context.canceled) {
            Interact.Invoke(false);
        }
    }

    public void OnCrouch(InputAction.CallbackContext context) {
        // noop
    }


    public void OnNext(InputAction.CallbackContext context) {
        // noop
    }

    public void OnPrevious(InputAction.CallbackContext context) {
        // noop
    }


}

