using DG.Tweening;
using Unity.Netcode;
using UnityEngine;
using System.Collections;
using Unity.Cinemachine;

public enum PlayerState
{
    Locomotion,
    Attacking,
    Jumping,
    Defending
}

[DisallowMultipleComponent]
public class Player : NetworkBehaviour
{
    // References
    private Rigidbody rb;
    private Animator animator;
    private InputReader inputReader;

    // State Management
    private NetworkVariable<PlayerState> currentState = new(PlayerState.Locomotion, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("Movement Settings")]
    [SerializeField] private float movementSpeed = 5f;
    [SerializeField] private float sprintSpeed = 10f;
    [SerializeField] private float turnSpeed = .3f;
    private NetworkVariable<bool> isSprinting = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private Vector2 cachedInput; // DE���T�R�LD�: Sadece x ve y yeterli

    [Header("Camera Settings")]
    [SerializeField] private CinemachineCamera virtualCam;
    private Quaternion cachedCameraRotation;

    [Header("Jump & Gravity Settings")]
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float gravityMultiplier = 2.5f;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask jumpableMask;
    private bool isGrounded; // YEN�: Bu sunucuda kontrol edilecek lokal bir de�i�ken olarak kalabilir.

    [Header("Attack Settings")]
    [SerializeField] private float attackCooldown = .5f;
    private float attackTime = 0f; // Sadece sunucuda kullan�lacak
    private int attackType = 0; // Sadece sunucuda kullan�lacak
    private bool isAttackHeldDown_server; // Sadece sunucuda kullan�lacak

    [Header("Damage Settings")] // YENİ BÖLÜM
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private int attackDamage = 25;
    [SerializeField] private LayerMask enemyLayer;

    // YEN�: Animasyon i�in NetworkVariable'lar
    private NetworkVariable<float> networkAnimSpeed = new(0f);
    private NetworkVariable<bool> networkAnimIsGrounded = new(true);
    private NetworkVariable<float> networkAnimVelocityY = new(0f);


    public override void OnNetworkSpawn() {
        // Referanslar� ve olaylar� burada ata
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        inputReader = GetComponent<InputReader>();

        if (IsOwner) {
            // Sadece kendi karakterimiz i�in kameray� ve inputlar� ayarla
            inputReader.Sprint += HandleSprintInput;
            inputReader.Jump += HandleJumpInput;
            inputReader.Attack += HandleAttackInput;
            inputReader.Defend += HandleDefendInput;

        }

        else {
            virtualCam.enabled = false; // Di�er oyuncular i�in kameray� devre d��� b�rak
        }

        // YEN�: Animasyonlar� g�ncellemek i�in OnValueChanged callback'lerini ekle
        networkAnimSpeed.OnValueChanged += (prev, current) => animator.SetFloat("speed", current);
        networkAnimIsGrounded.OnValueChanged += (prev, current) => animator.SetBool("isGrounded", current);
        networkAnimVelocityY.OnValueChanged += (prev, current) => animator.SetFloat("velocityY", current);
    }

    public override void OnNetworkDespawn() {
        // Event aboneliklerini kald�r
        if (IsOwner) {
            inputReader.Sprint -= HandleSprintInput;
            inputReader.Jump -= HandleJumpInput;
            inputReader.Attack -= HandleAttackInput;
            inputReader.Defend -= HandleDefendInput;
        }

    }

    private void Update() {
        if (!IsOwner) return; // Sadece sahibi olan client input g�nderebilir

        // Hareketi her frame sunucuya bildir
        SubmitMovementRequestServerRpc(inputReader.Move, virtualCam.transform.rotation);
    }

    private void FixedUpdate() {
        // T�m fiziksel hesaplamalar sadece sunucuda yap�l�r
        if (!IsServer) return;

        // YEN�: Cooldown'u her frame azalt
        if (attackTime > 0) {
            attackTime -= Time.fixedDeltaTime;
        }

        // YEN�: Sald�r� durumunu s�rekli kontrol et
        HandleAttacking();

        CheckGrounded();
        HandleGravity();
        HandleMovement();

        // YEN�: Sunucu, animasyon state'lerini g�nceller
        networkAnimSpeed.Value = new Vector2(rb.linearVelocity.x, rb.linearVelocity.z).magnitude / (isSprinting.Value ? sprintSpeed : movementSpeed) * (isSprinting.Value ? 1f : 0.5f);
        networkAnimIsGrounded.Value = isGrounded;
        networkAnimVelocityY.Value = rb.linearVelocity.y;
    }

    #region Input Handlers (Client-Side)

    // DE���T�R�LD�: Input handler'lar art�k sadece ServerRpc �a��r�yor.
    private void HandleSprintInput(bool isPressed) => SubmitSprintStateServerRpc(isPressed);
    private void HandleJumpInput(bool isPressed) { if (isPressed) SubmitJumpRequestServerRpc(); }
    private void HandleAttackInput(bool isPressed) => SubmitAttackRequestServerRpc(isPressed);
    private void HandleDefendInput(bool isPressed) => SubmitDefendStateServerRpc(isPressed);

    #endregion

    #region Server RPCs (Client -> Server)

    [ServerRpc]
    private void SubmitMovementRequestServerRpc(Vector2 input, Quaternion cameraRotation) {
        cachedInput = input;
        cachedCameraRotation = cameraRotation;
    }

    [ServerRpc]
    private void SubmitSprintStateServerRpc(bool isPressed) {
        isSprinting.Value = isPressed;
    }

    [ServerRpc]
    private void SubmitJumpRequestServerRpc() {
        if (isGrounded && currentState.Value != PlayerState.Defending) {
            currentState.Value = PlayerState.Jumping;
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            JumpAnimationClientRpc(); // YEN�: Animasyon anl�k oldu�u i�in ClientRpc ile tetiklenebilir.
        }
    }

    [ServerRpc]
    private void SubmitAttackRequestServerRpc(bool isPressed) {
        if (currentState.Value == PlayerState.Defending) return;

        // Client'�n iste�ini sunucudaki de�i�kene ata
        isAttackHeldDown_server = isPressed;

        if (isPressed) {
            // E�er sald�r� yeni ba�l�yorsa, combo'yu ve state'i ayarla
            if (currentState.Value != PlayerState.Attacking) {
                currentState.Value = PlayerState.Attacking;
                attackType = 1;
                attackTime = 0; // Zamanlay�c�y� s�f�rla ki ilk sald�r� hemen ger�ekle�sin
            }
        }
        else {
            // Tu� b�rak�ld�ysa Locomotion durumuna geri d�n
            if (currentState.Value == PlayerState.Attacking) {
                currentState.Value = PlayerState.Locomotion;
            }
        }
    }

    [ServerRpc]
    private void SubmitDefendStateServerRpc(bool isPressed) {
        animator.SetBool("defend", isPressed); // Direkt animat�r� kontrol edebiliriz
        if (isPressed) {
            currentState.Value = PlayerState.Defending;
        }
        else {
            // Coroutine yerine direkt Locomotion'a ge�ebiliriz
            currentState.Value = PlayerState.Locomotion;
        }
    }

    #endregion

    #region Client RPCs (Server -> Clients)

    [ClientRpc]
    private void JumpAnimationClientRpc() {
        animator.SetTrigger("jump");
    }

    [ClientRpc]
    private void AttackAnimationClientRpc(int type) {
        animator.SetInteger("attackType", type);
        // Animat�rdeki "attackType" integer'�n� bir sonraki frame'de s�f�rlamak i�in
        // Animator state machine'de bir "exit transition" kullanmak daha temizdir.
        // Ama coroutine ile yapmak istersen:
        StartCoroutine(ResetAttackTypeAnim());
    }

    #endregion

    #region Server-Side Logic

    private void HandleMovement() {
        if (currentState.Value == PlayerState.Defending) {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0); // Savunurken hareketi kes
            return;
        }

        Vector3 inputDirection = new Vector3(cachedInput.x, 0, cachedInput.y);

        // Kamera y�n�n� hesaba kat. Bu RPC ile sunucuya g�nderilmeli veya sunucunun bilmesi gerekir.
        // �imdilik, kameran�n rotasyonunu NetworkTransform ile senkronize etti�ini varsayal�m.
        // Daha sa�lam bir ��z�m i�in Client'tan kamera y�n�n� de g�ndermek gerekebilir.
        Vector3 movement = Quaternion.AngleAxis(cachedCameraRotation.eulerAngles.y, Vector3.up) * inputDirection * (isSprinting.Value ? sprintSpeed : movementSpeed);
        rb.linearVelocity = new Vector3(movement.x, rb.linearVelocity.y, movement.z);

        if (inputDirection.magnitude > 0.1f) {
            TurnProcess(movement);
        }

        if (currentState.Value != PlayerState.Locomotion && inputDirection.magnitude > 0f) {
            // Di�er state'lerden (�rn: z�plama bitti�inde) harekete ge�i�
            if (isGrounded) currentState.Value = PlayerState.Locomotion;
        }
    }

    private void TurnProcess(Vector3 direction) {
        // Sunucu rotasyonu hesaplar, NetworkTransform bunu client'lara yans�t�r.
        if (direction.sqrMagnitude < 0.01f) return;
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        // DOTween sunucu taraf�nda �al��maz. NetworkTransform'un rotasyon senkronizasyonuna g�venelim.
        // transform.DORotateQuaternion(targetRotation, turnSpeed); // BU SATIRI KALDIR
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed); // DAHA UYUMLU B�R ALTERNAT�F
    }

    private void CheckGrounded() {
        isGrounded = Physics.CheckSphere(groundCheck.position, 0.2f, jumpableMask);
        if (isGrounded && currentState.Value == PlayerState.Jumping) {
            currentState.Value = PlayerState.Locomotion;
        }
    }

    private void HandleGravity() {
        if (!isGrounded) {
            rb.AddForce(Physics.gravity * gravityMultiplier, ForceMode.Acceleration);
        }
    }

    private void HandleAttacking() {
        // E�er client sald�r� tu�una bas�l� tutuyorsa VE karakter Attacking durumundaysa
        if (isAttackHeldDown_server && currentState.Value == PlayerState.Attacking) {
            // Sald�r�y� ger�ekle�tir (bu fonksiyon cooldown'u kontrol edecektir)
            PerformAttack();
        }
    }

    private void PerformAttack() {
        // Cooldown kontrolü, bu fonksiyon zaten sadece sunucuda çalışıyor.
        if (attackTime <= 0) {
            // 1. Animasyonu tüm client'larda oynat.
            AttackAnimationClientRpc(attackType);

            // 2. Combo ve cooldown'u ayarla.
            attackType = (attackType < 3) ? attackType + 1 : 1;
            attackTime = attackCooldown;

            // --- YENİ HASAR VERME MANTIĞI ---

            // 3. Oyuncunun önündeki alanı küre şeklinde tara.
            Collider[] hitEnemies = Physics.OverlapSphere(transform.position, attackRange, enemyLayer);

            // 4. Vurulan her düşman için...
            foreach (Collider enemyCollider in hitEnemies) {
                // Vurulan objenin EnemyHealth script'i var mı diye kontrol et.
                if (enemyCollider.TryGetComponent<EnemyHealth>(out EnemyHealth enemyHealth)) {
                    // Varsa, hasar ver.
                    enemyHealth.TakeDamage(attackDamage);
                    Debug.Log($"Düşmana vuruldu: {enemyCollider.name}, Verilen Hasar: {attackDamage}");
                }
            }
        }
    }

    // YEN�: S�rekli �al��an attack check yerine, RPC i�inde bir zaman kontrol� ekledim.
    // Update i�inde s�rekli �al��mas�na gerek kalmad�.
    // Her frame attackTime'� azaltmak i�in FixedUpdate kullan�labilir.
    // void FixedUpdate() { ... attackTime -= Time.fixedDeltaTime; ... }

    private IEnumerator ResetAttackTypeAnim() {
        // Bu coroutine client-side �al��acak.
        yield return null; // Bir sonraki frame'e kadar bekle
        animator.SetInteger("attackType", 0);
    }

    #endregion
}
