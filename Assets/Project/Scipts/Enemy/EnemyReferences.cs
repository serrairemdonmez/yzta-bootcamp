using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public class EnemyReferences : MonoBehaviour
{
    public NavMeshAgent Agent { get; private set; }

    [Header("Stats")]
    [SerializeField] private float pathUpdateDelay = .25f;
    [SerializeField] private float turnSpeed = 4f;

    public float PathUpdateDelay => pathUpdateDelay;
    public float TurnSpeed => turnSpeed;

    private void Awake() {
        Agent = GetComponent<NavMeshAgent>();
    }
}   
