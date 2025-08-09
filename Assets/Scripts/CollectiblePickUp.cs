using UnityEngine;

public class CollectiblePickUp : MonoBehaviour
{
    public CollectibleType type;
    public int amount = 1;

    public void OnTriggerEnter(Collider collider)
    {
        ItemManager itemManager = collider.GetComponent<ItemManager>();
        if (itemManager != null)
        {
            itemManager.AddCollectible(type, amount);
            Destroy(gameObject);
        }
    }
}
