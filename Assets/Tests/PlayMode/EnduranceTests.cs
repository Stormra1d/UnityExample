using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

[Category("Endurance")]
public class EnduranceTests
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
        foreach (var gameObject in spawnedCollectibles) if (gameObject) Object.DestroyImmediate(gameObject);

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
    [Timeout(1000000)]
    public IEnumerator GeneralEnduranceTest()
    {
        int numEnemies = 50;
        int numProjectiles = 20;
        int numCollectibles = 10;

        var enemyPrefab = Resources.Load<GameObject>("EnemyAI");
        var bulletPrefab = Resources.Load<GameObject>("Bullet");
        var collectiblePrefab = Resources.Load<GameObject>("RedRuby");

        Assert.IsNotNull(enemyPrefab, "EnemyAI prefab not found");
        Assert.IsNotNull(bulletPrefab, "Bullet prefab not found");
        Assert.IsNotNull(collectiblePrefab, "RedRuby prefab not found");

        for (int i = 0; i < numEnemies; i++)
        {
            Vector3 pos = new Vector3(Random.Range(-20, 20), 0, Random.Range(-20, 20));
            NavMeshHit hit;
            if (NavMesh.SamplePosition(pos, out hit, 2.0f, NavMesh.AllAreas))
            {
                spawnedEnemies.Add(Object.Instantiate(enemyPrefab, hit.position, Quaternion.identity));
            }
        }
        for (int i = 0; i < numProjectiles; i++)
        {
            spawnedProjectiles.Add(Object.Instantiate(bulletPrefab, Random.insideUnitSphere * 10f, Quaternion.identity));
        }
        for (int i = 0; i < numCollectibles; i++)
        {
            spawnedCollectibles.Add(Object.Instantiate(collectiblePrefab, Random.insideUnitSphere * 10f, Quaternion.identity));
        }

        using (Measure.Frames().WarmupCount(10).MeasurementCount(20).Scope())
        using (Measure.Scope("MemoryUsage"))
        {
            float duration = 60f;
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
                    if (proj)
                    {
                        proj.transform.Translate(Vector3.forward * Time.deltaTime * 5f);
                    }
                }
                Measure.Custom(new SampleGroup("MemoryUsage", SampleUnit.Megabyte), Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f));
                yield return new WaitForSeconds(0.1f);
                elapsed += 0.1f;
            }
        }

        Assert.IsEmpty(errorLogs, $"Errors detected: {string.Join("\n", errorLogs)}");
        yield return null;
    }

    [UnityTest, Performance]
    [Timeout(1000000)]
    public IEnumerator MemoryLeakTest()
    {
        int numEnemies = 50;
        var enemyPrefab = Resources.Load<GameObject>("EnemyAI");

        Assert.IsNotNull(enemyPrefab, "EnemyAI prefab not found");

        for (int i = 0; i < numEnemies; i++)
        {
            Vector3 pos = new Vector3(Random.Range(-20, 20), 0, Random.Range(-20, 20));
            NavMeshHit hit;
            if (NavMesh.SamplePosition(pos, out hit, 2.0f, NavMesh.AllAreas))
            {
                spawnedEnemies.Add(Object.Instantiate(enemyPrefab, hit.position, Quaternion.identity));
            }
        }

        using (Measure.Scope("MemoryUsage"))
        {
            float duration = 60f;
            float elapsed = 0f;
            float initialMemory = Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f);
            while (elapsed < duration)
            {
                if (Random.value < 0.05f)
                {
                    int index = Random.Range(0, spawnedEnemies.Count);
                    if (spawnedEnemies[index])
                    {
                        Object.Destroy(spawnedEnemies[index]);
                        Vector3 pos = new Vector3(Random.Range(-20, 20), 0, Random.Range(-20, 20));
                        if (NavMesh.SamplePosition(pos, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
                        {
                            spawnedEnemies[index] = Object.Instantiate(enemyPrefab, hit.position, Quaternion.identity);
                        }
                    }
                }
                Measure.Custom(new SampleGroup("MemoryUsage", SampleUnit.Megabyte), Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f));
                yield return new WaitForSeconds(0.1f);
                elapsed += 0.1f;
            }

            float finalMemory = Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f);
            float delta = finalMemory - initialMemory;
            Assert.LessOrEqual(delta, 5f, $"Memory leak detected: Delta {delta:F2} MB exceeds 5 MB");
        }

        yield return null;
    }

    [UnityTest, Performance]
    [Timeout(1000000)]
    public IEnumerator PerformanceDegredationTest()
    {
        var enemyPrefab = Resources.Load<GameObject>("EnemyAI");
        Assert.IsNotNull(enemyPrefab, "EnemyAI prefab not found");

        var frameGroup = new SampleGroup(name: "FrameTime", unit: SampleUnit.Millisecond, increaseIsBetter: false);

        using (Measure.Frames().SampleGroup(frameGroup).WarmupCount(10).MeasurementCount(60).Scope())
        {
            float duration = 60f;
            float elapsed = 0f;
            int maxEnemies = 100;
            int enemiesPerStep = 10;
            while (elapsed < duration)
            {
                if (elapsed % 10f < 0.1f && spawnedEnemies.Count < maxEnemies)
                {
                    for (int i = 0; i < enemiesPerStep; i++)
                    {
                        Vector3 pos = new Vector3(Random.Range(-20, 20), 0, Random.Range(-20, 20));
                        if (NavMesh.SamplePosition(pos, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
                        {
                            spawnedEnemies.Add(Object.Instantiate(enemyPrefab, hit.position, Quaternion.identity));
                        }
                    }
                }
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
                yield return null;
                elapsed += Time.deltaTime;
            }
        }

        var fpsGroup = PerformanceTest.GetSampleGroup("FrameTime") ?? PerformanceTest.GetSampleGroup("Time");
        Assert.NotNull(fpsGroup, "No frame time sample group found");
        Assert.NotNull(fpsGroup, "FrameTime sample group not found");
        Assert.LessOrEqual(fpsGroup.Median, 16.67f,$"Median frame time {fpsGroup.Median:F2} ms exceeds 16.67 ms (60 FPS)");
    }

    [UnityTest, Performance]
    [Timeout(1000000)]
    public IEnumerator RenderingPerformanceTest()
    {
        var enemyPrefab = Resources.Load<GameObject>("EnemyAI");
        Assert.IsNotNull(enemyPrefab, "EnemyAI prefab not found");

        for (int i = 0; i < 50; i++)
        {
            Vector3 pos = new Vector3(Random.Range(-20, 20), 0, Random.Range(-20, 20));
            if (NavMesh.SamplePosition(pos, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
            {
                spawnedEnemies.Add(Object.Instantiate(enemyPrefab, hit.position, Quaternion.identity));
            }
        }

        using (Measure.ProfilerMarkers(new[] { "Camera.Render" }))
        {
            var camera = Camera.main;
            if (camera)
            {
                camera.transform.RotateAround(Vector3.zero, Vector3.up, 10f * Time.deltaTime);
            }
            yield return new WaitForSeconds(5f);
        }

        var renderData = PerformanceTest.GetSampleGroup("Camera.Render");
        Assert.NotNull(renderData, "Camera.Render sample group not found");
        Assert.LessOrEqual(renderData.Median, 0.1f, $"Camera.Render median time {renderData.Median:F2} ms too high");
        yield return null;
    }
}
