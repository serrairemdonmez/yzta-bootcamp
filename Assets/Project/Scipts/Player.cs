using DG.Tweening;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class Player : MonoBehaviour
{
    private Rigidbody rb;
    private Animator animator;
    private Transform mainCam;
    private InputReader inputReader;

    [Header("Movement Settings")]
    [SerializeField] private float movementSpeed = 5f;
    [SerializeField] private float sprintSpeed = 10f;
    [SerializeField] private float turnSpeed = .3f;
    private bool isSprint = false;

    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 1f; // Force applied when jumping
    [SerializeField] private float gravityBase = -9.81f;
    [SerializeField] private float gravityMultiplier = 2f;
    [SerializeField] private float fallMultiplier = 3f;
    [SerializeField] private Transform groundCheck; // Transform to check if the player is grounded
    [SerializeField] private LayerMask jumpableMask; // Layer mask for ground detection

    private bool isJumping = false; // Flag to check if the player is jumping
    private float verticalVelocity = 0f;
    private float groundCheckDelay = 0.1f;
    private float lastJumpTime;

    [Header("Attack Settings")]
    [SerializeField] private float attackCooldown = .5f; // Cooldown time for attacks

    private float attackTime = 0f;

    private int attackType = 1; // 0: no attack, 1: attack type 1, 2: attack type 2, 3: attack type 3
    private bool isDefending = false; // Flag to check if the player is defending

    private void Awake() {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        inputReader = GetComponent<InputReader>();
        mainCam = Camera.main.transform;
    }

    private void Update() {
        // Sprint key :
        if (inputReader.IsSprintPressed()) {
            isSprint = true;
        }
        else {
            isSprint = false;
        }

        // Attack key : 
        if (inputReader.IsAttackPressed()) {
            Attack();
        }
        else {
            attackTime = 0f; 
            attackType = 1;
        }

        // Jump key :
        if (inputReader.IsJumpTriggered() && IsGrounded()) {
            Jumping();
        }

        // Defend key :
        if (inputReader.IsDefendPressed()) {
            animator.SetBool("defend", true);
            // Reset attack type when defense is triggered
            if (!isDefending) {
                attackType = 1;
                animator.SetInteger("attackType", 0);
                attackTime = 0f; // Reset attack cooldown when defense is triggered
            }

            isDefending = true;
        }
        else {
            animator.SetBool("defend", false);
            isDefending = false;
            Debug.Log("Defend released");
        }

        ApplyCustomGravity();
    }

    private void FixedUpdate() {
        Movement();
    }

    private void Movement() {
        if (!isDefending) {
            float moveHorizontal = inputReader.GetMovementInput().x;  // -1 for left, 1 for right
            float moveVertical = inputReader.GetMovementInput().y;  //-1 for down, 1 for up

            Vector3 inputDir = new Vector3(moveHorizontal, 0f, moveVertical).normalized;
            Vector3 movement = Quaternion.AngleAxis(mainCam.eulerAngles.y, Vector3.up) * inputDir * (isSprint ? sprintSpeed : movementSpeed);
            rb.linearVelocity = new Vector3(movement.x, rb.linearVelocity.y, movement.z);

            // Rotate the player towards the movement direction
            TurnProcess(movement);

            // Clamp speedValue to a max of 0.5f
            float inputMagnitude = new Vector2(moveHorizontal, moveVertical).magnitude;
            float speedValue = inputMagnitude * (isSprint ? 1f : 0.5f);
            animator.SetFloat("speed", speedValue);
        }
    }

    private void TurnProcess(Vector3 direction) {
        if (direction.magnitude < 0.1f) return; // Avoid turning when not moving
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.DORotateQuaternion(targetRotation, turnSpeed);
    }

    private void Jumping() {
        if (!isDefending) {
            animator.SetTrigger("jump");
            isJumping = true;
            verticalVelocity = jumpForce;
            lastJumpTime = Time.time;
        }
    }

    private bool IsGrounded() {
        if (Time.time - lastJumpTime < groundCheckDelay) {
            return false;
        }

        return Physics.CheckSphere(groundCheck.position, 0.1f, jumpableMask);
    }

    private void ApplyCustomGravity() {
        if (!IsGrounded()) {
            if (verticalVelocity > 0) {
                verticalVelocity += gravityBase * gravityMultiplier * Time.deltaTime;
            }
            else {
                verticalVelocity += gravityBase * fallMultiplier * Time.deltaTime;
            }

            rb.linearVelocity = new Vector3(rb.linearVelocity.x, verticalVelocity, rb.linearVelocity.z);
        }
        else {
            if (isJumping) {
                isJumping = false;
                verticalVelocity = 0f;
            }
        }
    }

    private void Attack() {
        if (!isDefending) {
            if (attackTime <= 0f) {
                animator.SetInteger("attackType", attackType); // Randomly choose an attack type between 1 and 3
                Debug.Log(attackType);
                attackType = (attackType < 3) ? attackType + 1 : 1; // Cycle through attack types 1, 2, 3
                StartCoroutine(ResetAttackAnimation()); // Reset attack type after 1 frame 
                attackTime = attackCooldown; // Reset cooldown
            }

            else {
                attackTime -= Time.deltaTime; // Decrease cooldown time
            }
        }
    }

    private IEnumerator ResetAttackAnimation() {
        yield return null; // 1 frame bekle
        animator.SetInteger("attackType", 0);
    }

    private void OnDrawGizmosSelected() {
        if (groundCheck != null) {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, .1f);
        }

    }
}
