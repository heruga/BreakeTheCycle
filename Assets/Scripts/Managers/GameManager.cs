using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using DungeonGeneration;
using DungeonGeneration.Scripts;

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
    private AsyncOperation sceneLoadOperation;

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
        Debug.Log("[GameManager] Awake начат");
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[GameManager] Instance создан и сохранен");
        }
        else
        {
            Debug.Log("[GameManager] Уничтожение дубликата Instance");
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        Debug.Log("[GameManager] Start начат");
        // Проверяем, что мы находимся в правильной сцене
        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene != realitySceneName && currentScene != consciousnessSceneName)
        {
            Debug.Log($"[GameManager] Загрузка начальной сцены {realitySceneName}");
            StartCoroutine(LoadSceneAsync(realitySceneName));
        }
        else
        {
            Debug.Log($"[GameManager] Уже находимся в сцене {currentScene}");
            isInReality = currentScene == realitySceneName;
        }
    }

    private void Update()
    {
        // Переключение по клавише R
        if (Input.GetKeyDown(KeyCode.R) && !isTransitioning)
        {
            Debug.Log("[GameManager] Нажата клавиша R, начало переключения мира");
            SwitchWorld();
        }
    }

    public void SwitchWorld()
    {
        if (isTransitioning)
        {
            Debug.Log("[GameManager] Переключение уже выполняется, игнорируем запрос");
            return;
        }

        Debug.Log("[GameManager] Начало переключения мира");
        isTransitioning = true;
        isInReality = !isInReality;

        string targetScene = isInReality ? realitySceneName : consciousnessSceneName;
        Debug.Log($"[GameManager] Целевая сцена: {targetScene}");
        
        // Очищаем текущую сцену перед загрузкой новой
        CleanupCurrentScene();
        
        StartCoroutine(LoadSceneAsync(targetScene));
    }

    private void CleanupCurrentScene()
    {
        Debug.Log("[GameManager] Начало очистки текущей сцены");
        
        // Находим всех игроков в сцене
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject player in players)
        {
            Debug.Log($"[GameManager] Уничтожаем игрока: {player.name}");
            Destroy(player);
        }
        
        // Очищаем все объекты DungeonGenerator
        DungeonGenerator[] dungeonGenerators = FindObjectsOfType<DungeonGenerator>();
        foreach (DungeonGenerator generator in dungeonGenerators)
        {
            Debug.Log($"[GameManager] Уничтожаем DungeonGenerator: {generator.name}");
            Destroy(generator.gameObject);
        }
        
        // Очищаем все порталы
        var portals = FindObjectsOfType<InteractablePortal>();
        foreach (var portal in portals)
        {
            Debug.Log($"[GameManager] Уничтожаем портал: {portal.name}");
            Destroy(portal.gameObject);
        }
        
        // Очищаем все комнаты
        var rooms = FindObjectsOfType<RoomManager>();
        foreach (var room in rooms)
        {
            Debug.Log($"[GameManager] Уничтожаем комнату: {room.name}");
            Destroy(room.gameObject);
        }
        
        // Принудительно запускаем сборщик мусора
        System.GC.Collect();
        Resources.UnloadUnusedAssets();
        
        Debug.Log("[GameManager] Очистка сцены завершена");
    }

    private IEnumerator LoadSceneAsync(string sceneName)
    {
        Debug.Log($"[GameManager] Начало асинхронной загрузки сцены: {sceneName}");
        AsyncOperation operation = null;
        bool sceneExists = false;
        try
        {
            Debug.Log($"[GameManager] Проверка существования сцены: {sceneName}");
            sceneExists = !SceneUtility.GetBuildIndexByScenePath(sceneName).Equals(-1);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[GameManager] Ошибка при проверке сцены {sceneName}: {e.Message}");
            isTransitioning = false;
            yield break;
        }

        if (!sceneExists)
        {
            Debug.LogError($"[GameManager] Сцена {sceneName} не найдена в Build Settings!");
            isTransitioning = false;
            yield break;
        }

        Debug.Log($"[GameManager] Сцена {sceneName} найдена, начинаем загрузку");
        yield return StartCoroutine(LoadSceneOperation(sceneName));
    }

    private IEnumerator LoadSceneOperation(string sceneName)
    {
        Debug.Log($"[GameManager] Создание операции загрузки для сцены: {sceneName}");
        AsyncOperation operation = null;
        try
        {
            operation = SceneManager.LoadSceneAsync(sceneName);
            Debug.Log("[GameManager] Операция загрузки создана успешно");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[GameManager] Ошибка при создании операции загрузки сцены {sceneName}: {e.Message}");
            isTransitioning = false;
            yield break;
        }

        operation.allowSceneActivation = false;
        Debug.Log("[GameManager] Активация сцены отложена");

        // Ждем загрузки сцены
        while (operation.progress < 0.9f)
        {
            Debug.Log($"[GameManager] Прогресс загрузки: {operation.progress:P0}");
            yield return null;
        }

        Debug.Log("[GameManager] Загрузка завершена, активируем сцену");
        operation.allowSceneActivation = true;
        yield return operation;
        Debug.Log("[GameManager] Сцена активирована");
        isTransitioning = false;
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
        Debug.Log("[GameManager] Вызов SwitchGameState");
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