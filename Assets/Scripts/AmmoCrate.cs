using UnityEngine;

public class AmmoCrate : MonoBehaviour
{
    public int extraMags = 1;

    private void OnTriggerEnter(Collider other)
    {
        Weapon weapon = other.GetComponentInChildren<Weapon>();
        if (weapon != null)
        {
            for (int i = 0; i < extraMags; i++)
            {
                weapon.spareMags.Add(weapon.magazineSize);
            }

            weapon.itemManager?.ammoUIManager?.UpdateAmmoUI();
            Destroy(gameObject);
        }
    }
}
