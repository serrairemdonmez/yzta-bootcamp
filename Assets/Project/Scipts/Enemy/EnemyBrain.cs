using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using DG.Tweening;

[RequireComponent(typeof(EnemyReferences))]
public class EnemyBrain : NetworkBehaviour
{
    // References
    private EnemyReferences enemyReferences;
    private Transform currentTarget;
    private float attackDistance;

    [Header("Pathfinding")]
    [SerializeField] private float targetScanInterval = 2f;
    private float targetScanDeadline = 0f;
    private float pathUpdateDeadline = 0f;

    [Header("Attack Settings")]
    [Tooltip("Saniyede kaç kez saldırabilir (1 = 1 saniye bekleme).")]
    [SerializeField] private float attackRate = 1f;
    [Tooltip("Her saldırıda verdiği hasar miktarı.")]
    [SerializeField] private int attackDamage = 10;
    [Tooltip("Saldırı menzili (OverlapSphere yarıçapı).")]
    [SerializeField] private float attackRange = 1.5f;
    [Tooltip("Saldırı anında ileri atılacağı mesafe.")]
    [SerializeField] private float lungeDistance = 1f;
    [Tooltip("Hangi katmandaki oyuncuları vursun.")]
    [SerializeField] private LayerMask playerLayer;

    // İç sayaçlar
    private float attackCooldown = 0f;

    public override void OnNetworkSpawn() {
        if (!IsServer) {
            enabled = false;
            return;
        }

        enemyReferences = GetComponent<EnemyReferences>();
        attackDistance = enemyReferences.Agent.stoppingDistance;
    }

    private void Update() {
        if (!IsServer) return;

        // Cooldown azalt
        if (attackCooldown > 0f)
            attackCooldown -= Time.deltaTime;

        // Oyuncu yoksa dur ve bekle
        if (GameManager.Instance.AllPlayers.Count == 0) {
            currentTarget = null;
            enemyReferences.Agent.isStopped = true;
            return;
        }

        // Hedef tarama
        if (currentTarget == null || targetScanDeadline <= 0f) {
            FindNearestTarget();
            targetScanDeadline = targetScanInterval;
        }
        else {
            targetScanDeadline -= Time.deltaTime;
        }

        // Hedef varsa hareket veya saldırı
        if (currentTarget != null) {
            float distance = Vector3.Distance(transform.position, currentTarget.position);
            if (distance <= attackDistance) {
                // Menzi̇l içi: dur, dön ve saldır
                enemyReferences.Agent.isStopped = true;
                LookAtTarget();
                TryAttack();
            }
            else {
                // Menzi̇l dışı: yol güncelle ve yürü
                if (pathUpdateDeadline <= 0f) {
                    enemyReferences.Agent.SetDestination(currentTarget.position);
                    pathUpdateDeadline = enemyReferences.PathUpdateDelay;
                }
                else {
                    pathUpdateDeadline -= Time.deltaTime;
                }
                enemyReferences.Agent.isStopped = false;
            }
        }
    }

    private void FindNearestTarget() {
        Transform nearest = null;
        float minDist = float.MaxValue;

        foreach (var player in GameManager.Instance.AllPlayers) {
            if (player == null) continue;
            float d = Vector3.Distance(transform.position, player.transform.position);
            if (d < minDist) {
                minDist = d;
                nearest = player.transform;
            }
        }

        currentTarget = nearest;
    }

    private void LookAtTarget() {
        Vector3 dir = currentTarget.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f) {
            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, enemyReferences.TurnSpeed * Time.deltaTime);
        }
    }

    private void TryAttack() {
        if (attackCooldown <= 0f) {
            PerformAttack();
            attackCooldown = 1f / attackRate;
        }
    }

    private void PerformAttack() {
        // 1) Oyuncuları hasar bölgesinde tarar
        Collider[] hits = Physics.OverlapSphere(transform.position, attackRange, playerLayer);
        foreach (var hit in hits) {
            if (hit.TryGetComponent<PlayerHealthSystem>(out var phs) && !phs.IsDead) {
                phs.TakeDamage(attackDamage);

                LungeAttackClientRpc();
            }
        }

    }

    // YENİ: Görsel atılma efektini client'larda çalıştıran RPC
    [ClientRpc]
    private void LungeAttackClientRpc() {
        Vector3 targetPos = transform.position + transform.forward * lungeDistance;
        enemyReferences.Agent.isStopped = true;
        transform
            .DOMove(targetPos, 0.2f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() => {
                enemyReferences.Agent.isStopped = false;
            });
    }

    // İsteğe bağlı: sahnede düzen görmek için görünür debug çizimi
    private void OnDrawGizmosSelected() {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
