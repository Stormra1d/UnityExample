using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using Unity.PerformanceTesting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class SpikeTests
{
    private readonly List<GameObject> spawnedEnemies = new();
    private readonly List<GameObject> spawnedProjectiles = new();
    private readonly List<GameObject> spawnedCollectibles = new();
    private GameObject playerGameObject;

    [UnitySetUp]
    public IEnumerator Setup()
    {
        SceneManager.LoadScene("PerformanceTestScene");
        while (!SceneManager.GetActiveScene().isLoaded)
            yield return null;

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
        yield return null;
        yield return new WaitForSeconds(0.1f);

        try
        {
            DoCleanupNoYield();
        }
        catch (System.Exception e)
        {
            Assert.Fail("Teardown exception: " + e);
        }

        yield return null;
    }

    private void DoCleanupNoYield()
    {
        if (playerGameObject) Object.Destroy(playerGameObject);

        DestroyList(spawnedEnemies);
        DestroyList(spawnedProjectiles);
        DestroyList(spawnedCollectibles);

        spawnedEnemies.Clear();
        spawnedProjectiles.Clear();
        spawnedCollectibles.Clear();
    }

    private static void DestroyList(List<GameObject> list)
    {
        if (list == null) return;
        foreach (var go in list)
            if (go) Object.Destroy(go);
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

        LogAssert.NoUnexpectedReceived();
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

        DestroyList(spawnedEnemies);
        DestroyList(spawnedProjectiles);
        DestroyList(spawnedCollectibles);
        spawnedEnemies.Clear();
        spawnedProjectiles.Clear();
        spawnedCollectibles.Clear();
        yield return null;

        for (int i = 0; i < 60; i++)
        {
            yield return null;
            Measure.Custom("NegativeSpike_FPS", 1.0f / Time.deltaTime);
            Measure.Custom("NegativeSpike_MemoryMB", Profiler.GetTotalAllocatedMemoryLong() / (1024.0f * 1024.0f));
        }

        LogAssert.NoUnexpectedReceived();
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

            DestroyList(spawnedEnemies);
            DestroyList(spawnedProjectiles);
            DestroyList(spawnedCollectibles);
            spawnedEnemies.Clear();
            spawnedProjectiles.Clear();
            spawnedCollectibles.Clear();

            yield return null;
            yield return new WaitForSeconds(1f);

            Measure.Custom("RepeatedSpike_FPS", 1.0f / Time.deltaTime);
            Measure.Custom("RepeatedSpike_MemoryMB", Profiler.GetTotalAllocatedMemoryLong() / (1024.0f * 1024.0f));
        }

        LogAssert.NoUnexpectedReceived();
        yield return null;
    }
}
