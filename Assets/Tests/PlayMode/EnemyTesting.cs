using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// This was rough. I had to do a bunch of testing to prevent flakiness. Also some adjustments were necessary to fix tests failing (TDD).
/// </summary>
public class ZEnemyTesting : BasePlayModeTest
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
    public IEnumerator Enemy_ChangesState_OnPlayerSight()
    {
        player = Object.Instantiate(Resources.Load<GameObject>("Player"), playerPos, Quaternion.identity);

        smartEnemy = Object.Instantiate(Resources.Load<GameObject>("EnemyAI"), enemyPos, Quaternion.identity);

        var ai = smartEnemy.GetComponent<EnemyAI>();
        ai.patrolOrigin = enemyPos;

        ai.playerLayer = LayerMask.GetMask("Player");
        ai.obstacleMask = LayerMask.GetMask("Movable");

        var agent = smartEnemy.GetComponent<NavMeshAgent>();
        yield return null;

        agent.Warp(agent.transform.position);
        yield return null;

        smartEnemy.transform.rotation = Quaternion.LookRotation(player.transform.position - smartEnemy.transform.position);
        yield return new WaitForSeconds(0.25f);

        Assert.AreEqual(State.Chase, ai.currentState, "Enemy should be chasing the player");

        var wall = MakeWall(new Vector3(7.5f, 1.5f, 0), new Vector3(1f, 3f, 4f));

        player.transform.position = new Vector3(10f, 0f, 0f);
        yield return null;

        yield return new WaitUntil(() => !ai.PlayerInSight());

        yield return WaitUntilOrTimeout(() => ai.isGoingToLastKnownPosition, 5f, "ai.isGoingToLastKnownPosition for 5s");
        Assert.IsTrue(ai.isGoingToLastKnownPosition, "Enemy should be on way to last known position");
        Assert.AreEqual(State.Chase, ai.currentState, "Enemy should still be Chasing while heading to LKP");

        yield return WaitUntilOrTimeout(() => !ai.isGoingToLastKnownPosition, 5f, "!ai.isGoingToLastKnownPosition for 5s");
        Assert.IsFalse(ai.isGoingToLastKnownPosition, "Enemy should have reached last known position");
        Assert.AreEqual(State.Wait, ai.currentState, "Enemy should be waiting at last known position");

        yield return WaitUntilOrTimeout(() => ai.currentState == State.Patrol, ai.forgetDuration + 3f, "ai.currentState == State.Patrol for 3s + forget duration");
        Assert.AreEqual(State.Patrol, ai.currentState, "Enemy should be on patrol");
    }

    private GameObject MakeWall(Vector3 center, Vector3 size)
    {
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.transform.position = center;
        wall.transform.localScale = size;
        var col = wall.GetComponent<Collider>();
        col.gameObject.layer = LayerMask.NameToLayer("Movable");
        return wall;
    }

    private static IEnumerator WaitUntilOrTimeout(System.Func<bool> predicate, float timeoutSeconds, string message)
    {
        float start = Time.time;
        while (Time.time - start < timeoutSeconds)
        {
            if (predicate()) yield break;
            yield return null;
        }
        Assert.Fail($"Timeout after {timeoutSeconds:F2}s waiting for condition: {predicate.Method.Name}: {message}");
    }
}
