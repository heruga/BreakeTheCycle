using UnityEngine;

public class MainMenuCursor : MonoBehaviour
{
    void Start()
    {
        if (CursorManager.Instance != null)
        {
            CursorManager.Instance.SetUIState();
        }
        else
        {
            Debug.LogWarning("[MainMenuCursor] CursorManager.Instance is null. Using fallback cursor state.");
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }
} 