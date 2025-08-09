using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Lengthy setup? Idk, early tests are much different than my later tests. Standardize!
/// </summary>
public class WeaponAmmoSystemTest
{

    GameObject playerGameObject;
    Weapon weapon;
    GameObject bulletPrefab;
    GameObject bulletSpawnGameObject;
    Camera camera;
    GameObject uiGameobject;
    AmmoUIManager ammoUIManager;
    TextMeshProUGUI ammoText;

    [SetUp]
    public void Setup()
    {
        var cameraGameObject = new GameObject("Camera");
        camera  = cameraGameObject.AddComponent<Camera>();
        cameraGameObject.transform.position = Vector3.zero;

        bulletPrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bulletPrefab.AddComponent<Rigidbody>();
        bulletPrefab.AddComponent<Bullet>();

        bulletSpawnGameObject = new GameObject("BulletSpawn");
        bulletSpawnGameObject.transform.position = cameraGameObject.transform.position + cameraGameObject.transform.forward * 1f;

        playerGameObject = new GameObject("Player");
        weapon = playerGameObject.AddComponent<Weapon>();
        weapon.playerCamera = camera;
        weapon.bulletSpawn = bulletSpawnGameObject.transform;
        weapon.bulletPrefab = bulletPrefab;
        weapon.magazineSize = 3;
        weapon.reloadTime = 1f;
        weapon.shootingDelay = 0.2f;
        weapon.bulletsInMag = weapon.magazineSize;
        weapon.spareMags.Clear();
        weapon.spareMags.Add(3);
        weapon.spareMags.Add(3);

        weapon.readyToShoot = true;
        weapon.isReloading = false;
        weapon.isShooting = false;

        uiGameobject = new GameObject("AmmoUI");
        ammoUIManager = uiGameobject.AddComponent<AmmoUIManager>();
        ammoText = uiGameobject.AddComponent<TextMeshProUGUI>();
        ammoUIManager.ammo = ammoText;
        ammoUIManager.currentWeapon = weapon;
    }

    [TearDown]
    public void Teardown()
    {
        if(weapon != null)
        {
            weapon.StopAllCoroutines();
            playerGameObject.GetComponent<MonoBehaviour>()?.StopAllCoroutines();
        }

        Object.DestroyImmediate(uiGameobject);
        Object.DestroyImmediate(playerGameObject);
        Object.DestroyImmediate(ammoUIManager);
        Object.DestroyImmediate(ammoText);
        Object.DestroyImmediate(weapon);
        Object.DestroyImmediate(bulletPrefab);
        Object.DestroyImmediate(bulletSpawnGameObject);
        Object.DestroyImmediate(camera.gameObject);

        Time.timeScale = 1.0f;
    }

    [UnityTest]
    [Timeout(100000000)]
    public IEnumerator ReloadingWeapon_AfterEmptyingMagazine_UpdatesAmmoUIAndWeaponReady()
    {
        yield return null;

        ammoUIManager.UpdateAmmoUI();

        Assert.AreEqual("3/6", ammoText.text, "UI should show correct initial state");

        for (int i = 0; i < weapon.magazineSize; i++)
        {
            weapon.TryFireWeapon();
            yield return new WaitUntil(() => weapon.readyToShoot);
            yield return null;
            ammoUIManager.UpdateAmmoUI();
            Assert.AreEqual($"{weapon.bulletsInMag}/{TotalSpareAmmo(weapon)}", ammoText.text, "UI matches weapon state");
        }

        Assert.AreEqual(0, weapon.bulletsInMag, "Should be empty after emptying mag");

        weapon.TryFireWeapon();
        yield return new WaitForSeconds(weapon.shootingDelay + 0.01f);
        Assert.AreEqual(0, weapon.bulletsInMag, "No bullets should be fired as mag is empty");

        ammoUIManager.UpdateAmmoUI();
        Assert.AreEqual("0/6", ammoText.text, "UI shows empty mag with full spares");

        yield return new WaitForSeconds(2.0f);
        ammoUIManager.UpdateAmmoUI();

        Assert.AreEqual(3, weapon.bulletsInMag, "Reloaded mag should be full");
        Assert.AreEqual(1, weapon.spareMags.Count, "1 mag remaining");
        Assert.AreEqual("3/3", ammoText.text, "UI shows full mag");

        weapon.TryFireWeapon();
        yield return new WaitForSeconds(weapon.shootingDelay + 0.01f);
        ammoUIManager.UpdateAmmoUI();

        Assert.AreEqual(2, weapon.bulletsInMag, "2 Bullets remaining after firing");
        Assert.AreEqual("2/3", ammoText.text, "UI updates after firing");
    }

    private int TotalSpareAmmo(Weapon w)
    {
        int total = 0;
        foreach (int mag in w.spareMags)
        {
            total += mag;
        }
        return total;
    }
}
