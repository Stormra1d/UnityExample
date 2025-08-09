using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Weapon : MonoBehaviour
{

    public Camera playerCamera;
    public bool isShooting;
    public bool readyToShoot = true;
    public bool isAutomatic = true;
    public float shootingDelay = 1.5f;

    public GameObject bulletPrefab;
    public Transform bulletSpawn;
    public float bulletVelocity = 40;
    public float bulletLifeTime = 5f;

    public float reloadTime;
    public int magazineSize;
    public int bulletsInMag;
    public bool isReloading;
    public List<int> spareMags = new List<int>();

    public ItemManager itemManager;

    public float damage = 20f;

    private void Awake()
    {
        readyToShoot = true;
        bulletsInMag = magazineSize;
        for (int i = 0; i < 3; i++)
        {
            spareMags.Add(magazineSize);
        }

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
    }

    void Update()
    {
        if (isAutomatic)
        {
            isShooting = Input.GetKey(KeyCode.Mouse0);
        }
        else
        {
            isShooting = Input.GetKeyDown(KeyCode.Mouse0);
        }

        if ((Input.GetKeyDown(KeyCode.R) && bulletsInMag < magazineSize && spareMags.Count > 0 && !isReloading) || (bulletsInMag <= 0 && spareMags.Count > 0 && !isReloading))
        {
            TryReloading();
        }

        if (isShooting)
        {
            TryFireWeapon();
        }
    }

    public void TryFireWeapon()
    {
        if (readyToShoot && bulletsInMag > 0 && !isReloading)
        {
            StartCoroutine(FireWeaponCoroutine());
        }
    }

    public IEnumerator FireWeaponCoroutine()
    {
        readyToShoot = false;

        bulletsInMag = Mathf.Max(0, bulletsInMag - 1);
        itemManager?.ammoUIManager?.UpdateAmmoUI();

        Vector3 shootingDirection = CalculateDirection().normalized;

        if (bulletPrefab == null || bulletSpawn == null)
        {
            readyToShoot = true;
            yield break;
        }
        GameObject bullet = Instantiate(bulletPrefab, bulletSpawn.position, Quaternion.identity);

        
        Bullet bulletScript = bullet.GetComponent<Bullet>();
        if (bulletScript != null)
        {
            bulletScript.damage = this.damage;
        }
        

        Vector3 shotPos = bullet.transform.position;
        bullet.transform.forward = shootingDirection;
        bullet.GetComponent<Rigidbody>().AddForce(shootingDirection * bulletVelocity, ForceMode.Impulse);

        
        foreach (EnemyAI ai in FindObjectsByType<EnemyAI>(FindObjectsSortMode.None))
        {
            ai.OnPlayerShot(shotPos);
        }
        

        StartCoroutine(DestroyBullet(bullet, bulletLifeTime));

       yield return new WaitForSeconds(shootingDelay);

        readyToShoot = true;
    }

    public void TryReloading()
    {
        isReloading = true;
        StartCoroutine(ReloadCoroutine());
    }

    private IEnumerator ReloadCoroutine()
    {

        itemManager?.ammoUIManager?.UpdateAmmoUI();
        yield return new WaitForSeconds(reloadTime);

        if (spareMags.Count > 0)
        {
            bulletsInMag = spareMags[0];
            spareMags.RemoveAt(0);
        }
        isReloading = false;
        itemManager?.ammoUIManager?.UpdateAmmoUI();
    }

    private Vector3 CalculateDirection()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        Vector3 targetPoint;
        if (Physics.Raycast(ray, out hit))
        {
            targetPoint = hit.point;
        }
        else
        {
            targetPoint = ray.GetPoint(100);
        }

        return targetPoint - bulletSpawn.position;
    }

    private IEnumerator DestroyBullet(GameObject bullet, float bulletLifeTime)
    {
        yield return new WaitForSeconds(bulletLifeTime);
        Destroy(bullet);
    }
}