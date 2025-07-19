using DG.Tweening;
using System;
using System.Collections;
using UnityEngine;

public class Player : MonoBehaviour
{
    private Rigidbody rb;
    private Animator animator;

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
    [SerializeField] private float attackIdleResetThreshold = 0.5f; // Cooldown time for attack types

    private float attackTime = 0f;
    private float attackIdleTime = 0f; 

    private int attackType = 1; // 0: no attack, 1: attack type 1, 2: attack type 2, 3: attack type 3
    private bool isAttacking = false; // Flag to check if the player is attacking
    private bool isDefending = false; // Flag to check if the player is defending

    private void Awake() {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
    }

    private void Update() {
        //Sprint key :
        if (Input.GetKeyDown(KeyCode.LeftShift)) {
            isSprint = true;
        }

        else if (Input.GetKeyUp(KeyCode.LeftShift)) {
            isSprint = false;
        }

        //Attack key : 
        if (Input.GetMouseButton(0)) {
            Attack();
            isAttacking = true;
        }

        else if (Input.GetMouseButtonUp(0)) {
            attackTime = 0f; // Reset attack cooldown when mouse button is released
            isAttacking = false;
        }

        //Jump key :
        if (Input.GetKeyDown(KeyCode.Space) && IsGrounded()) {
            Jumping();
        }

        //Defend key :
        if (Input.GetMouseButton(1)) {
            animator.SetBool("defend", true);
            isDefending = true;
        }

        else if (Input.GetMouseButtonUp(1)) {
            animator.SetBool("defend", false);
            isDefending = false;
        }

        ApplyCustomGravity();

        // ⬇️ AttackType reset logic:
        if (!isAttacking) {
            attackIdleTime += Time.deltaTime;
            if (attackIdleTime >= attackIdleResetThreshold) {
                attackType = 1;
                attackIdleTime = 0f;
            }
        }
        else {
            attackIdleTime = 0f;
        }
    }


    private void FixedUpdate() {
        Movement();
    }

    private void Movement() {
        if (!isDefending) {
            float moveHorizontal = Input.GetAxisRaw("Horizontal");  // -1 for left, 1 for right
            float moveVertical = Input.GetAxisRaw("Vertical");  //-1 for down, 1 for up

            Vector3 inputDir = new Vector3(moveHorizontal, 0f, moveVertical).normalized;
            Vector3 movement = inputDir * (isSprint ? sprintSpeed : movementSpeed);
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
                attackType = (attackType < 3) ? attackType + 1 : 1; // Cycle through attack types 1, 2, 3
                StartCoroutine(ResetAttackType()); // Reset attack type after 1 frame 
                attackTime = attackCooldown; // Reset cooldown
            }

            else {
                attackTime -= Time.deltaTime; // Decrease cooldown time
            }
        }
    }

    private IEnumerator ResetAttackType() {
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
