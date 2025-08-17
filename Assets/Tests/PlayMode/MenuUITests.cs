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
    private Keyboard testKeyboard;
    private Mouse testMouse;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        Time.timeScale = 1f;

        yield return null;
        yield return null;

        CleanupInputDevices();
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Time.timeScale = 1f;

        CleanupInputDevices();

        foreach (var device in InputSystem.devices)
            InputSystem.ResetDevice(device);

        yield return null;
    }

    private void CleanupInputDevices()
    {
        if (testKeyboard != null && testKeyboard.added)
        {
            InputSystem.RemoveDevice(testKeyboard);
            testKeyboard = null;
        }

        if (testMouse != null && testMouse.added)
        {
            InputSystem.RemoveDevice(testMouse);
            testMouse = null;
        }
    }

    [UnityTest]
    public IEnumerator MainMenu_LoadsGameScene_WhenStartGameButtonIsClicked()
    {
        var op = SceneManager.LoadSceneAsync("TestMainMenu", LoadSceneMode.Single);
        while (!op.isDone) yield return null;
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
        var op = SceneManager.LoadSceneAsync("TestGame", LoadSceneMode.Single);
        while (!op.isDone) yield return null;

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
        var op = SceneManager.LoadSceneAsync("TestGame", LoadSceneMode.Single);
        while (!op.isDone) yield return null;

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

        yield return null;
        InputSystem.Update();
        yield return null;

        InputUser.PerformPairingWithDevice(testKeyboard, playerInput.user);
        InputUser.PerformPairingWithDevice(testMouse, playerInput.user);

        playerInput.SwitchCurrentControlScheme(testKeyboard, testMouse);

        if (playerInput.actions.actionMaps.Any(map => map.name == "Player"))
        {
            playerInput.SwitchCurrentActionMap("Player");
        }

        InputSystem.Update();
        yield return null;
        InputSystem.Update();
        yield return null;

        var pauseMenu = Object.FindAnyObjectByType<PauseMenuManager>();
        if (pauseMenu?.pauseAction?.action != null)
        {
            var action = pauseMenu.pauseAction.action;
        }
    }

    private IEnumerator TriggerKeyPressSync(Key key, string keyName)
    {
        InputSystem.QueueStateEvent(testKeyboard, new KeyboardState());
        InputSystem.Update();
        yield return null;

        var keyboardState = new KeyboardState(key);
        InputSystem.QueueStateEvent(testKeyboard, keyboardState);
        InputSystem.Update();

        yield return null;

        InputSystem.QueueStateEvent(testKeyboard, new KeyboardState());
        InputSystem.Update();

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
