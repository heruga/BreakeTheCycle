using UnityEngine;
using UnityEngine.SceneManagement;
using BreakTheCycle;

public class PauseMenuUI : MonoBehaviour
{
    public GameObject pausePanel;

    public void OnMainMenuClicked()
    {
        SetPauseMenuState(false);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
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

        // Гарантируем сохранение всех данных PlayerPrefs
        PlayerPrefs.Save();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (pausePanel != null)
            {
                bool isActive = pausePanel.activeSelf;
                SetPauseMenuState(!isActive);
            }
        }
    }

    private void SetPauseMenuState(bool isActive)
    {
        if (pausePanel != null)
            pausePanel.SetActive(isActive);

        Time.timeScale = isActive ? 0f : 1f;
        Cursor.visible = isActive;
        Cursor.lockState = isActive ? CursorLockMode.None : CursorLockMode.Locked;

        if (PlayerControlManager.Instance != null)
            PlayerControlManager.Instance.SetControlsEnabled(!isActive);
    }
} 