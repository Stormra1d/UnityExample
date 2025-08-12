using System.Collections;
using NUnit.Framework;
using UnityEngine;
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
        if (!SceneManager.GetSceneByName("GameAITest").isLoaded)
        {
            SceneManager.LoadScene("GameAITest");
        }

        yield return new WaitUntil(() => SceneManager.GetActiveScene().name == "GameAITest");
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

        Vector3 direction = player.transform.position - smartEnemy.transform.position;
        Quaternion lookRotation = Quaternion.LookRotation(direction);

        smartEnemy.transform.rotation = lookRotation;

        yield return null;

        player.transform.position = smartEnemy.transform.position + smartEnemy.transform.forward * 2f;
        yield return new WaitForSeconds(1f);
        yield return null;

        Assert.AreEqual(State.Chase, ai.currentState, "Enemy should be chasing the player");

        player.transform.position = new Vector3(-100, 1000000, 50);
        yield return new WaitForSeconds(ai.forgetDuration + 1.0f);
        yield return null;

        Assert.AreEqual(State.Chase, ai.currentState, "Enemy should still be Chasing");
        Assert.IsTrue(ai.isGoingToLastKnownPosition, "Enemy should be on way to last known position");

        yield return new WaitUntil(() => !ai.isGoingToLastKnownPosition);
        yield return null;

        Assert.IsFalse(ai.isGoingToLastKnownPosition, "Enemy should have reached last known position");
        Assert.AreEqual(State.Wait, ai.currentState, "Enemy should be waiting at last known position");

        yield return new WaitForSeconds(2.5f);

        Assert.AreEqual(State.Patrol, ai.currentState, "Enemy should be on patrol");
    }
}
