using UnityEngine;
using UnityEngine.AI;

public class EnemyMovementController : MonoBehaviour
{
    public Transform player;
    public NavMeshAgent navMeshAgent;

    private void Update()
    {
        if (player != null)
        {
            navMeshAgent.SetDestination(player.position);
        }

    }
}
