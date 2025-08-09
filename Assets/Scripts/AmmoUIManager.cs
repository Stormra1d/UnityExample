using TMPro;
using UnityEngine;

public class AmmoUIManager : MonoBehaviour
{
    public Weapon currentWeapon;
    public TextMeshProUGUI ammo;

    public void UpdateAmmoUI()
    {
        int total = 0;
        foreach (int mag in currentWeapon.spareMags)
        {
            total += mag;
        }
        ammo.text = $"{currentWeapon.bulletsInMag}/{total}";
        ammo.color = currentWeapon.isReloading ? Color.red : Color.white;
    }
}
