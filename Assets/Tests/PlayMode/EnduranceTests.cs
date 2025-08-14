//using System.Collections;
//using System.Collections.Generic;
//using System.Linq;
//using NUnit.Framework;
//using Unity.PerformanceTesting;
//using UnityEngine;
//using UnityEngine.AI;
//using UnityEngine.Profiling;
//using UnityEngine.SceneManagement;
//using UnityEngine.TestTools;

///// <summary>
///// Here I had a better system. Across all Performance Tests I think I'm pretty consistent with what I put in Setup/Teardown and how I structure my tests. These are the ones that work best I think.
///// </summary>
//[Category("Endurance")]
//public class EnduranceTests
//{
//    private List<GameObject> spawnedEnemies = new();
//    private List<GameObject> spawnedProjectiles = new();
//    private List<GameObject> spawnedCollectibles = new();
//    private GameObject playerGameObject;
//    private List<string> errorLogs = new();

//    [UnitySetUp]
//    public IEnumerator Setup()
//    {
//        SceneManager.LoadScene("PerformanceTestScene");
//        yield return null;

//        errorLogs.Clear();
//        Application.logMessageReceived += OnLogMessageReceived;

//        var playerPrefab = Resources.Load<GameObject>("Player");
//        Assert.IsNotNull(playerPrefab);
//        playerGameObject = Object.Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);

//        spawnedEnemies.Clear();
//        spawnedProjectiles.Clear();
//        spawnedCollectibles.Clear();

//        yield return null;
//    }

//    [UnityTearDown]
//    public IEnumerator Teardown()
//    {
//        Application.logMessageReceived -= OnLogMessageReceived;

//        if (playerGameObject) Object.DestroyImmediate(playerGameObject);
//        foreach (var gameObject in spawnedEnemies) if (gameObject) Object.DestroyImmediate(gameObject);
//        foreach (var gameObject in spawnedProjectiles) if (gameObject) Object.DestroyImmediate(gameObject);
//        foreach (var gameObject in spawnedCollectibles) if (gameObject) Object.DestroyImmediate(gameObject);

//        spawnedEnemies.Clear();
//        spawnedProjectiles.Clear();
//        spawnedCollectibles.Clear();

//        yield return null;
//    }

//    void OnLogMessageReceived(string condition, string stackTrace, LogType type)
//    {
//        if (type == LogType.Error || type == LogType.Exception)
//        {
//            errorLogs.Add($"{type}: {condition}\n{stackTrace}");
//        }
//    }

//    /// <summary>
//    /// Not emulating gameplay well enough
//    /// </summary>
//    /// <returns></returns>
//    [UnityTest, Performance]
//    [Timeout(1000000)]
//    public IEnumerator GeneralEnduranceTest()
//    {
//        int numEnemies = 50;
//        int numProjectiles = 20;
//        int numCollectibles = 10;

//        var enemyPrefab = Resources.Load<GameObject>("EnemyAI");
//        var bulletPrefab = Resources.Load<GameObject>("Bullet");
//        var collectiblePrefab = Resources.Load<GameObject>("RedRuby");

//        for (int i = 0; i < numEnemies; i++)
//        {
//            Vector3 pos = new Vector3(Random.Range(-20, 20), 0, Random.Range(-20, 20));
//            NavMeshHit hit;
//            if (NavMesh.SamplePosition(pos, out hit, 2.0f, NavMesh.AllAreas))
//            {
//                spawnedEnemies.Add(Object.Instantiate(enemyPrefab, hit.position, Quaternion.identity));
//            }
//        }
//        for (int i = 0; i < numProjectiles; i++)
//        {
//            spawnedProjectiles.Add(Object.Instantiate(bulletPrefab, Random.insideUnitSphere * 10f, Quaternion.identity));
//        }
//        for (int i = 0; i < numCollectibles; i++)
//        {
//            spawnedCollectibles.Add(Object.Instantiate(collectiblePrefab, Random.insideUnitSphere * 10f, Quaternion.identity));
//        }

//        float duration = 600f;
//        float elapsed = 0f;
//        List<float> fpsSamples = new();
//        List<float> memorySamples = new();

//        while (elapsed < duration)
//        {
//            yield return new WaitForSeconds(1f);
//            fpsSamples.Add(1.0f / Time.deltaTime);
//            memorySamples.Add(Profiler.GetTotalAllocatedMemoryLong() / (1024.0f * 1024.0f));
//            elapsed += 1f;
//        }

//        Debug.Log($"GeneralEnduranceTest: Errors {errorLogs.Count} noted");

//        yield return null;
//    }

//    /// <summary>
//    /// Not emulating gameplay well enough. First test seems like this and the 4th? Like where is the difference
//    /// </summary>
//    /// <returns></returns>
//    [UnityTest, Performance]
//    [Timeout(1000000)]
//    public IEnumerator MemoryLeakTest()
//    {
//        int numEnemies = 50;
//        var enemyPrefab = Resources.Load<GameObject>("EnemyAI");
//        for (int i = 0; i < numEnemies; i++)
//        {
//            Vector3 pos = new Vector3(Random.Range(-20, 20), 0, Random.Range(-20, 20));
//            NavMeshHit hit;
//            if (NavMesh.SamplePosition(pos, out hit, 2.0f, NavMesh.AllAreas))
//            {
//                spawnedEnemies.Add(Object.Instantiate(enemyPrefab, hit.position, Quaternion.identity));
//            }
//        }

//        float duration = 600f;
//        float elapsed = 0f;
//        List<float> memorySamples = new();

//        while (elapsed < duration)
//        {
//            yield return new WaitForSeconds(1f);
//            memorySamples.Add(Profiler.GetTotalAllocatedMemoryLong() / (1024.0f * 1024.0f));
//            elapsed += 1f;

//            if (Random.value < 0.02f)
//            {
//                int index = Random.Range(0, spawnedEnemies.Count);
//                Object.Destroy(spawnedEnemies[index]);
//                Vector3 randomPos = Random.insideUnitSphere * 20f;
//                randomPos.y = 0;

//                NavMeshHit hit;
//                if (NavMesh.SamplePosition(randomPos, out hit, 2.0f, NavMesh.AllAreas))
//                {
//                    spawnedEnemies[index] = Object.Instantiate(enemyPrefab, hit.position, Quaternion.identity);
//                }
//            }
//        }

//        float delta = memorySamples.Last() - memorySamples.First();
//        Debug.Log($"MemoryLeakTest: Start: {memorySamples.First():F2} MB, End: {memorySamples.Last():F2} MB, Delta: {delta:F2} MB");
//        if (delta > 10)
//        {
//            Debug.LogWarning("Potential Memory Leak detected");
//        }

//        yield return null;
//    }

//    /// <summary>
//    /// Seems pretty irrelevant for this scope
//    /// </summary>
//    /// <returns></returns>
//    [UnityTest, Performance]
//    [Timeout(1000000)]
//    public IEnumerator ResourceLeakTest()
//    {
//        List<Texture2D> textures = new();
//        int loops = 1000;

//        for (int i = 0; i < loops; i++)
//        {
//            Texture2D texture = new Texture2D(512, 512);
//            textures.Add(texture);
//            if (i % 10 == 0)
//                yield return null;
//        }

//        foreach (var texture in textures)
//            Object.Destroy(texture);

//        float memory = Profiler.GetTotalAllocatedMemoryLong() / (1024.0f * 1024.0f);
//        Debug.Log($"ResourceLeakTEst created/destroyed {loops} textures. Mem: {memory:F2} MB");
//        yield return null;
//    }

//    /// <summary>
//    /// Yeah this is just 1:1 the same as the first? Maybe research where the difference is between 1) and 2/4. Like yeah, we do 
//    /// avg and the whole degredation thing but the test itself is pretty much identical?
//    /// </summary>
//    /// <returns></returns>
//    [UnityTest, Performance]
//    [Timeout(1000000)]
//    public IEnumerator PerformanceDegredationTest()
//    {
//        int numEnemies = 50;
//        var enemyPrefab = Resources.Load<GameObject>("EnemyAI");
//        for (int i = 0; i < numEnemies; i++)
//        {
//            Vector3 pos = new Vector3(Random.Range(-20, 20), 0, Random.Range(-20, 20));
//            NavMeshHit hit;
//            if (NavMesh.SamplePosition(pos, out hit, 2.0f, NavMesh.AllAreas))
//            {
//                spawnedEnemies.Add(Object.Instantiate(enemyPrefab, hit.position, Quaternion.identity));
//            }
//        }

//        float duration = 600f;
//        float elapsed = 0f;
//        List<float> fpsSamples = new();

//        while (elapsed < duration)
//        {
//            yield return new WaitForSeconds(1f);
//            fpsSamples.Add(1.0f / Time.deltaTime);
//            elapsed += 1f;
//        }

//        float startAvg = fpsSamples.Take(60).Average();
//        float endAvg = fpsSamples.Skip(fpsSamples.Count - 60).Average();

//        Debug.Log($"PerformanceDegredation: Start FPS: {startAvg:F2}, End: {endAvg:F2}");
//        if (endAvg < startAvg * 0.75)
//        {
//            Debug.LogWarning("Performance degredation detected");
//        }

//        yield return null;
//    }
//}
