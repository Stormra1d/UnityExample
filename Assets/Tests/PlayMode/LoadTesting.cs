using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.PerformanceTesting;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using UnityEngine.AI;
using UnityEngine.Profiling;

[Category("Endurance")]
public class LoadTesting
{
    private GameObject playerGameObject;
    private readonly string playerPrefabPath = "Player";
    private readonly string enemyPrefabPath = "EnemyAI";
    private List<GameObject> spawnedEnemies = new();
    private List<string> errorLogs = new();

    [UnitySetUp]
    public IEnumerator Setup()
    {
        SceneManager.LoadScene("PerformanceTestScene");
        yield return null;

        errorLogs.Clear();
        Application.logMessageReceived += OnLogMessageReceived;

        var playerPrefab = Resources.Load<GameObject>(playerPrefabPath);
        Assert.IsNotNull(playerPrefab, "Player prefab should be available");
        playerGameObject = Object.Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);

        spawnedEnemies.Clear();
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator Teardown()
    {
        Application.logMessageReceived -= OnLogMessageReceived;

        if (playerGameObject) Object.DestroyImmediate(playerGameObject);
        foreach (var enemy in spawnedEnemies)
        {
            if (enemy)
            {
                Object.DestroyImmediate(enemy);
            }
        }
        spawnedEnemies.Clear();
        yield return null;
    }

    private void OnLogMessageReceived(string condition, string stackTrace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception)
            errorLogs.Add($"{type}: {condition}\n{stackTrace}");
    }

    [UnityTest, Performance]
    [Timeout(1000000)]
    public IEnumerator SteadyStateTest()
    {
        var enemyPrefab = Resources.Load<GameObject>(enemyPrefabPath);
        Assert.IsNotNull(enemyPrefab, "Enemy prefab should be available");

        for (int i = 0; i < 100; i++)
        {
            Vector3 pos = new Vector3(Random.Range(-10, 10), 0, Random.Range(-10, 10));
            NavMeshHit hit;
            if (NavMesh.SamplePosition(pos, out hit, 2.0f, NavMesh.AllAreas))
            {
                spawnedEnemies.Add(Object.Instantiate(enemyPrefab, hit.position, Quaternion.identity));
            }
        }

        using (Measure.Frames().WarmupCount(10).MeasurementCount(20).Scope())
        using (Measure.Scope("MemoryUsage"))
        {
            float duration = 60f, elapsed = 0f;
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
                Measure.Custom(new SampleGroup("MemoryUsage", SampleUnit.Megabyte), Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f));
                yield return new WaitForSeconds(0.1f);
                elapsed += 0.1f;
            }
        }

        var fpsGroup = PerformanceTest.GetSampleGroup("FrameTime") ?? PerformanceTest.GetSampleGroup("Time"); ;
        Assert.NotNull(fpsGroup, "FrameTime sample group not found");
        Assert.LessOrEqual(fpsGroup.Median, 33.33f, $"Median frame time {fpsGroup.Median:F2} ms exceeds 33.33ms (30 FPS)");
        Assert.IsEmpty(errorLogs, $"Errors detected: {string.Join("\n", errorLogs)}");
        yield return null;
    }

    [UnityTest, Performance]
    [Timeout(1000000)]
    public IEnumerator RampUpTest()
    {
        var enemyPrefab = Resources.Load<GameObject>(enemyPrefabPath);
        Assert.IsNotNull(enemyPrefab, "Enemy prefab should be available");

        using (Measure.Frames().WarmupCount(10).MeasurementCount(20).Scope())
        using (Measure.Scope("MemoryUsage"))
        {
            float duration = 60f;
            float elapsed = 0f;
            int maxEnemies = 50;
            int enemiesPerStep = 10;
            while (elapsed < duration)
            {
                if (elapsed % 10f < 0.1f && spawnedEnemies.Count < maxEnemies)
                {
                    for (int i = 0; i < enemiesPerStep; i++)
                    {
                        Vector3 pos = new Vector3(Random.Range(-10, 10), 0, Random.Range(-10, 10));
                        NavMeshHit hit;
                        if (NavMesh.SamplePosition(pos, out hit, 2.0f, NavMesh.AllAreas))
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
                Measure.Custom(new SampleGroup("MemoryUsage", SampleUnit.Megabyte), Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f));
                yield return new WaitForSeconds(0.1f);
                elapsed += 0.1f;
            }
        }

        var fpsGroup = PerformanceTest.GetSampleGroup("FrameTime") ?? PerformanceTest.GetSampleGroup("Time"); ;
        Assert.NotNull(fpsGroup, "FrameTime sample group not found");
        Assert.LessOrEqual(fpsGroup.Median, 33.33f, $"Median frame time {fpsGroup.Median:F2} ms exceeds 33.33ms (30 FPS)");
        Assert.IsEmpty(errorLogs, $"Errors detected: {string.Join("\n", errorLogs)}");
        yield return null;
    }

    [UnityTest, Performance]
    [Timeout(1000000)]
    public IEnumerator ConcurrentEnemyChaseTest()
    {
        var enemyPrefab = Resources.Load<GameObject>(enemyPrefabPath);
        Assert.IsNotNull(enemyPrefab, "Enemy prefab not found");

        for (int i = 0; i < 50; i++)
        {
            Vector3 pos = new Vector3(Random.Range(-10, 10), 0, Random.Range(-10, 10));
            NavMeshHit hit;
            if (NavMesh.SamplePosition(pos, out hit, 2.0f, NavMesh.AllAreas)) 
            {
                spawnedEnemies.Add(Object.Instantiate(enemyPrefab, hit.position, Quaternion.identity));
            }
        }

        yield return null;

        foreach (var enemy in spawnedEnemies)
        {
            var ai = enemy.GetComponent<EnemyAI>();
            if (ai != null)
            {
                ai.OnPlayerShot(playerGameObject.transform.position);
            }
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
                Measure.Custom(new SampleGroup("MemoryUsage", SampleUnit.Megabyte), Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f));
                yield return new WaitForSeconds(0.1f);
                elapsed += 0.1f;
            }
        }

        var fpsGroup = PerformanceTest.GetSampleGroup("FrameTime") ?? PerformanceTest.GetSampleGroup("Time");
        Assert.NotNull(fpsGroup, "FrameTime sample group not found");
        Assert.LessOrEqual(fpsGroup.Median, 33.33f, $"Median frame time {fpsGroup.Median:F2} ms exceeds 33.33ms (30 FPS)");
        Assert.IsEmpty(errorLogs, $"Errors detected: {string.Join("\n", errorLogs)}");
        yield return null;
    }

    [UnityTest, Performance]
    [Timeout(1000000)]
    public IEnumerator MainGameSceneLoadTime()
    {
        const string sceneName = "Game";

        using (Measure.Scope("SceneLoadTime"))
        {
            var asyncOp = SceneManager.LoadSceneAsync(sceneName);
            while (!asyncOp.isDone)
            {
                yield return null;
            }
        }

        var group = PerformanceTest.GetSampleGroup("SceneLoadTime");
        Assert.NotNull(group, "SceneLoadTime sample group not found");
        Assert.LessOrEqual(group.Median, 3.0f, $"Median scene load time {group.Median:F2} seconds exceeds 3 seconds");

        yield return null;
    }
}