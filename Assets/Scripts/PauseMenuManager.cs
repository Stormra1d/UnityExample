using StarterAssets;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseMenuManager : MonoBehaviour
{
    public GameObject pauseMenuUI;
    public GameObject mainUI;
    public GameObject player;

    public InputActionReference pauseAction;
    private bool isPaused = false;

    private FirstPersonController firstPersonController;
    private PlayerInput playerInput;

    private void OnEnable()
    {
        if (pauseAction != null)
        {
            pauseAction.action.performed += OnPause;
            pauseAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (pauseAction != null)
        {
            pauseAction.action.performed -= OnPause;
            pauseAction.action.Disable();
        }
    }

    private void OnPause(InputAction.CallbackContext context)
    {
        if (isPaused)
            ResumeGame();
        else
            PauseGame();
    }

    private void Start()
    {
        pauseMenuUI.SetActive(false);
        LockCursor();

        if (player != null)
        {
            firstPersonController = player.GetComponent<FirstPersonController>();
            playerInput = player.GetComponent<PlayerInput>();
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;

        mainUI.SetActive(false);
        pauseMenuUI.SetActive(true);

        UnlockCursor();
        if (firstPersonController) firstPersonController.enabled = false;
    }

    public void ResumeGame()
    {
        isPaused = false;
        mainUI.SetActive(true);
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;

        LockCursor();
        if (firstPersonController) firstPersonController.enabled = true;
    }

    public void QuitToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void DisablePlayerInput()
    {
        if (firstPersonController != null)
        {
            firstPersonController.enabled = false;
        }

        if (playerInput != null)
        {
            playerInput.enabled = false;
        }
    }

    private void EnablePlayerInput()
    {
        if (firstPersonController != null)
        {
            firstPersonController.enabled = true;
        }

        if (playerInput != null)
        {
            playerInput.enabled = true;
        }
    }
}
