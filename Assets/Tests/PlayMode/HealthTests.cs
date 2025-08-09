using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Actor Tests, not Unit Tests. First tests I wrote, issues with consistency and setup.
/// </summary>
public class HealthTests
{
    GameObject gameObject;
    Health health;

    [SetUp]
    public void Setup()
    {
        gameObject = new GameObject();
        health = gameObject.AddComponent<Health>();
    }

    [TearDown]
    public void Teardown()
    {
        Object.DestroyImmediate(gameObject);

        Time.timeScale = 1.0f;
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
        health.TakeDamage(health.maxHealth);
        yield return null;

        Assert.IsNull(health.gameObject);
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
