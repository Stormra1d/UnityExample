using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

/// <summary>
/// Due to the nature of the project, the smoke test is pretty much a system test. Oh well.
/// Setup is a bit inconsistent like all my early tests. 
/// </summary>
public class SmokeTest : BasePlayModeTest
{
    GameObject playerGameObject;
    GameObject spawnerGameObject;
    GameObject weaponGameObject;
    GameObject healthPackPrefab;
    GameObject spawnedHealthPack;

    GameObject pauseMenuUI;
    GameObject mainUI;

    Weapon weapon;
    Health playerHealth;
    MiscSpawner miscSpawner;
    PauseMenuManager pauseMenuManager;

    NavMeshSurface navSurface;

    [SetUp]
    public void Setup()
    {
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.position = Vector3.zero;

        var navGameObject = new GameObject("NavMeshSurfaceGameObject");
        navSurface = navGameObject.AddComponent<NavMeshSurface>();
        navSurface.collectObjects = CollectObjects.All;
        navSurface.BuildNavMesh();

        playerGameObject = new GameObject("Player");
        playerHealth = playerGameObject.AddComponent<Health>();

        weaponGameObject = new GameObject("Weapon");
        weapon = weaponGameObject.AddComponent<Weapon>();
        weapon.magazineSize = 3;
        weapon.bulletsInMag = 3;

        healthPackPrefab = new GameObject("HealthPackPrefab");
        healthPackPrefab.AddComponent<BoxCollider>().isTrigger = true;
        healthPackPrefab.AddComponent<HealthPack>();

        spawnerGameObject = new GameObject("Spawner");
        spawnerGameObject.SetActive(false);
        miscSpawner = spawnerGameObject.AddComponent<MiscSpawner>();
        miscSpawner.pickups = new List<PickupConfig>
        {
            new PickupConfig
            {
                prefab = healthPackPrefab,
                type = PickupType.Health,
                spawnInterval = 0.1f
            }
        };
        spawnerGameObject.SetActive(true);

        pauseMenuUI = new GameObject("PauseMenuUI");
        mainUI = new GameObject("MainUI");
        pauseMenuUI.SetActive(false);
        mainUI.SetActive(true);

        var pauseManagerGameObject = new GameObject("PauseManager");
        pauseMenuManager = pauseManagerGameObject.AddComponent<PauseMenuManager>();
        pauseMenuManager.pauseMenuUI = pauseMenuUI;
        pauseMenuManager.mainUI = mainUI;
        pauseMenuManager.player = playerGameObject;
    }

    [TearDown]
    public void Teardown()
    {
        if (miscSpawner != null) miscSpawner.enabled = false;

        Object.DestroyImmediate(spawnerGameObject);
        Object.DestroyImmediate(healthPackPrefab);
        Object.DestroyImmediate(playerGameObject);
        Object.DestroyImmediate(weaponGameObject);
        Object.DestroyImmediate(pauseMenuUI);
        Object.DestroyImmediate(mainUI);
        if (navSurface != null) Object.DestroyImmediate(navSurface);
    }

    [UnityTest]
    public IEnumerator CoreGameSystems_SmokeTest()
    {
        Random.InitState(12345);

        yield return null;

        spawnedHealthPack = null;
        float deadline = Time.realtimeSinceStartup + 3f;
        while (Time.realtimeSinceStartup < deadline && spawnedHealthPack == null)
        {
            var hp = Object.FindFirstObjectByType<HealthPack>(FindObjectsInactive.Exclude);
            if (hp != null) spawnedHealthPack = hp.gameObject;
            yield return null;
        }
        Assert.IsNotNull(spawnedHealthPack, "Healthpack should be spawned");

        NavMeshHit hit;
        bool valid = NavMesh.SamplePosition(spawnedHealthPack.transform.position, out hit, 1f, NavMesh.AllAreas);
        Assert.IsTrue(valid, "Healthpack should be on the NavMesh");

        var playerCollider = playerGameObject.AddComponent<BoxCollider>();
        spawnedHealthPack.GetComponent<HealthPack>().OnTriggerEnter(playerCollider);

        float destroyDeadline = Time.realtimeSinceStartup + 1f;
        while (Time.realtimeSinceStartup < destroyDeadline && spawnedHealthPack != null && !spawnedHealthPack.Equals(null))
        {
            yield return null;
        }
        Assert.IsTrue(spawnedHealthPack == null || spawnedHealthPack.Equals(null), "HealthPack should have been collected");

        pauseMenuManager.PauseGame();
        Assert.IsTrue(pauseMenuUI.activeSelf, "Pause Menu should be active");
        Assert.IsFalse(mainUI.activeSelf, "Main UI should be inactive after pause");
        Assert.AreEqual(0f, Time.timeScale, "Game should be paused");

        pauseMenuManager.ResumeGame();
        Assert.IsFalse(pauseMenuUI.activeSelf, "Pause Menu should be inactive");
        Assert.IsTrue(mainUI.activeSelf, "Main UI should be active after unpause");
        Assert.AreEqual(1f, Time.timeScale, "Game should be running");

        yield return null;
    }

}
