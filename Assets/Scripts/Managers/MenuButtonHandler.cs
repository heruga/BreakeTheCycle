using UnityEngine;

public class MenuButtonHandler : MonoBehaviour
{
    public void StartNewGame()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.StartNewGame();
        else
            Debug.LogError("GameManager.Instance не найден!");
    }

    public void ExitGame()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.ExitGame();
        else
            Debug.LogError("GameManager.Instance не найден!");
    }
} 