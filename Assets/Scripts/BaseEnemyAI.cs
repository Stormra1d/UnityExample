using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class BaseEnemyAI : MonoBehaviour
{
    protected Transform player;
    protected NavMeshAgent agent;

    public float contactDamage = 20f;

    protected virtual void Start()
    {
        player = GameObject.FindWithTag("Player").transform;
        agent = GetComponent<NavMeshAgent>();
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            Health playerHealth = other.gameObject.GetComponent<Health>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(contactDamage);
                Vector3 knockbackDirection = (other.transform.position - transform.position).normalized;
                playerHealth.ApplyKnockback(knockbackDirection, 50f);
            }
        }
    }
}
