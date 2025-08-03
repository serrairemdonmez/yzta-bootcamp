using DG.Tweening;
using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class EnemyHealth : NetworkBehaviour
{
    [SerializeField] private int maxHealth = 100;
    // DÜZELTME: NetworkVariable'ı bu şekilde tanımlamak daha iyi bir alışkanlıktır.
    private NetworkVariable<int> currentHealth = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [SerializeField] private Slider healthBar;

    public static event Action OnEnemyDied;

    public override void OnNetworkSpawn() {
        // YENİ: Değişiklikleri dinlemeye başla. Bu satır tüm client'larda çalışır.
        currentHealth.OnValueChanged += OnHealthChanged;

        // Başlangıç değerlerini ve UI'ı ayarla.
        healthBar.maxValue = maxHealth;
        healthBar.value = currentHealth.Value;

        if (IsServer) {
            currentHealth.Value = maxHealth;
        }

        // Eğer canı tam ise can barını gizle
        if (currentHealth.Value == maxHealth) {
            healthBar.gameObject.SetActive(false);
        }
    }

    public override void OnNetworkDespawn() {
        // YENİ: Dinlemeyi bırak (Memory leak önlemek için önemli).
        currentHealth.OnValueChanged -= OnHealthChanged;
    }

    // YENİ FONKSİYON: currentHealth değiştiğinde tüm client'larda bu fonksiyon tetiklenir.
    private void OnHealthChanged(int previousValue, int newValue) {
        // Can barını görünür yap.
        if (!healthBar.gameObject.activeSelf) {
            healthBar.gameObject.SetActive(true);
        }

        // Sadece bu client'ın kendi UI'ını güncelle.
        healthBar.value = newValue;
    }

    public void TakeDamage(int amount) {
        if (!IsServer) return;

        currentHealth.Value -= amount;

        // DÜZELTME: UI güncelleme satırını buradan siliyoruz!
        // OnValueChanged bunu bizim için otomatik yapacak.

        if (currentHealth.Value <= 0) {
            Die();
        }
    }

    private void Die() {
        // Bu fonksiyon hala sadece sunucuda çalışıyor.
        // Spawner'a haber ver (Bu doğru, aynı kalmalı).
        OnEnemyDied?.Invoke();

        // DÜZELTME: Anında Despawn etmek yerine, ölüm sürecini başlat.

        // 1. Düşmanı "hayalet" moduna geçir. Artık hareket edemez ve hasar alamaz.
        //GetComponent<Collider>().enabled = false;
        GetComponent<EnemyBrain>().enabled = false; // EnemyBrain script'ini devre dışı bırak
        if (TryGetComponent<UnityEngine.AI.NavMeshAgent>(out var agent)) {
            agent.enabled = false;
        }

        // 2. Tüm client'lara ölüm animasyonunu oynatmaları için RPC gönder.
        float deathAnimationDuration = 2f; // Animasyonun ne kadar süreceğini burada belirle
        PlayDeathVisualsClientRpc(deathAnimationDuration);

        // 3. Animasyon bittikten sonra objeyi yok etmek için bir coroutine başlat.
        StartCoroutine(DespawnAfterDelay(deathAnimationDuration));
    }

    [ClientRpc]
    private void PlayDeathVisualsClientRpc(float duration) {
        // Bu fonksiyon TÜM CLIENT'LARDA çalışır.
        // Burada istediğin DOTween animasyonunu yapabilirsin. İşte birkaç örnek:

        // Örnek 1: Yere Gömülme Animasyonu
        transform.DOMoveY(transform.position.y - 1.5f, duration).SetEase(Ease.InQuad);

        // Örnek 2: Saydamlaşıp Yok Olma (Fade Out)
        // Not: Bu animasyonun çalışması için düşmanın materyalinin Shader'ının
        // "Rendering Mode" ayarının "Transparent" veya "Fade" olması gerekir!
        // Aksi takdirde bu kod çalışmaz.
        var renderer = GetComponentInChildren<SkinnedMeshRenderer>(); // veya MeshRenderer
        if (renderer != null) {
            renderer.material.DOFade(0f, duration);
        }
    }

    private IEnumerator DespawnAfterDelay(float delay) {
        // Bu Coroutine SUNUCUDA çalışır.

        // Animasyon süresi kadar bekle.
        yield return new WaitForSeconds(delay);

        // Süre dolduktan sonra objeyi ağdan kaldır ve yok et.
        if (IsServer && NetworkObject != null && NetworkObject.IsSpawned) {
            NetworkObject.Despawn(true);
        }
    }
}