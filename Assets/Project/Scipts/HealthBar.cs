using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    private Slider healthSlider;

    [Header("Health Bar Settings")]
    [SerializeField] private float maxHealth = 100f; // Maximum health value
    [SerializeField] private float tweenDuration = 0.5f;

    // Property to get or set the current health value
    public float CurrentHealth { get; set; }

    //Getters : 
    public Slider HealthSlider { get { return healthSlider; } }


    private void Awake() {
        healthSlider = GetComponent<Slider>();
        healthSlider.maxValue = maxHealth;
        healthSlider.value = maxHealth; 
    }


    public void IncreaseHealth(float increase) {
        ChangeHealth(increase);
    }

    public void DecreaseHealth(float decrease) {
        ChangeHealth(-decrease);
    }

    private void ChangeHealth(float amount) {
        if (healthSlider.value > 0f) {
            float newHealth = healthSlider.value + amount;
            CurrentHealth = newHealth;
            healthSlider.DOValue(newHealth, tweenDuration).SetEase(Ease.OutQuad); 
        }
    }
}