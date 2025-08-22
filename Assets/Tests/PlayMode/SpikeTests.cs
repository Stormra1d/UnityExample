using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.PerformanceTesting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

[Category("Endurance")]
public class SpikeTests
{
    private readonly List<GameObject> spawnedEnemies = new();
    private readonly List<GameObject> spawnedProjectiles = new();
    private readonly List<GameObject> spawnedCollectibles = new();
    private GameObject playerGameObject;
    private List<string> errorLogs = new();

    [UnitySetUp]
    public IEnumerator Setup()
    {
        SceneManager.LoadScene("PerformanceTestScene");
        yield return new WaitForSeconds(0.1f);

        errorLogs.Clear();
        Application.logMessageReceived += OnLogMessageReceived;

        var playerPrefab = Resources.Load<GameObject>("Player");
        Assert.IsNotNull(playerPrefab, "Player prefab should exist");
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
        foreach (var gameObject in spawnedEnemies) if (gameObject) Object.DestroyImmediate(gameObject);
        foreach (var gameObject in spawnedProjectiles) if (gameObject) Object.DestroyImmediate(gameObject);
        foreach (var gameObject in spawnedCollectibles) if (gameObject) Object.DestroyImmediate(gameObject);

        spawnedEnemies.Clear();
        spawnedProjectiles.Clear();
        spawnedCollectibles.Clear();
        yield return null;
    }

    private void OnLogMessageReceived(string condition, string stackTrace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception)
        {
            errorLogs.Add($"{type}: {condition}\n{stackTrace}");
        }
    }

    [UnityTest, Performance]
    [Timeout(1000000)]
    public IEnumerator PositiveSpikeTest()
    {
        int spikeEnemies = 300;
        int spikeProjectiles = 1000;
        int spikeCollectibles = 100;

        var enemyPrefab = Resources.Load<GameObject>("EnemyAI");
        var bulletPrefab = Resources.Load<GameObject>("Bullet");
        var collectiblePrefab = Resources.Load<GameObject>("RedRuby");

        Assert.IsNotNull(enemyPrefab, "EnemyAI prefab missing");
        Assert.IsNotNull(bulletPrefab, "Bullet prefab missing");
        Assert.IsNotNull(collectiblePrefab, "RedRuby prefab missing");

        using (Measure.Frames().WarmupCount(5).MeasurementCount(10).Scope())
        using (Measure.Scope("MemoryUsage"))
        {
            for (int i = 0; i < spikeEnemies; i++)
            {
                Vector3 pos = new Vector3(Random.Range(-20, 20), 0, Random.Range(-20, 20));
                NavMeshHit hit;
                if (NavMesh.SamplePosition(pos, out hit, 2.0f, NavMesh.AllAreas))
                {
                    spawnedEnemies.Add(Object.Instantiate(enemyPrefab, hit.position, Quaternion.identity));
                }
            }
            for (int i = 0; i < spikeProjectiles; i++)
            {
                spawnedProjectiles.Add(Object.Instantiate(bulletPrefab, Random.insideUnitSphere * 10f, Quaternion.identity));
            }
            for (int i = 0; i < spikeCollectibles; i++)
            {
                spawnedCollectibles.Add(Object.Instantiate(collectiblePrefab, Random.insideUnitSphere * 10f, Quaternion.identity));
            }

            float duration = 10f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                foreach (var enemy in spawnedEnemies)
                {
                    if (enemy)
                    {
                        var navAgent = enemy.GetComponent<NavMeshAgent>();
                        if (navAgent)
                        {
                            navAgent.SetDestination(playerGameObject.transform.position);
                        }
                    }
                }
                foreach (var proj in spawnedProjectiles)
                {
                    if (proj) proj.transform.Translate(Vector3.forward * Time.deltaTime * 5f);
                }
                Measure.Custom(new SampleGroup("MemoryUsage", SampleUnit.Megabyte), Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f));
                yield return new WaitForSeconds(0.1f);
                elapsed += 0.1f;
            }
        }

        var fpsGroup = PerformanceTest.GetSampleGroup("Time");
        Assert.NotNull(fpsGroup, "FrameTime sample group not found");
        Assert.LessOrEqual(fpsGroup.Median, 33.33f, $"Median frame time {fpsGroup.Median:F2} ms exceeds 33.33ms (30 FPS)");
        Assert.IsEmpty(errorLogs, $"Errors detected: {string.Join("\n", errorLogs)}");
        yield return null;
    }

    [UnityTest, Performance]
    [Timeout(1000000)]
    public IEnumerator RepeatedSpikeTest()
    {
        int spikeEnemies = 200;
        int spikeProjectiles = 500;
        int spikeCollectibles = 50;

        int cycles = 5;
        var enemyPrefab = Resources.Load<GameObject>("EnemyAI");
        var bulletPrefab = Resources.Load<GameObject>("Bullet");
        var collectiblePrefab = Resources.Load<GameObject>("RedRuby");

        Assert.IsNotNull(enemyPrefab, "EnemyAI prefab missing");
        Assert.IsNotNull(bulletPrefab, "Bullet prefab missing");
        Assert.IsNotNull(collectiblePrefab, "RedRuby prefab missing");

        using (Measure.Frames().WarmupCount(5).MeasurementCount(10).Scope())
        using (Measure.Scope("MemoryUsage"))
        {
            for (int c = 0; c < cycles; c++)
            {
                for (int i = 0; i < spikeEnemies; i++)
                {
                    Vector3 pos = new Vector3(Random.Range(-20, 20), 0, Random.Range(-20, 20));
                    NavMeshHit hit;
                    if (NavMesh.SamplePosition(pos, out hit, 2.0f, NavMesh.AllAreas))
                    {
                        spawnedEnemies.Add(Object.Instantiate(enemyPrefab, hit.position, Quaternion.identity));
                    }
                }
                for (int i = 0; i < spikeProjectiles; i++)
                {
                    spawnedProjectiles.Add(Object.Instantiate(bulletPrefab, Random.insideUnitSphere * 10f, Quaternion.identity));
                }
                for (int i = 0; i < spikeCollectibles; i++)
                {
                    spawnedCollectibles.Add(Object.Instantiate(collectiblePrefab, Random.insideUnitSphere * 10f, Quaternion.identity));
                }

                float spikeDuration = 5f;
                float elapsed = 0f;
                while (elapsed < spikeDuration)
                {
                    foreach (var enemy in spawnedEnemies)
                    {
                        if (enemy)
                        {
                            var navAgent = enemy.GetComponent<NavMeshAgent>();
                            if (navAgent)
                            {
                                navAgent.SetDestination(playerGameObject.transform.position);
                            }
                        }
                    }

                    foreach (var proj in spawnedProjectiles)
                    {
                        if (proj)
                        {
                            proj.transform.Translate(Vector3.forward * Time.deltaTime * 5f);
                        }
                    }
                    Measure.Custom(new SampleGroup("MemoryUsage", SampleUnit.Megabyte), Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f));
                    yield return new WaitForSeconds(0.1f);
                    elapsed += 0.1f;
                }

                foreach (var gameObject in spawnedEnemies) if (gameObject) Object.DestroyImmediate(gameObject);
                foreach (var gameObject in spawnedProjectiles) if (gameObject) Object.DestroyImmediate(gameObject);
                foreach (var gameObject in spawnedCollectibles) if (gameObject) Object.DestroyImmediate(gameObject);

                spawnedEnemies.Clear();
                spawnedProjectiles.Clear();
                spawnedCollectibles.Clear();

                elapsed = 0f;
                while (elapsed < spikeDuration)
                {
                    Measure.Custom(new SampleGroup("MemoryUsage", SampleUnit.Megabyte), Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f));
                    yield return new WaitForSeconds(0.1f);
                    elapsed += 0.1f;
                }
            }
        }

        var fpsGroup = PerformanceTest.GetSampleGroup("Time");
        Assert.NotNull(fpsGroup, "FrameTime sample group not found");
        Assert.LessOrEqual(fpsGroup.Median, 33.33f, $"Median frame time {fpsGroup.Median:F2} ms exceeds 33.33ms (30 FPS)");
        Assert.IsEmpty(errorLogs, $"Errors detected: {string.Join("\n", errorLogs)}");
        yield return null;
    }

    [UnityTest, Performance]
    [Timeout(1000000)]
    public IEnumerator IsolationStressTest_UI()
    {
        errorLogs.Clear();
        var healthUI = Object.FindFirstObjectByType<PlayerHealthUI>();
        var ammoUI = Object.FindFirstObjectByType<AmmoUIManager>();

        var weaponGO = new GameObject("TestWeapon");
        var weapon = weaponGO.AddComponent<Weapon>();
        weapon.bulletsInMag = 30;
        weapon.isReloading = false;
        weapon.spareMags = new List<int> { 30, 30, 30 };

        ammoUI.currentWeapon = weapon;

        Assert.IsNotNull(healthUI, "PlayerHealthUI not found");
        Assert.IsNotNull(ammoUI, "AmmoUIManager not found");

        yield return null;

        using (Measure.Frames().WarmupCount(5).MeasurementCount(10).Scope())
        using (Measure.Scope("MemoryUsage"))
        {
            float duration = 10f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                healthUI.Update();
                ammoUI.UpdateAmmoUI();
                Measure.Custom(new SampleGroup("MemoryUsage", SampleUnit.Megabyte), Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f));
                yield return new WaitForSeconds(0.1f);
                elapsed += 0.1f;
            }
        }

        var fpsGroup = PerformanceTest.GetSampleGroup("Time");
        Assert.NotNull(fpsGroup, "FrameTime sample group not found");
        Assert.LessOrEqual(fpsGroup.Median, 33.33f, $"Median frame time {fpsGroup.Median:F2} ms exceeds 33.33ms (30 FPS)");
        Assert.IsEmpty(errorLogs, $"Errors detected: {string.Join("\n", errorLogs)}");
        yield return null;
    }
}