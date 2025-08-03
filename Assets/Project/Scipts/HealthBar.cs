using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class HealthBar : MonoBehaviour
{
    private Slider healthSlider;

    [Header("Health Bar Settings")]
    [SerializeField] private float tweenDuration = 0.5f;

    private void Awake() {
        healthSlider = GetComponent<Slider>();
    }

    // YENİ: Başlangıç değerlerini ayarlamak için bir fonksiyon
    public void Initialize(float maxHealth) {
        healthSlider.maxValue = maxHealth;
        healthSlider.value = maxHealth;
    }

    // YENİ: Canı güncellemek için tek bir fonksiyonumuz olacak.
    public void UpdateHealth(float newHealth) {
        // DOTween ile slider'ın değerini yumuşak bir şekilde değiştir.
        healthSlider.DOValue(newHealth, tweenDuration).SetEase(Ease.OutQuad);
    }
}