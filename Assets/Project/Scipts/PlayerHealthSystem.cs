using Unity.Netcode;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerHealthSystem : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Player playerScript; // Kontrolleri kapatmak için
    [SerializeField] private Animator animator;
    private HealthBar healthBar; // UI script'ine referans

    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100;
    private NetworkVariable<int> currentHealth = new NetworkVariable<int>();
    private NetworkVariable<bool> isDead = new NetworkVariable<bool>();

    public bool IsDead => isDead.Value;

    public override void OnNetworkSpawn() {
        if (IsOwner) {
            healthBar = GameObject.FindWithTag("HealthBar").GetComponent<HealthBar>();
            // UI'ın başlangıç değerlerini ayarla.
            healthBar.Initialize(maxHealth);
        }

        // Can değişkenindeki değişiklikleri dinlemeye başla.
        currentHealth.OnValueChanged += OnHealthChanged;


        // Canı sadece sunucu belirler.
        if (IsServer) {
            currentHealth.Value = maxHealth;
        }
    }

    public override void OnNetworkDespawn() {
        // Dinlemeyi bırak.
        currentHealth.OnValueChanged -= OnHealthChanged;
    }

    // currentHealth.Value sunucuda değiştiğinde bu fonksiyon TÜM client'larda tetiklenir.
    private void OnHealthChanged(int previousValue, int newValue) {
        if (IsOwner && healthBar != null) {
            healthBar.UpdateHealth(newValue);
        }
    }

    // Bu fonksiyon sunucu tarafında EnemyBrain tarafından çağrılacak.
    public void TakeDamage(int amount) {
        if (!IsServer) return;
        if (isDead.Value) return;

        currentHealth.Value = Mathf.Max(0, currentHealth.Value - amount);

        if (currentHealth.Value <= 0) {
            Die();
            isDead.Value = true; 
        }
    }

    private void Die() {
        if (!IsServer) return;
        PlayerDiedClientRpc();
    }

    [ClientRpc]
    private void PlayerDiedClientRpc() {
        animator.SetTrigger("die");

        if (IsOwner) {
            playerScript.enabled = false;
        }
    }
}