using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Linq;
using UnityEngine.SceneManagement;

/// <summary>
/// These, as mentioned in the VSC file, I had to adjust code as I did some TDD. I thought of the test for Collectibles not spawning in Geo and noticed it can happen and fixed it.
/// </summary>
public class SpawnValidationTests
{
    private const string sceneName = "Game";
    private GameObject spawnerInstance;

    [UnitySetUp]
    public IEnumerator Setup()
    {
        if (!SceneManager.GetSceneByName(sceneName).isLoaded)
        {
            SceneManager.LoadScene(sceneName);
        }

        yield return new WaitForSeconds(0.1f);
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        foreach (var collectible in GameObject.FindGameObjectsWithTag("Collectible"))
        {
            Object.DestroyImmediate(collectible);
        }

        if (spawnerInstance != null)
        {
            Object.DestroyImmediate(spawnerInstance);
        }

        yield return null;
    }

    [UnityTest]
    public IEnumerator Collectible_Spawns_OnNavMeshAndNotInsideGeometry()
    {
        yield return new WaitForSeconds(0.1f);

        foreach (var collectible in GameObject.FindGameObjectsWithTag("Collectible"))
        {
            Assert.IsTrue(UnityEngine.AI.NavMesh.SamplePosition(
                collectible.transform.position, out var hit, 0.5f, UnityEngine.AI.NavMesh.AllAreas),
                "All collectibles should be on the NavMesh");

            var overlaps = Physics.OverlapSphere(collectible.transform.position, 0.3f, LayerMask.GetMask("Movable"));
            Assert.IsTrue(overlaps.Length == 0, "No Collectibles should be spawning inside geometry");
        }
    }

    [UnityTest]
    public IEnumerator Collectibles_Not_Stacked()
    {
        yield return new WaitForSeconds(0.1f);

        var collectibles = GameObject.FindGameObjectsWithTag("Collectible");
        for (int i = 0; i < collectibles.Length; i++)
        {
            for (int j = i + 1; j < collectibles.Length; j++)
            {
                float distance = Vector3.Distance(collectibles[i].transform.position, collectibles[j].transform.position);
                Assert.Greater(distance, 0.5f, "Collectibles should not spawn inside each other");
            }
        }
    }

    //TODO: On top of platform?
    //TODO: Are these actually valid tests? Since we only try 10 times for an event to happen. This test passing isn't very indicative since it still could happen. This is a terrible test!
}
