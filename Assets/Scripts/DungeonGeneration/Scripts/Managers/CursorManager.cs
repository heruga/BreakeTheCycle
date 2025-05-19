using UnityEngine;

public class CursorManager : MonoBehaviour
{
    public static CursorManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[CursorManager] Instance created and marked DontDestroyOnLoad.");
        }
        else
        {
            Debug.LogWarning("[CursorManager] Duplicate instance detected. Destroying new one.");
            Destroy(gameObject);
            return;
        }
    }

    public void SetGameplayState()
    {
        Debug.Log($"[CursorManager] Attempting to set Gameplay State. Current: LockState={Cursor.lockState}, Visible={Cursor.visible}");
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Debug.Log($"[CursorManager] Set Gameplay State COMPLETE. New: LockState={Cursor.lockState}, Visible={Cursor.visible}");
    }

    public void SetUIState()
    {
        Debug.Log($"[CursorManager] Attempting to set UI State. Current: LockState={Cursor.lockState}, Visible={Cursor.visible}");
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Debug.Log($"[CursorManager] Set UI State COMPLETE. New: LockState={Cursor.lockState}, Visible={Cursor.visible}");
    }

    // Можно добавить и другие состояния, если потребуется, например:
    // public void SetConfinedState()
    // {
    //     Cursor.lockState = CursorLockMode.Confined;
    //     Cursor.visible = true;
    //     Debug.Log("[CursorManager] Set Confined State (Confined, Visible).");
    // }
}
