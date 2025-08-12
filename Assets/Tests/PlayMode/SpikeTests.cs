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

public class SpikeTests : BasePlayModeTest
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

    void OnLogMessageReceived(string condition, string stackTrace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception)
        {
            errorLogs.Add($"{type}: {condition}\n{stackTrace}");
        }
    }

    /// <summary>
    /// Is this too different than load tests etc? Like, is the SPIKE aspect here too different than to load/stress tests?
    /// </summary>
    /// <returns></returns>
    [UnityTest, Performance]
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
            spawnedProjectiles.Add(Object.Instantiate(bulletPrefab, Random.insideUnitSphere * 10f, Quaternion.identity));
        for (int i = 0; i < spikeCollectibles; i++)
            spawnedCollectibles.Add(Object.Instantiate(collectiblePrefab, Random.insideUnitSphere * 10f, Quaternion.identity));

        yield return null;

        for (int i = 0; i < 60; i++)
        {
            yield return null;
            Measure.Custom("PositiveSpike_FPS", 1.0f / Time.deltaTime);
            Measure.Custom("PositiveSpike_MemoryMB", Profiler.GetTotalAllocatedMemoryLong() / (1024.0f * 1024.0f));
        }

        Debug.Log($"PositiveSpikeTest: {errorLogs.Count} errors / exceptions captured");
        foreach (var error in errorLogs) Debug.LogError(error);

        yield return null;
    }

    [UnityTest, Performance]
    public IEnumerator NegativeSpikeTest()
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
            spawnedProjectiles.Add(Object.Instantiate(bulletPrefab, Random.insideUnitSphere * 10f, Quaternion.identity));
        for (int i = 0; i < spikeCollectibles; i++)
            spawnedCollectibles.Add(Object.Instantiate(collectiblePrefab, Random.insideUnitSphere * 10f, Quaternion.identity));

        yield return new WaitForSeconds(2f);

        foreach (var gameObject in spawnedEnemies) if (gameObject) Object.DestroyImmediate(gameObject);
        foreach (var gameObject in spawnedProjectiles) if (gameObject) Object.DestroyImmediate(gameObject);
        foreach (var gameObject in spawnedCollectibles) if (gameObject) Object.DestroyImmediate(gameObject);

        spawnedEnemies.Clear();
        spawnedProjectiles.Clear();
        spawnedCollectibles.Clear();

        yield return null;

        for (int i = 0; i < 60; i++)
        {
            yield return null;
            Measure.Custom("NegativeSpike", 1.0f / Time.deltaTime);
            Measure.Custom("NegativeSpike_MemoryMB", Profiler.GetTotalAllocatedMemoryLong() / (1024.0f * 1024.0f));
        }

        Debug.Log($"NegativeSpikeTest: {errorLogs.Count} errors / exceptions captured");
        foreach (var error in errorLogs) Debug.LogError(error);

        yield return null;
    }

    [UnityTest, Performance]
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
                spawnedProjectiles.Add(Object.Instantiate(bulletPrefab, Random.insideUnitSphere * 10f, Quaternion.identity));
            for (int i = 0; i < spikeCollectibles; i++)
                spawnedCollectibles.Add(Object.Instantiate(collectiblePrefab, Random.insideUnitSphere * 10f, Quaternion.identity));

            yield return new WaitForSeconds(1f);

            foreach (var gameObject in spawnedEnemies) if (gameObject) Object.DestroyImmediate(gameObject);
            foreach (var gameObject in spawnedProjectiles) if (gameObject) Object.DestroyImmediate(gameObject);
            foreach (var gameObject in spawnedCollectibles) if (gameObject) Object.DestroyImmediate(gameObject);

            spawnedEnemies.Clear();
            spawnedProjectiles.Clear();
            spawnedCollectibles.Clear();

            yield return new WaitForSeconds(1f);

            Measure.Custom("RepeatedSpike", 1.0f / Time.deltaTime);
            Measure.Custom("RepeatedSpike_MemoryMB", Profiler.GetTotalAllocatedMemoryLong() / (1024.0f * 1024.0f));
        }

        Debug.Log($"RepeatedSpikeTest: {errorLogs.Count} errors / exceptions captured");
        foreach (var error in errorLogs) Debug.LogError(error);

        yield return null;
    }
}
