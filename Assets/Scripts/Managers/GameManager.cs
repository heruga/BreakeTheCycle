using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Менеджер игры, управляющий состояниями и переходами между "Реальностью" и "Сознанием"
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Scene Names")]
    public string realitySceneName = "Reality";
    public string consciousnessSceneName = "Consciousness";

    [Header("Transition Settings")]
    public float transitionDuration = 1f;
    public Color fadeColor = Color.black;

    private bool isTransitioning = false;
    private bool isInReality = true;

    /// <summary>
    /// Возможные состояния игры
    /// </summary>
    public enum GameState
    {
        Reality,
        Consciousness
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Убедимся, что мы начинаем в Reality
        if (SceneManager.GetActiveScene().name != realitySceneName)
        {
            SceneManager.LoadScene(realitySceneName);
        }
    }

    private void Update()
    {
        // Переключение по клавише R
        if (Input.GetKeyDown(KeyCode.R) && !isTransitioning)
        {
            SwitchWorld();
        }
    }

    public void SwitchWorld()
    {
        if (isTransitioning) return;

        isTransitioning = true;
        isInReality = !isInReality;

        string targetScene = isInReality ? realitySceneName : consciousnessSceneName;
        SceneManager.LoadScene(targetScene);
    }

    public bool IsInReality()
    {
        return isInReality;
    }

    /// <summary>
    /// Получение текущего состояния игры
    /// </summary>
    public GameState GetCurrentState()
    {
        return isInReality ? GameState.Reality : GameState.Consciousness;
    }

    /// <summary>
    /// Переключение между "Реальностью" и "Сознанием"
    /// </summary>
    public void SwitchGameState()
    {
        SwitchWorld();
    }
}

/// <summary>
/// Класс для хранения состояния игрового мира
/// </summary>
[System.Serializable]
public class GameWorldState
{
    public Vector3 playerPosition;
    public Quaternion playerRotation;
    public System.Collections.Generic.Dictionary<string, object> interactableStates = 
        new System.Collections.Generic.Dictionary<string, object>();
    
    // Добавьте другие параметры состояния мира по необходимости
} 