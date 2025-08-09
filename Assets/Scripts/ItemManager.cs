using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public enum CollectibleType
{
    RedGem,
    BlueCrystal,
}

public class ItemManager : MonoBehaviour
{
    public Transform weaponSlot;
    public AmmoUIManager ammoUIManager;

    private List<Weapon> weapons = new();
    private int currentWeaponIndex = 0;

    public Weapon CurrentWeapon => weapons.Count > 0 ? weapons[currentWeaponIndex] : null;

    private Dictionary<CollectibleType, int> collectibleCounts = new();
    public CollectibleUIManager collectibleUIManager;

    public void EquipWeapon(Weapon newWeapon)
    {
        if (weapons.Contains(newWeapon)) return;

        newWeapon.transform.SetParent(weaponSlot);
        newWeapon.transform.localPosition = Vector3.zero;
        newWeapon.transform.localRotation = Quaternion.identity;

        newWeapon.itemManager = this;

        if (weapons.Count > 0)
        {
            weapons[currentWeaponIndex].gameObject.SetActive(false);
        }

        weapons.Add(newWeapon);
        currentWeaponIndex = weapons.Count - 1;
        newWeapon.gameObject.SetActive(true);

        if (ammoUIManager != null)
        {
            ammoUIManager.currentWeapon = weapons[currentWeaponIndex];
            ammoUIManager.UpdateAmmoUI();
        }
    }

    private void Update()
    {
        HandleWeaponSwitch();
    }

    private void HandleWeaponSwitch()
    {
        if (weapons.Count <= 1) return;
        int scroll = (int)Input.mouseScrollDelta.y;
        if (scroll != 0)
        {
            int nextIndex = (currentWeaponIndex + (scroll > 0 ? 1 : -1) + weapons.Count) % weapons.Count;
            SwitchToWeapon(nextIndex);
        }

        for (int i = 0; i < weapons.Count && i < 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                SwitchToWeapon(i);
            }
        }
    }

    private void SwitchToWeapon(int nextIndex)
    {
        if (nextIndex == currentWeaponIndex) return;

        weapons[currentWeaponIndex].gameObject.SetActive(false);
        currentWeaponIndex = nextIndex;
        weapons[currentWeaponIndex].gameObject.SetActive(true);

        if (ammoUIManager != null)
        {
            ammoUIManager.currentWeapon = weapons[currentWeaponIndex];
            ammoUIManager.UpdateAmmoUI();
        }
    }

    public void AddCollectible(CollectibleType type, int amount = 1)
    {
        if (!collectibleCounts.ContainsKey(type))
        {
            collectibleCounts[type] = 0;
        }

        collectibleCounts[type] += amount;

        if (collectibleUIManager != null)
        {
            collectibleUIManager.UpdateCollectibleUI(type, collectibleCounts[type]);
        }
    }

    public int GetCollectibleCount(CollectibleType type)
    {
        return collectibleCounts.TryGetValue(type, out int count) ? count : 0;
    }
}
