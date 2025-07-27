using UnityEngine;

[DisallowMultipleComponent]
public class InputReader : MonoBehaviour
{
    PlayerInputActions playerInputActions;

    private void Awake() {
        playerInputActions = new PlayerInputActions();
    }

    private void OnEnable() {
        playerInputActions.Enable();
    }

    private void OnDisable() {
        playerInputActions.Disable();
    }

    public Vector2 GetMovementInput() {
        return playerInputActions.Player.Move.ReadValue<Vector2>();
    }

    public Vector2 GetLookInput() {
        return playerInputActions.Player.Look.ReadValue<Vector2>();
    }

    public bool IsAttackPressed() {
        return playerInputActions.Player.Attack.IsPressed();
    }

    public bool IsDefendPressed() {
        return playerInputActions.Player.Defend.IsPressed();
    }   

    public bool IsSprintPressed() {
        return playerInputActions.Player.Sprint.IsPressed();
    }

    public bool IsJumpTriggered() {
        return playerInputActions.Player.Jump.triggered;
    }

}

