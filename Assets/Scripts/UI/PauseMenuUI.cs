using UnityEngine;
using UnityEngine.SceneManagement;
using BreakTheCycle;

public class PauseMenuUI : MonoBehaviour
{
    public GameObject pausePanel;
    public static bool IsPaused { get; private set; }

    public void OnMainMenuClicked()
    {
        SetPauseMenuState(false);
        if (CursorManager.Instance != null)
        {
            CursorManager.Instance.SetUIState();
        }
        else
        {
            Debug.LogWarning("[PauseMenuUI] CursorManager.Instance is null in OnMainMenuClicked. Using fallback cursor state.");
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        SceneManager.LoadScene("MainMenu");
    }

    public void OnContinueClicked()
    {
        SetPauseMenuState(false);
    }

    public void OnSaveClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SaveGame();
            GameManager.Instance.SaveSceneState(SceneManager.GetActiveScene().name);
        }
        else
        {
            Debug.LogError("GameManager.Instance не найден!");
        }
        PlayerPrefs.Save();
    }

    public void ToggleInfoPanel(GameObject infoPanel)
    {
        if (infoPanel != null)
        {
            infoPanel.SetActive(!infoPanel.activeSelf);
        }
        else
        {
            Debug.LogError("InfoPanel не назначена для ToggleInfoPanel в PauseMenuUI!");
        }
    }

    public void OpenPauseMenu()
    {
        SetPauseMenuState(true);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (pausePanel != null)
            {
                bool currentPauseState = pausePanel.activeSelf;
                SetPauseMenuState(!currentPauseState);
            }
        }
    }

    private void SetPauseMenuState(bool isActive)
    {
        if (pausePanel != null)
            pausePanel.SetActive(isActive);

        Time.timeScale = isActive ? 0f : 1f;
        IsPaused = isActive;

        if (CursorManager.Instance != null)
        {
            if (isActive)
            {
                CursorManager.Instance.SetUIState();
            }
            else
            {
                CursorManager.Instance.SetGameplayState();
            }
        }
        else
        {
            Debug.LogWarning("[PauseMenuUI] CursorManager.Instance is null in SetPauseMenuState. Using fallback cursor state.");
            Cursor.visible = isActive;
            Cursor.lockState = isActive ? CursorLockMode.None : CursorLockMode.Locked;
        }

        if (PlayerControlManager.Instance != null)
        {
            PlayerControlManager.Instance.SetUIControlsBlock(isActive);
            Debug.Log($"[PauseMenuUI] SetUIControlsBlock called with {isActive}.");
        }
    }

    private void OnDisable()
    {
        if (IsPaused && pausePanel != null && !pausePanel.activeSelf)
        {
            IsPaused = false;
            if (PlayerControlManager.Instance != null)
            {
                PlayerControlManager.Instance.SetUIControlsBlock(false);
            }
            Debug.LogWarning("[PauseMenuUI] IsPaused reset and UIControlsBlock(false) called in OnDisable as a fallback.");
        }
    }
     private void OnDestroy()
    {
        if (IsPaused)
        {
            IsPaused = false;
            if (PlayerControlManager.Instance != null)
            {
                PlayerControlManager.Instance.SetUIControlsBlock(false);
            }
            Time.timeScale = 1f;
            Debug.LogWarning("[PauseMenuUI] IsPaused reset, UIControlsBlock(false) called, and Time.timeScale to 1 in OnDestroy.");
        }
    }
}