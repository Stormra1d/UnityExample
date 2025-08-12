using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Actor Tests, not Unit Tests. First tests I wrote, issues with consistency and setup.
/// </summary>
public class HealthTests : BasePlayModeTest
{
    GameObject gameObject;
    Health health;

    [UnitySetUp]
    public void Setup()
    {
        gameObject = new GameObject();
        health = gameObject.AddComponent<Health>();
    }

    [UnityTearDown]
    public void Teardown()
    {
        Object.DestroyImmediate(gameObject);
    }

    [UnityTest]
    public IEnumerator TakeDamage_DecreasesHealth_WhenDamageIsApplied()
    {
        float startHP = health.CurrentHealth;
        health.TakeDamage(10);
        yield return null;

        Assert.AreEqual(startHP - 10, health.CurrentHealth);
    }

    [UnityTest]
    public IEnumerator Heal_ClampsToMax_WhenHealthIsAdded()
    {
        health.TakeDamage(50);
        health.Heal(1000);
        yield return null;

        Assert.AreEqual(health.maxHealth, health.CurrentHealth);
    }

    [UnityTest]
    public IEnumerator Die_DestroysGameObject_WhenEnemyDies()
    {
        var gameObject = health.gameObject;
        health.TakeDamage(health.maxHealth);
        yield return null;

        Assert.IsTrue(gameObject == null);
    }

    [UnityTest]
    public IEnumerator OnDeathEvent_IsInvoked_WhenDies()
    {
        bool eventFired = false;
        health.OnDeath += () => eventFired = true;

        health.TakeDamage(health.maxHealth);
        yield return null;

        Assert.IsTrue(eventFired);
    }
}
