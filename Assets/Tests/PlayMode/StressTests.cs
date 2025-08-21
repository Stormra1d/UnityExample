using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using Unity.PerformanceTesting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

[Category("Stress")]
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
        yield return new WaitForSeconds(0.1f);

        errorLogs.Clear();
        Application.logMessageReceived += OnLogMessageReceived;

        var playerPrefab = Resources.Load<GameObject>("Player");
        Assert.IsNotNull(playerPrefab, "Player prefab not found");
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
    [Timeout(300000)]
    public IEnumerator SystemStressTest()
    {
        int numEnemies = 500;
        int numProjectiles = 1000;
        int numCollectibles = 100;

        var enemyPrefab = Resources.Load<GameObject>("EnemyAI");
        var bulletPrefab = Resources.Load<GameObject>("Bullet");
        var collectiblePrefab = Resources.Load<GameObject>("RedRuby");
        Assert.IsNotNull(enemyPrefab, "EnemyAI prefab not found");
        Assert.IsNotNull(bulletPrefab, "Bullet prefab not found");
        Assert.IsNotNull(collectiblePrefab, "RedRuby prefab not found");

        for (int i = 0; i < numEnemies; i++)
        {
            Vector3 pos = new Vector3(Random.Range(-20, 20), 0, Random.Range(-20, 20));
            if (NavMesh.SamplePosition(pos, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
            {
                spawnedEnemies.Add(Object.Instantiate(enemyPrefab, hit.position, Quaternion.identity));
            }
            if (i % 50 == 0) 
            {
                yield return null;
            }
        }

        for (int i = 0; i < numProjectiles; i++)
        {
            Vector3 pos = new Vector3(Random.Range(-10, 10), 1f, Random.Range(-10, 10));
            var gameObject = Object.Instantiate(bulletPrefab, pos, Quaternion.identity);
            var rb = gameObject.GetComponent<Rigidbody>();
            if (rb != null)
                rb.linearVelocity = Random.onUnitSphere * 10f;
            spawnedProjectiles.Add(gameObject);
            if (i % 100 == 0)
            {
                yield return null;
            }
        }

        for (int i = 0; i < numCollectibles; i++)
        {
            Vector3 pos = new Vector3(Random.Range(-20, 20), 0.5f, Random.Range(-20, 20));
            spawnedCollectibles.Add(Object.Instantiate(collectiblePrefab, pos, Quaternion.identity));
            if (i % 20 == 0) 
            {
                yield return null;
            } 
        }

        using (Measure.Frames().WarmupCount(5).MeasurementCount(10).Scope())
        using (Measure.Scope("MemoryUsage"))
        {
            float duration = 5f, elapsed = 0f;
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
                    if (proj)
                    {
                        var rb = proj.GetComponent<Rigidbody>();
                        if (rb)
                        {
                            rb.AddForce(Random.onUnitSphere * 5f, ForceMode.Impulse);
                        }
                    }
                }
                Measure.Custom(new SampleGroup("MemoryUsage", SampleUnit.Megabyte), Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f));
                yield return new WaitForSeconds(0.5f);
                elapsed += 0.5f;
            }
        }

        var fpsGroup = PerformanceTest.GetSampleGroup("Time");
        Assert.NotNull(fpsGroup, "Time sample group not found");
        Assert.LessOrEqual(fpsGroup.Median, 33.33f, $"Median frame time {fpsGroup.Median:F2} ms exceeds 33.33ms (30 FPS)");
        Assert.IsEmpty(errorLogs, $"Errors detected: {string.Join("\n", errorLogs)}");
        yield return null;
    }

    [UnityTest, Performance]
    [Timeout(300000)]
    public IEnumerator PhysicsStressTest()
    {
        int numProjectiles = 5000;
        var bulletPrefab = Resources.Load<GameObject>("Bullet");
        Assert.IsNotNull(bulletPrefab, "Bullet Prefab should be available");

        for (int i = 0; i < numProjectiles; i++)
        {
            Vector3 pos = new Vector3(Random.Range(-10, 10), 1f, Random.Range(-10, 10));
            var gameObject = Object.Instantiate(bulletPrefab, pos, Quaternion.identity);
            var rb = gameObject.GetComponent<Rigidbody>();
            if (rb != null)
                rb.linearVelocity = Random.onUnitSphere * 10f;
            spawnedProjectiles.Add(gameObject);
            if (i % 100 == 0)
            {
                yield return null;
            }
        }

        using (Measure.Frames().WarmupCount(5).MeasurementCount(10).Scope())
        using (Measure.ProfilerMarkers(new[] { "Physics.Simulate" }))
        {
            float duration = 5f, elapsed = 0f;
            while (elapsed < duration)
            {
                foreach (var proj in spawnedProjectiles)
                {
                    if (proj)
                    {
                        var rb = proj.GetComponent<Rigidbody>();
                        if (rb)
                        {
                            rb.AddForce(Random.onUnitSphere * 5f, ForceMode.Impulse);
                        }
                    }
                }
                yield return new WaitForSeconds(0.5f);
                elapsed += 0.5f;
            }
        }

        var fpsGroup = PerformanceTest.GetSampleGroup("Time");
        var physicsGroup = PerformanceTest.GetSampleGroup("Physics.Simulate");
        Assert.NotNull(fpsGroup, "Time sample group not found");
        Assert.NotNull(physicsGroup, "Physics.Simulate sample group not found");
        Assert.LessOrEqual(fpsGroup.Median, 33.33f, $"Median frame time {fpsGroup.Median:F2} ms exceeds 33.33ms (30 FPS)");
        Assert.LessOrEqual(physicsGroup.Median, 0.1f, $"Physics.Simulate median time {physicsGroup.Median:F2} ms too high");
        Assert.IsEmpty(errorLogs, $"Errors detected: {string.Join("\n", errorLogs)}");
        yield return null;
    }

    [UnityTest, Performance]
    [Timeout(300000)]
    public IEnumerator RenderingStressTest()
    {
        int numEnemies = 500;
        var enemyPrefab = Resources.Load<GameObject>("EnemyAI");
        Assert.IsNotNull(enemyPrefab, "EnemyAI prefab not found");

        for (int i = 0; i < numEnemies; i++)
        {
            Vector3 pos = new Vector3(Random.Range(-20, 20), 0, Random.Range(-20, 20));
            if (NavMesh.SamplePosition(pos, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
            {
                spawnedEnemies.Add(Object.Instantiate(enemyPrefab, hit.position, Quaternion.identity));
                if (i % 50 == 0)
                {
                    yield return null;
                } 
            }
        }

        using (Measure.ProfilerMarkers(new[] { "Camera.Render" }))
        {
            float duration = 5f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                var camera = Camera.main;
                if (camera)
                {
                    camera.transform.RotateAround(Vector3.zero, Vector3.up, 10f * Time.deltaTime);
                } 
                yield return new WaitForSeconds(0.5f);
                elapsed += 0.5f;
            }
        }

        var renderData = PerformanceTest.GetSampleGroup("Camera.Render");
        Assert.NotNull(renderData, "Camera.Render sample group not found");
        Assert.LessOrEqual(renderData.Median, 0.1f, $"Camera.Render median time {renderData.Median:F2} ms too high");
        Assert.IsEmpty(errorLogs, $"Errors detected: {string.Join("\n", errorLogs)}");
        yield return null;
    }
}
