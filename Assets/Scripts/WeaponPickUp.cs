using UnityEngine;

public class WeaponPickUp : MonoBehaviour
{
    public Weapon weaponPrefab;

    public void OnTriggerEnter(Collider collider)
    {

        ItemManager itemManager = collider.GetComponent<ItemManager>();
        if (itemManager != null)
        {
            Weapon newWeapon = Instantiate(weaponPrefab);
            itemManager.EquipWeapon(newWeapon);
            Destroy(gameObject);
        }
    }
}