using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using Unity.PerformanceTesting;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

public class StressTests
{
    private List<GameObject> spawnedEnemies = new();
    private List<GameObject> spawnedProjectiles = new();
    private List<GameObject> spawnedCollectibles = new();
    private GameObject playerGameObject;

    private List<string> errorLogs = new();

    [UnitySetUp]
    public IEnumerator Setup()
    {
        SceneManager.LoadScene("PerformanceTestScene");
        yield return null;

        errorLogs.Clear();
        Application.logMessageReceived += OnLogMessageReceived;

        var playerPrefab = Resources.Load<GameObject>("Player");
        Assert.IsNotNull(playerPrefab);
        playerGameObject = Object.Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);

        spawnedEnemies.Clear();
        spawnedProjectiles.Clear();
        spawnedCollectibles.Clear();

        yield return null;
    }

    [UnityTearDown]
    public IEnumerator Teardown()
    {
        Application.logMessageReceived -= OnLogMessageReceived;

        if (playerGameObject) Object.DestroyImmediate(playerGameObject);
        foreach (var gameObject in spawnedEnemies) if (gameObject) Object.DestroyImmediate (gameObject);
        foreach (var gameObject in spawnedProjectiles) if (gameObject) Object.DestroyImmediate(gameObject);
        foreach (var gameObject in spawnedProjectiles) if (gameObject) Object.DestroyImmediate(gameObject);

        spawnedEnemies.Clear();
        spawnedProjectiles.Clear();
        spawnedCollectibles.Clear();

        yield return null;
    }

    void OnLogMessageReceived(string condition, string stackTrace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception)
        {
            errorLogs.Add($"{type}: {condition}\n{stackTrace}");
        }
    }

    [UnityTest, Performance]
    public IEnumerator SystemStressTest()
    {
        int numEnemies = 2000;
        int numProjectiles = 5000;
        int numCollectibles = 500;

        var enemyPrefab = Resources.Load<GameObject>("EnemyAI");
        for (int i = 0; i < numEnemies; i++)
        {
            Vector3 pos = new Vector3(Random.Range(-20, 20), 0, Random.Range(-20, 20));
            var gameObject = Object.Instantiate(enemyPrefab, pos, Quaternion.identity);
            spawnedEnemies.Add(gameObject);
        }

        var bulletPrefab = Resources.Load<GameObject>("Bullet");
        for (int i = 0; i < numProjectiles; i++)
        {
            Vector3 pos = new Vector3(Random.Range(-10, 10), 1f, Random.Range(-10, 10));
            var gameObject = Object.Instantiate(bulletPrefab, pos, Quaternion.identity);
            var rb = gameObject.GetComponent<Rigidbody>();
            if (rb != null)
                rb.linearVelocity = Random.onUnitSphere * 30f;
            spawnedProjectiles.Add(gameObject);
        }

        var collectiblePrefab = Resources.Load<GameObject>("RedRuby");
        for (int i = 0; i < numCollectibles; i++)
        {
            Vector3 pos = new Vector3(Random.Range(-20, 20), 0.5f, Random.Range(-20, 20));
            var gameObject = Object.Instantiate(collectiblePrefab, pos, Quaternion.identity);
            spawnedCollectibles.Add(gameObject);
        }

        yield return new WaitForSeconds(5f);

        for (int i = 0; i < 60; i++)
        {
            yield return null;
            Measure.Custom("SystemStress_FPS", 1.0 / Time.deltaTime);
            Measure.Custom("SystemStress_MemoryDB", Profiler.GetTotalAllocatedMemoryLong() / (1024.0f * 1024.0f));
        }

        Debug.Log($"System Stress Test: {errorLogs.Count} errors/exceptions captured");
        foreach (var error in errorLogs) Debug.LogError(error);

        yield return null;
    }

    /// <summary>
    /// Seems kinda lame. Just the same as above but because those components can't really exist isolated, what is the point?
    /// </summary>
    /// <returns></returns>
    [UnityTest, Performance]
    public IEnumerator ComponentStressTest_Projectiles()
    {
        int numProjectiles = 50000;

        spawnedProjectiles.Clear();

        var bulletPrefab = Resources.Load<GameObject>("Bullet");
        Assert.IsNotNull(bulletPrefab, "Bullet Prefab should be available");

        for (int i = 0; i< numProjectiles; i++)
        {
            Vector3 pos = new Vector3(Random.Range(-10, 10), 1f, Random.Range(-10, 10));
            var gameObject = Object.Instantiate(bulletPrefab, pos, Quaternion.identity);
            var rb = gameObject.GetComponent<Rigidbody>();
            if (rb != null)
                rb.linearVelocity = Random.onUnitSphere * 30f;
            spawnedProjectiles.Add(gameObject);
        }
        yield return new WaitForSeconds(2f);

        for (int i = 0; i  < 60; i++)
        {
            yield return null;
            Measure.Custom("ComponentStress_Projectile_FPS", 1.0f / Time.deltaTime);
            Measure.Custom("ComponentStress_Projectile_MemoryMB", Profiler.GetTotalAllocatedMemoryLong() / (1024.0f * 1024.0f));
        }

        Debug.Log($"Component Stress Projectiles: {errorLogs.Count} errors / exceptions captured");
        foreach (var error in errorLogs) Debug.LogError(error);
    }

    [UnityTest, Performance]
    public IEnumerator IsolationStressTest_UI()
    {
        long memBefore = Profiler.GetTotalAllocatedMemoryLong();
        errorLogs.Clear();
        playerGameObject.AddComponent<Health>();

        ItemManager itemManager =  playerGameObject.GetComponent<ItemManager>();
        GameObject weaponPrefab = Resources.Load<GameObject>("Rifle");
        GameObject weaponObject = UnityEngine.Object.Instantiate(weaponPrefab);
        Weapon newWeapon = weaponObject.GetComponent<Weapon>();
        var healthUI = UnityEngine.Object.FindFirstObjectByType<PlayerHealthUI>();
        var ammoUI = UnityEngine.Object.FindFirstObjectByType<AmmoUIManager>();

        itemManager.ammoUIManager = ammoUI;
        itemManager.EquipWeapon(newWeapon);

        Assert.IsNotNull(healthUI);
        Assert.IsNotNull(ammoUI);

        for (int i = 0; i < 10000; i++)
        {
            healthUI.Update();
            ammoUI.UpdateAmmoUI();
        }

        yield return null;

        //TODO does this make sense
        float allowedThreshold = 2 * 1024 * 1024;
        long memAfter = Profiler.GetTotalAllocatedMemoryLong();
        Assert.LessOrEqual(memAfter, memBefore + allowedThreshold, "Memory leak detected");

        Measure.Custom("IsolationStress_UI_MemoryMB", Profiler.GetTotalAllocatedMemoryLong() / (1024.0f * 1024.0f));
        Debug.Log($"Isolation Stress UI: {errorLogs.Count} errors/exceptions captured");
        foreach (var error in errorLogs) Debug.LogError(error);

        yield return null;
    }
}
