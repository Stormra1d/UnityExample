using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Users;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

/// <summary>
/// Input Simulation proved difficult and required changing to using InputActions for Input control.
/// </summary>
public class MenuUITests : BasePlayModeTest
{
    private readonly InputTestFixture inputFixture = new InputTestFixture();
    private Keyboard testKeyboard;
    private Mouse testMouse;

    [UnitySetUp]
    public override IEnumerator BaseSetUp()
    {
        yield return base.BaseSetUp();

        inputFixture.Setup();

        yield return null;
        yield return null;
    }

    [UnityTearDown]
    public override IEnumerator BaseTearDown()
    {
        yield return base.BaseTearDown();

        inputFixture.TearDown();

        yield return null;
    }

    [UnityTest]
    public IEnumerator MainMenu_LoadsGameScene_WhenStartGameButtonIsClicked()
    {
        yield return LoadTestScene("TestMainMenu", LoadSceneMode.Single);

        yield return null;
        yield return null;

        var startButton = GameObject.Find("Start")?.GetComponent<Button>();
        Assert.IsNotNull(startButton, "StartGameButton should be in the Test Scene");
        startButton.onClick.Invoke();

        yield return new WaitForSeconds(0.2f);

        Assert.AreEqual("Game", SceneManager.GetActiveScene().name, "Should have loaded the TestGame Scene");
    }

    [UnityTest]
    public IEnumerator Game_PausesWithEscapeAndResumesWithResumeButton()
    {
        yield return LoadTestScene("TestGame", LoadSceneMode.Single);

        yield return null;
        yield return null;

        yield return SetupInputForTest();

        var pauseMenu = Object.FindAnyObjectByType<PauseMenuManager>();
        Assert.IsNotNull(pauseMenu, "PauseMenuManager should be present");

        yield return TriggerKeyPressSync(Key.Escape, "Escape");

        yield return WaitForCondition(() => pauseMenu.pauseMenuUI.activeSelf || Time.timeScale == 0f, 1.0f, "Pause to activate");

        Assert.IsTrue(pauseMenu.pauseMenuUI.activeSelf, "Pause Menu UI should be active after ESC");
        Assert.AreEqual(0f, Time.timeScale, "Time should be stopped when paused");

        yield return ClickResumeButton();

        yield return WaitForCondition(() => !pauseMenu.pauseMenuUI.activeSelf && Time.timeScale == 1f, 1.0f, "Resume to complete");

        Assert.IsFalse(pauseMenu.pauseMenuUI.activeSelf, "Pause Menu UI should be inactive after unpausing");
        Assert.AreEqual(1f, Time.timeScale, "Time should be resumed after unpausing");
    }

    [UnityTest]
    public IEnumerator Game_PausesWithPAndQuitsWithQuitButton()
    {
        yield return LoadTestScene("TestGame", LoadSceneMode.Single);

        yield return null;
        yield return null;

        yield return SetupInputForTest();

        var pauseMenu = Object.FindAnyObjectByType<PauseMenuManager>();
        Assert.IsNotNull(pauseMenu, "PauseMenuManager should be present");

        yield return TriggerKeyPressSync(Key.P, "P");

        yield return WaitForCondition(() => pauseMenu.pauseMenuUI.activeSelf || Time.timeScale == 0f, 1.0f, "Pause to activate");

        Assert.IsTrue(pauseMenu.pauseMenuUI.activeSelf, "Pause Menu UI should be active after P");
        Assert.AreEqual(0f, Time.timeScale, "Time should be stopped when paused");

        var quitToMenuButton = GameObject.Find("Menu Button")?.GetComponent<Button>();
        Assert.IsNotNull(quitToMenuButton, "Quit to Menu Button should be present in the scene");

        quitToMenuButton.onClick.Invoke();
        yield return new WaitForSeconds(0.3f);

        Assert.AreEqual("MainMenu", SceneManager.GetActiveScene().name, "MainMenu Scene should be loaded after quitting");
    }

    private IEnumerator SetupInputForTest()
    {
        var playerInput = Object.FindAnyObjectByType<PlayerInput>();
        Assert.IsNotNull(playerInput, "PlayerInput not found");

        playerInput.user.UnpairDevices();

        testKeyboard = InputSystem.AddDevice<Keyboard>();
        testMouse = InputSystem.AddDevice<Mouse>();

        InputUser.PerformPairingWithDevice(testKeyboard, playerInput.user);
        InputUser.PerformPairingWithDevice(testMouse, playerInput.user);

        playerInput.SwitchCurrentControlScheme("KeyboardMouse", testKeyboard, testMouse);

        if (playerInput.actions.actionMaps.Any(map => map.name == "Player"))
        {
            playerInput.SwitchCurrentActionMap("Player");
        }

        yield return null;
        yield return null;
    }

    private IEnumerator TriggerKeyPressSync(Key key, string keyName)
    {
        Debug.Log($"Triggering {keyName} key press");

        var testKeyboard = InputSystem.devices.OfType<Keyboard>().FirstOrDefault();
        Assert.IsNotNull(testKeyboard, "Test keyboard not found");

        inputFixture.Press(testKeyboard[key]);

        yield return null;

        inputFixture.Release(testKeyboard[key]);

        yield return null;
    }

    private IEnumerator ClickResumeButton()
    {
        var resumeButton = GameObject.Find("Resume Button")?.GetComponent<Button>();
        Assert.IsNotNull(resumeButton, "Resume Button should be present");

        resumeButton.onClick.Invoke();
        yield return new WaitForSeconds(0.1f);
    }

    private IEnumerator WaitForCondition(System.Func<bool> condition, float timeout, string description)
    {
        float elapsedTime = 0f;
        while (!condition() && elapsedTime < timeout)
        {
            yield return null;
            elapsedTime += Time.unscaledDeltaTime;
        }
    }
}
