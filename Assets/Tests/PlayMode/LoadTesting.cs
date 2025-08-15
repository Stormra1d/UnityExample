using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.PerformanceTesting;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Profiling;
using System.Diagnostics;

public class LoadTesting
{
    private GameObject playerGameObject;
    private readonly string playerPrefabPath = "Player";
    private readonly string enemyPrefabPath = "EnemyAI";
    private List<GameObject> spawnedEnemies = new();

    [UnitySetUp]
    public IEnumerator Setup()
    {
        SceneManager.LoadScene("PerformanceTestScene");
        yield return null;

        var playerPrefab = Resources.Load<GameObject>(playerPrefabPath);
        Assert.IsNotNull(playerPrefab, "Player prefab should be available");
        playerGameObject = GameObject.Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);

        spawnedEnemies.Clear();
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator Teardown()
    {
        if (playerGameObject) GameObject.DestroyImmediate(playerGameObject);
        foreach (var enemy in spawnedEnemies)
        {
            if (enemy)
            {
                GameObject.DestroyImmediate(enemy);
            }
        }
        spawnedEnemies.Clear();
        yield return null;
    }

    [UnityTest, Performance]
    public IEnumerator SteadyStateTest()
    {
        var enemyPrefab = Resources.Load<GameObject>(enemyPrefabPath);
        Assert.IsNotNull(enemyPrefab, "Enemy prefab should be available");

        for (int i = 0; i < 100; i++)
        {
            Vector3 pos = new Vector3(Random.Range(-10, 10), 0, Random.Range(-10, 10));
            var enemy = GameObject.Instantiate(enemyPrefab, pos, Quaternion.identity);
            spawnedEnemies.Add(enemy);
        }

        yield return new WaitForSeconds(2f);

        for (int i = 0; i < 120; i++)
        {
            yield return null;
            Measure.Custom("SteadyState_FPS", 1.0f / Time.deltaTime);
            Measure.Custom("SteadState_MemoryMB", Profiler.GetTotalAllocatedMemoryLong() / (1024.0f * 1024.0f));
        }

        yield return null;
    }

    [UnityTest, Performance]
    public IEnumerator RampUpTest()
    {
        var enemyPrefab = Resources.Load<GameObject>(enemyPrefabPath);
        Assert.IsNotNull(enemyPrefab, "Enemy prefab should be available");
        int currentCount = 0;

        for (int i = 0; i < 5; i++)
        {
            for (int j = 0; j < 10; j++)
            {
                Vector3 pos = new Vector3(Random.Range(-10, 10), 0, Random.Range(-10, 10));
                var enemy = GameObject.Instantiate(enemyPrefab, pos, Quaternion.identity);
                spawnedEnemies.Add(enemy);
                currentCount++;
            }

            yield return new WaitForSeconds(2f);

            for (int f = 0; f < 30; f++)
            {
                yield return null;
                Measure.Custom($"{currentCount}_Enemies_FPS", 1.0f / Time.deltaTime);
                Measure.Custom($"{currentCount}_Enemies_MemoryMB", Profiler.GetTotalAllocatedMemoryLong() / (1024.0f * 1024.0f));
            }
        }
    }

    [UnityTest, Performance]
    public IEnumerator ConcurrentEnemyChaseTest()
    {
        var enemyPrefab = Resources.Load<GameObject>(enemyPrefabPath);
        Assert.IsNotNull(enemyPrefab, "Enemy prefab should be available");

        for (int i = 0; i < 50; i++)
        {
            Vector3 pos = new Vector3(Random.Range(-10, 10), 0, Random.Range(-10, 10));
            var enemy = GameObject.Instantiate(enemyPrefab, pos, Quaternion.identity);
            spawnedEnemies.Add(enemy);
        }

        yield return new WaitForSeconds(1f);

        foreach (var enemy in spawnedEnemies)
        {
            var ai = enemy.GetComponent<EnemyAI>();
            if (ai != null)
            {
                ai.OnPlayerShot(playerGameObject.transform.position);
            }
        }

        yield return null;

        for (int i = 0; i < 10; i++)
        {
            var stopwatch = Stopwatch.StartNew();

            yield return null;

            stopwatch.Stop();

            double cpuTimeMs = stopwatch.Elapsed.TotalMilliseconds;
            Measure.Custom("ConcurrentChase_FPS", 1.0f / Time.deltaTime);
            Measure.Custom("ConcurrentChase_CPUTimeMS", cpuTimeMs);
            Measure.Custom($"ConcurrentChase_MemoryMB", Profiler.GetTotalAllocatedMemoryLong() / (1024.0f * 1024.0f));
        }
    }

    [UnityTest, Performance]
    public IEnumerator MainGameSceneLoadTime()
    {
        string sceneName = "Game";

        float startTime = Time.realtimeSinceStartup;
        var asyncOp = SceneManager.LoadSceneAsync(sceneName);
        while (!asyncOp.isDone)
        {
            yield return null;
        }
        float loadTime = Time.realtimeSinceStartup - startTime;
        Measure.Custom("SceneLoadTime_Sec", loadTime);
        UnityEngine.Debug.Log($"Scene: {sceneName} loaded in {loadTime:F2} secpnds");

        Assert.Less(loadTime, 3.0f, "Scene shouldn't be higher than 3 seconds");

        yield return null;
    }
}
