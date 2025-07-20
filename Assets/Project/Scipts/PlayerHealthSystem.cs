using UnityEngine;

[DisallowMultipleComponent]
public class PlayerHealthSystem : MonoBehaviour
{
    private Animator animator; // Reference to the player's animator component

    [Header("Health Settings")]
    [SerializeField] private HealthBar healthBar; // Reference to the health bar UI component

    private void Awake() {
        animator = GetComponent<Animator>();
    }

    private void Update() {
        // Simulate health change for testing
        if (Input.GetKeyDown(KeyCode.J)) {
            Damage(50);
        }

    }

    private void Damage(float damage) {
        healthBar.DecreaseHealth(damage);
        if (healthBar.CurrentHealth <= 0) {
            Die();
        }
    }

    private void Die() {
        animator.SetTrigger("die");
        // Disable player controls or perform other death-related actions
        // For example, you might want to disable the player's movement script
         GetComponent<Player>().enabled = false; 
    }
}