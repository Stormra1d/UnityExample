using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Integration Test. Should work on gettin gSetup/Teardown standardized. These also use the default Setup/Teardown rather than UnityTeardown and Setup. 
/// Idk about the rest.
/// </summary>
public class HealthPickupTests : BasePlayModeTest
{
    GameObject playerGameObject;
    Health playerHealth;

    GameObject canvasGO;
    PlayerHealthUI healthUI;
    TextMeshProUGUI hpText;
    GameObject healthPackGameObject;
    HealthPack healthPack;

    [UnitySetUp]
    public IEnumerator Setup()
    {
        playerGameObject = new GameObject("Player");
        playerGameObject.tag = "Player";
        playerHealth = playerGameObject.AddComponent<Health>();
        playerHealth.maxHealth = 100f;

        canvasGO = new GameObject("Canvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        var textGO = new GameObject("HPText");
        textGO.transform.SetParent(canvasGO.transform, false);
        hpText = textGO.AddComponent<TMPro.TextMeshProUGUI>();

        healthUI = canvasGO.AddComponent<PlayerHealthUI>();
        healthUI.playerHealth = playerHealth;
        healthUI.hpText = hpText;

        healthPackGameObject = new GameObject("HealthPack");
        healthPackGameObject.AddComponent<BoxCollider>().isTrigger = true;
        healthPack = healthPackGameObject.AddComponent<HealthPack>();
        healthPack.healAmount = 25f;

        yield return null;
    }

    [UnityTearDown]
    public IEnumerator Teardown()
    {
        Object.DestroyImmediate(playerGameObject);
        Object.DestroyImmediate(healthPackGameObject);
        Object.DestroyImmediate(canvasGO);

        yield return null;
    }

    [UnityTest]
    public IEnumerator CollectingHealthPack_HealsPlayerAndUpdatesUI_WhenCollected()
    {
        playerHealth.TakeDamage(50f);
        yield return null;

        Assert.AreEqual(50f, playerHealth.CurrentHealth);

        var playerCollider = playerGameObject.AddComponent<BoxCollider>();
        healthPack.OnTriggerEnter(playerCollider);

        yield return null;

        Assert.AreEqual(75f, playerHealth.CurrentHealth);
        Assert.IsTrue(healthPack == null || healthPackGameObject == null);
        healthUI.Update();

        Assert.AreEqual("HP: 75", hpText.text);
    }

}
