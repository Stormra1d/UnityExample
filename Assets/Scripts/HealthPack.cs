using UnityEngine;

public class HealthPack : MonoBehaviour
{
    public float healAmount = 25f;

    public void OnTriggerEnter(Collider other)
    {
        Health health = other.GetComponent<Health>();
        if (health != null)
        {
            health.Heal(healAmount);
            Destroy(gameObject);
        }
    }
}
