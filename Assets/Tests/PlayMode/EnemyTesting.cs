using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// This was rough. I had to do a bunch of testing to prevent flakiness. Also some adjustments were necessary to fix tests failing (TDD).
/// </summary>
public class EnemyTesting : BasePlayModeTest
{
    private GameObject player;
    private GameObject smartEnemy;
    private GameObject dumbEnemy;

    Vector3 playerPos = new Vector3(0, 0, 0);
    Vector3 enemyPos = new Vector3(5, 0, 0);

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        NavMesh.RemoveAllNavMeshData();

        if (!SceneManager.GetSceneByName("GameAITest").isLoaded)
            SceneManager.LoadScene("GameAITest");

        yield return new WaitUntil(() => SceneManager.GetActiveScene().name == "GameAITest");
        yield return new WaitUntil(() => NavMesh.CalculateTriangulation().vertices.Length > 0);

        yield return null;
        foreach (var spawner in Object.FindObjectsByType<CollectibleSpawner>(FindObjectsSortMode.None))
            spawner.gameObject.SetActive(false);

        yield return null;
    }

    [UnityTearDown]
    public IEnumerator Teardown()
    {
        if (player) Object.Destroy(player);
        if (smartEnemy) Object.Destroy(smartEnemy);
        if (dumbEnemy) Object.Destroy(dumbEnemy);

        yield return null;
    }

    [UnityTest]
    public IEnumerator Enemy_Reaches_Player()
    {
        player = Object.Instantiate(Resources.Load<GameObject>("Player"), playerPos, Quaternion.identity);
        player.layer = LayerMask.NameToLayer("Player");
        if (player.tag != "Player") player.tag = "Player";

        dumbEnemy = Object.Instantiate(Resources.Load<GameObject>("Dumb AI Variant"), enemyPos, Quaternion.identity);
        var dumbEnemyAI = dumbEnemy.GetComponent<DumbEnemyAI>();

        yield return null;

        float expectedTime = Mathf.Clamp(Vector3.Distance(enemyPos, playerPos) / dumbEnemyAI.chaseSpeed, 1, 100);
        float timeout = expectedTime * 1.5f;
        float time = 0;

        while (Vector3.Distance(dumbEnemy.transform.position, player.transform.position) > 1.5f && time < timeout)
        {
            time += Time.deltaTime;
            yield return null;
        }

        Assert.LessOrEqual(Vector3.Distance(dumbEnemy.transform.position, player.transform.position), 1.5f, "Enemy should reach the player in time");
    }

    [UnityTest]
    public IEnumerator Enemy_Shot_Response_Test()
    {
        Vector3 testEnemyPos = new Vector3(0, 0, 0);
        Vector3 playerPos = new Vector3(0, 0, 100f);

        player = Object.Instantiate(Resources.Load<GameObject>("Player"), playerPos, Quaternion.identity);
        player.layer = LayerMask.NameToLayer("Player");
        if (player.tag != "Player") player.tag = "Player";

        GameObject weaponObj = Object.Instantiate(Resources.Load<GameObject>("Rifle"), player.transform);
        Weapon weapon = weaponObj.GetComponent<Weapon>();
        Assert.IsNotNull(weapon, "Rifle prefab must have a Weapon component");

        if (weapon.playerCamera == null)
        {
            GameObject cameraObj = new GameObject("TestCamera");
            cameraObj.transform.SetParent(player.transform);
            cameraObj.transform.localPosition = new Vector3(0, 1, 0);
            weapon.playerCamera = cameraObj.AddComponent<Camera>();
        }
        weapon.bulletPrefab = Resources.Load<GameObject>("Bullet");
        Assert.IsNotNull(weapon.bulletPrefab, "Bullet prefab must exist in Resources");
        weapon.bulletSpawn = weapon.transform;
        weapon.shootingDelay = 0.1f;
        weapon.bulletsInMag = weapon.magazineSize;

        smartEnemy = Object.Instantiate(Resources.Load<GameObject>("EnemyAI"), testEnemyPos, Quaternion.identity);
        var ai = smartEnemy.GetComponent<EnemyAI>();
        ai.testPlayer = player.transform;
        ai.patrolOrigin = testEnemyPos;
        ai.playerLayer = LayerMask.GetMask("Player");
        ai.obstacleMask = LayerMask.GetMask("Movable");

        yield return new WaitForSeconds(0.5f);

        Assert.AreEqual(State.Patrol, ai.currentState, "Enemy should start in Patrol");
        Assert.IsFalse(ai.isGoingToLastKnownPosition, "Enemy should not be going to LKP");

        yield return weapon.FireWeaponCoroutine();
        yield return new WaitForFixedUpdate();

        Assert.IsTrue(ai.isGoingToLastKnownPosition, "Enemy should start going to shot position");
        Assert.AreEqual(State.Chase, ai.currentState, "Enemy should switch to Chase after shot");

        float initialDistanceToShot = Vector3.Distance(smartEnemy.transform.position, ai.lastKnownPosition);
        yield return new WaitForSeconds(1f);
        float newDistanceToShot = Vector3.Distance(smartEnemy.transform.position, ai.lastKnownPosition);

        Assert.Less(newDistanceToShot, initialDistanceToShot, "Enemy should move closer to shot position");
    }
}
