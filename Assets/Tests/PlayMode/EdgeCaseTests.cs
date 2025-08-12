using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// This one felt really pointless to do. What should and shouldn't be happening is easy to detect in code. Actually, that's totally an issue. This test should SIMULATE taking damage and healing.
/// But even then, the game can very easily differentiate which was called first, by lines of code. Idk, this seems useless.
/// </summary>
public class EdgeCaseTests : BasePlayModeTest
{
    GameObject gameObject;
    Health health;

    [UnitySetUp]
    public IEnumerator Setup()
    {
        gameObject = new GameObject();
        health = gameObject.AddComponent<Health>();

        yield return null;
    }

    [UnityTearDown]
    public IEnumerator Teardown()
    {
        Object.DestroyImmediate(gameObject);

        yield return null;
    }

    [UnityTest]
    public IEnumerator Player_TakeDamageAndHeal_WorksSimultaneously()
    {
        health.TakeDamage(20);
        health.Heal(25);

        yield return null;

        Assert.AreEqual(100, health.CurrentHealth);
    }
}
