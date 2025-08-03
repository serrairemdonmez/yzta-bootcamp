using System.Collections.Generic;
using Unity.Netcode;
using System.Collections;
using UnityEngine;

public class EnemySpawner : NetworkBehaviour
{
    [Header("References")]
    [Tooltip("Spawn edilecek düşman prefab'ı. Üzerinde NetworkObject olmalı!")]
    [SerializeField] private GameObject enemyPrefab;

    [Header("Spawn Settings")]
    [Tooltip("Düşmanların spawn olabileceği noktaların listesi.")]
    [SerializeField] private List<Transform> spawnPoints;

    [Tooltip("İki spawn arasındaki bekleme süresi (saniye).")]
    [SerializeField] private float spawnInterval = 5f;

    [Tooltip("Sahnede aynı anda bulunabilecek maksimum düşman sayısı.")]
    [SerializeField] private int maxEnemies = 10;

    // Sunucunun anlık düşman sayısını takip etmesi için.
    private int currentEnemyCount = 0;

    public override void OnNetworkSpawn() {
        // Bu script'in tüm mantığı sadece sunucuda çalışmalı.
        if (!IsServer) return;

        // Başlangıçta sahnede olan düşmanları da saymak istersen buraya bir kod eklenebilir.

        // Düşman öldüğünde sayacı azaltmak için event'e abone ol. (Bu kısmı Adım 3'te yapacağız)
        EnemyHealth.OnEnemyDied += HandleEnemyDied;

        // Spawn döngüsünü başlat.
        StartCoroutine(SpawnEnemiesCoroutine());
    }

    public override void OnNetworkDespawn() {
        // Obje yok olduğunda event aboneliğini kaldır.
        if (!IsServer) return;
        EnemyHealth.OnEnemyDied -= HandleEnemyDied;
    }

    private IEnumerator SpawnEnemiesCoroutine() {
        // Sunucu olduğu sürece bu döngü çalışır.
        while (IsServer) {
            // Eğer mevcut düşman sayısı maksimuma ulaşmadıysa...
            if (currentEnemyCount < maxEnemies) {
                // Yeni bir düşman spawn et.
                SpawnEnemy();
            }

            // Bir sonraki spawn için bekle.
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnEnemy() {
        // Eğer hiç spawn noktası atanmamışsa hata verme.
        if (spawnPoints.Count == 0) {
            Debug.LogError("Hiç spawn noktası atanmamış!", this);
            return;
        }

        // Rastgele bir spawn noktası seç.
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Count)];

        // Prefab'i sunucuda oluştur (Instantiate).
        GameObject enemyInstance = Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);

        // Oluşturulan objenin NetworkObject component'ini al ve ağ üzerinde spawn et.
        // Bu, objenin tüm client'larda görünmesini sağlar.
        NetworkObject networkObject = enemyInstance.GetComponent<NetworkObject>();
        if (networkObject != null) {
            networkObject.Spawn(true);
            currentEnemyCount++;
            Debug.Log($"Düşman spawn edildi. Mevcut düşman sayısı: {currentEnemyCount}");
        }
        else {
            Debug.LogError("Spawn edilmeye çalışılan düşman prefab'ında NetworkObject component'i yok!", enemyPrefab);
            Destroy(enemyInstance); // Hatalı objeyi yok et.
        }
    }

    private void HandleEnemyDied() {
        // Bu fonksiyon EnemyHealth tarafından tetiklenecek.
        currentEnemyCount--;
        Debug.Log($"Bir düşman öldü. Kalan düşman sayısı: {currentEnemyCount}");
    }
}
