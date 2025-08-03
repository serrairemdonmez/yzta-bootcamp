using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

public class WeaponDamage : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private int damage = 25;
    [SerializeField] private float sphereCastRadius = 0.5f;
    [SerializeField] private LayerMask enemyLayer;

    // Her saldırıda sadece bir kez hasar vermek için vurduğumuz düşmanları tutan liste.
    private List<Collider> alreadyHitColliders = new List<Collider>();

    // Bu fonksiyonlar Animasyon Event'leri tarafından çağrılacak.
    public void StartDealDamage() {
        // Her yeni saldırı başında listeyi temizle.
        alreadyHitColliders.Clear();
    }

    public void EndDealDamage() {
        // Bu fonksiyon, animasyon bittiğinde çağrılabilir ama şimdilik boş bırakabiliriz.
        // Asıl işi Update'te yapacağız.
    }

    private void Update() {
        // Hasar verme mantığı sadece kendi karakterimizde (IsOwner) çalışmalı.
        if (!IsOwner) return;

        // Karakterin Attacking durumunda olup olmadığını Player script'inden veya
        // Animator'den kontrol etmek daha sağlam olabilir, şimdilik basit tutalım.
        // Eğer saldırı aktifse... (Bunu daha sonra geliştireceğiz)
        DealDamage();
    }

    private void DealDamage() {
        // Bu objenin bulunduğu pozisyonda bir küre oluşturup düşmanları tara.
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, sphereCastRadius, enemyLayer);

        foreach (var hitCollider in hitColliders) {
            // Eğer bu düşmana bu vuruşta zaten hasar vermediysek...
            if (!alreadyHitColliders.Contains(hitCollider)) {
                // Listeye ekle ki aynı vuruşta tekrar hasar almasın.
                alreadyHitColliders.Add(hitCollider);

                // Düşmanın EnemyHealth component'ini al.
                if (hitCollider.TryGetComponent<EnemyHealth>(out EnemyHealth enemyHealth)) {
                    // Vurduğumuzu sunucuya bildir.
                    // Düşmanın NetworkObjectId'sini ve hasar miktarını gönderiyoruz.
                    DealDamageServerRpc(enemyHealth.NetworkObjectId, damage);
                }
            }
        }
    }

    [ServerRpc]
    private void DealDamageServerRpc(ulong enemyNetworkObjectId, int damageAmount) {
        // Sunucu, gönderilen ID'ye sahip network objesini bulur.
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(enemyNetworkObjectId, out NetworkObject enemyObject)) {
            // Objenin EnemyHealth component'ini alıp hasar fonksiyonunu çağırır.
            if (enemyObject.TryGetComponent<EnemyHealth>(out EnemyHealth enemyHealth)) {
                enemyHealth.TakeDamage(damageAmount);
            }
        }
    }

    // Editörde hasar alanını görmek için Gizmos.
    private void OnDrawGizmosSelected() {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, sphereCastRadius);
    }
}