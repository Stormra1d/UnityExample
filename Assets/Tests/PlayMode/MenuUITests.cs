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
/// Seem rather flaky with opening the pause menu via input simulation. Input Simulation proved difficult and required changing to using InputActions for Input control.
/// </summary>
public class MenuUITests : BasePlayModeTest
{
    [UnitySetUp]
    public IEnumerator SetUp()
    {
        yield return null;

        foreach (var device in InputSystem.devices.ToList())
        {
            if ((device is Mouse && device != Mouse.current) || (device is Keyboard && device != Keyboard.current))
                InputSystem.RemoveDevice(device);
        }

        var playerInput = Object.FindAnyObjectByType<PlayerInput>();
        if (playerInput != null)
        {
            playerInput.user.UnpairDevices();
        }
    }

    [UnityTearDown]
    public IEnumerator Teardown()
    {
        var playerInput = Object.FindAnyObjectByType<PlayerInput>();
        if (playerInput != null)
            playerInput.user.UnpairDevices();

        foreach (var device in InputSystem.devices.ToList())
        {
            if ((device is Mouse && device != Mouse.current) || (device is Keyboard && device != Keyboard.current))
                InputSystem.RemoveDevice(device);
        }

        foreach (var device in InputSystem.devices)
            InputSystem.ResetDevice(device);

        yield return null;
    }

    [UnityTest]
    public IEnumerator MainMenu_LoadsGameScene_WhenStartGameButtonIsClicked()
    {
        var op = SceneManager.LoadSceneAsync("TestMainMenu", LoadSceneMode.Single);
        while (!op.isDone) yield return null;
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

        var playerInput = Object.FindAnyObjectByType<PlayerInput>();
        Assert.IsNotNull(playerInput, "PlayerInput not found");
        playerInput.SwitchCurrentActionMap("UI");

        var keyboard = Keyboard.current ?? InputSystem.AddDevice<Keyboard>();
        var mouse = Mouse.current ?? InputSystem.AddDevice<Mouse>();

        playerInput.user.UnpairDevices();
        InputUser.PerformPairingWithDevice(keyboard, playerInput.user);
        InputUser.PerformPairingWithDevice(mouse, playerInput.user);
        playerInput.SwitchCurrentControlScheme(keyboard, mouse);

        InputSystem.Update();
        yield return null;
        InputSystem.Update();

        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
        InputSystem.Update();
        yield return new WaitForSeconds(0.1f);
        yield return null;

        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        InputSystem.Update();
        yield return new WaitForSeconds(0.1f);
        yield return null;

        var pauseMenu = Object.FindAnyObjectByType<PauseMenuManager>();

        var actionRef = pauseMenu.pauseAction;
        Assert.IsNotNull(actionRef, "PauseMenuManager.pauseAction is not assigned in TestGame scene");

        var a = actionRef.action;
        Assert.IsTrue(a.enabled, "Pause action is not enabled (OnEnable may not have run)");

        Assert.IsNotNull(pauseMenu, "PauseMenuManager should be present");
        Assert.IsTrue(pauseMenu.pauseMenuUI.activeSelf, "Pause Menu UI should be active after ESC");
        Assert.AreEqual(0f, Time.timeScale, "Time should be stopped when paused");

        foreach (var device in InputSystem.devices)
        {
            if (device is Mouse && device != Mouse.current)
            {
                InputSystem.RemoveDevice(device);
            }
        }

        var resumeButtonGameObject = GameObject.Find("Resume Button")?.GetComponent<Button>();
        Assert.IsNotNull(resumeButtonGameObject, "Resume Button should be present");
        var rectTransform = resumeButtonGameObject.GetComponent<RectTransform>();

        var canvas = Object.FindAnyObjectByType<Canvas>();
        Camera uiCamera = (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera) ? canvas.worldCamera : null;

        Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(uiCamera, rectTransform.position);

        Assert.IsNotNull(Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>(), "Event System should be present");

        Debug.Log($"PauseMenuManager found: {pauseMenu != null}, PauseAction: {pauseMenu?.pauseAction?.action?.name}, Enabled: {pauseMenu?.pauseAction?.action?.enabled}");

        InputSystem.QueueStateEvent(mouse, new MouseState { position = screenPosition });
        InputSystem.Update();
        yield return new WaitForSeconds(0.1f);
        yield return null;

        InputSystem.QueueStateEvent(mouse, new MouseState { position = screenPosition, buttons = 1 });
        InputSystem.Update();
        yield return new WaitForSeconds(0.1f);
        yield return null;

        InputSystem.QueueStateEvent(mouse, new MouseState { position = screenPosition, buttons = 0 });
        InputSystem.Update();
        yield return new WaitForSeconds(0.1f);
        yield return null;

        Assert.IsFalse(pauseMenu.pauseMenuUI.activeSelf, "Pause Menu UI should be inactive after unpausing");
        Assert.AreEqual(1f, Time.timeScale, "Time should be resumed after unpausing");
    }

    [UnityTest]
    public IEnumerator Game_PausesWithPAndQuitsWithQuitButton()
    {
        var op = SceneManager.LoadSceneAsync("TestGame", LoadSceneMode.Single);
        while (!op.isDone) yield return null;
        yield return null;

        var playerInput = Object.FindAnyObjectByType<PlayerInput>();
        Assert.IsNotNull(playerInput, "PlayerInput not found");
        playerInput.SwitchCurrentActionMap("UI");

        var keyboard = Keyboard.current ?? InputSystem.AddDevice<Keyboard>();
        var mouse = Mouse.current ?? InputSystem.AddDevice<Mouse>();

        playerInput.user.UnpairDevices();
        InputUser.PerformPairingWithDevice(keyboard, playerInput.user);
        InputUser.PerformPairingWithDevice(mouse, playerInput.user);
        playerInput.SwitchCurrentControlScheme(keyboard, mouse);

        InputSystem.Update();
        yield return null;

        var pauseMenu = Object.FindAnyObjectByType<PauseMenuManager>();
        Debug.Log($"PauseMenuManager found: {pauseMenu != null}, PauseAction: {pauseMenu?.pauseAction?.action?.name}, Enabled: {pauseMenu?.pauseAction?.action?.enabled}");

        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.P));
        InputSystem.Update();
        yield return new WaitForSeconds(0.1f);
        yield return null;

        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        InputSystem.Update();
        yield return new WaitForSeconds(0.1f);
        yield return null;

        Assert.IsNotNull(pauseMenu, "PauseMenuManager should be present");
        Assert.IsTrue(pauseMenu.pauseMenuUI.activeSelf, "Pause Menu UI should be active after P");
        Assert.AreEqual(0f, Time.timeScale, "Time should be stopped when paused");

        var quitToMenuButton = GameObject.Find("Menu Button")?.GetComponent<Button>();
        Assert.IsNotNull(quitToMenuButton, "Quit to Menu Button should be present in the scene");
        quitToMenuButton.onClick.Invoke();

        yield return new WaitForSeconds(0.3f);

        Assert.AreEqual("MainMenu", SceneManager.GetActiveScene().name, "MainMenu Scene should be loaded after quitting");
    }
}
