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
public class MenuUITests
{
    [UnitySetUp]
    public IEnumerator SetUp()
    {
        Time.timeScale = 1f;
        yield return null;

        foreach (var device in InputSystem.devices)
            if (device is Mouse && device != Mouse.current)
                InputSystem.RemoveDevice(device);
    }

    [UnityTearDown]
    public IEnumerator Teardown()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (scene.name == "TestMainMenu" || scene.name == "TestGame")
            {
                SceneManager.UnloadSceneAsync(scene);
            }
        }

        var keyboards = InputSystem.devices;
        for (int i = keyboards.Count - 1; i >= 0; i--)
        {
            if (keyboards[i] is Keyboard && keyboards[i] != Keyboard.current)
            {
                InputSystem.RemoveDevice(keyboards[i]);
            }
        }

        foreach (var device in InputSystem.devices)
            InputSystem.ResetDevice(device);

        Time.timeScale = 1f;


        yield return null;
    }

    [UnityTest]
    public IEnumerator MainMenu_LoadsGameScene_WhenStartGameButtonIsClicked()
    {
        SceneManager.LoadScene("TestMainMenu");
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
        SceneManager.LoadScene("TestGame");
        yield return null;

        var playerInput = Object.FindAnyObjectByType<PlayerInput>();
        Assert.IsNotNull(playerInput, "PlayerInput not found");

        var keyboard = Keyboard.current ?? InputSystem.AddDevice<Keyboard>();
        var mouse = Mouse.current ?? InputSystem.AddDevice<Mouse>();

        playerInput.user.UnpairDevices();
        InputUser.PerformPairingWithDevice(keyboard, playerInput.user);
        InputUser.PerformPairingWithDevice(mouse, playerInput.user);
        playerInput.SwitchCurrentControlScheme(keyboard, mouse);

        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
        InputSystem.Update();

        yield return null;

        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        InputSystem.Update();

        yield return null;

        var pauseMenu = Object.FindAnyObjectByType<PauseMenuManager>();

        var actionRef = pauseMenu.pauseAction;
        Assert.IsNotNull(actionRef, "PauseMenuManager.pauseAction is not assigned in TestGame scene");

        var a = actionRef.action;
        Assert.IsTrue(a.enabled, "Pause action is not enabled (OnEnable may not have run)");

        Debug.Log("PAUSE bindings:\n" + string.Join("\n", a.bindings.Select(b => $"{b.groups} | {b.path}")));
        Debug.Log("PAUSE controls resolved:\n" + string.Join("\n", a.controls.Select(c => c.path)));


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

        InputSystem.QueueStateEvent(mouse, new MouseState { position = screenPosition });
        InputSystem.Update();
        yield return null;

        InputSystem.QueueStateEvent(mouse, new MouseState { position = screenPosition, buttons = 1 });
        InputSystem.Update();
        yield return null;

        InputSystem.QueueStateEvent(mouse, new MouseState { position = screenPosition, buttons = 0 });
        InputSystem.Update();
        yield return null;

        Debug.Log($"Testing click at: {screenPosition}");
        Debug.Log($"Button rectTransform.position: {rectTransform.position}");

        Assert.IsFalse(pauseMenu.pauseMenuUI.activeSelf, "Pause Menu UI should be inactive after unpausing");
        Assert.AreEqual(1f, Time.timeScale, "Time should be resumed after unpausing");
    }

    [UnityTest]
    public IEnumerator Game_PausesWithPAndQuitsWithQuitButton()
    {
        SceneManager.LoadScene("TestGame");
        yield return null;

        var keyboard = Keyboard.current ?? InputSystem.AddDevice<Keyboard>();
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.P));
        InputSystem.Update();

        yield return null;

        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        InputSystem.Update();

        yield return null;

        var pauseMenu = Object.FindAnyObjectByType<PauseMenuManager>();
        Assert.IsNotNull(pauseMenu, "PauseMenuManager should be present");
        Assert.IsTrue(pauseMenu.pauseMenuUI.activeSelf, "Pause Menu UI should be active after ESC");
        Assert.AreEqual(0f, Time.timeScale, "Time should be stopped when paused");

        var quitToMenuButton = GameObject.Find("Menu Button")?.GetComponent<Button>();
        Assert.IsNotNull(quitToMenuButton, "Quit to Menu Button should be present in the scene");
        quitToMenuButton.onClick.Invoke();

        yield return new WaitForSeconds(0.3f);

        Assert.AreEqual("MainMenu", SceneManager.GetActiveScene().name, "MainMenu Scene should be loaded after quitting");
    }
}
