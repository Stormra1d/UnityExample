using System.Collections;
using UnityEngine;

public class PoisonTile : MonoBehaviour
{
    public float damagePerSecond = 4f;
    public float duration = 6f;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            Health health = other.GetComponentInParent<Health>();
            if (health != null)
            {
                StartCoroutine(ApplyPoison(health));
            }
        }
    }

    private IEnumerator ApplyPoison(Health target)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            target.TakeDamage(damagePerSecond);
            yield return new WaitForSeconds(1f);
            elapsed += 1f;
        }
    }
}
