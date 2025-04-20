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
    
    [Header("Managers")]
    [SerializeField] private bool initCurrencyManagerOnStart = true;
    [SerializeField] private bool initEmotionSystemOnStart = true;

    [Header("UI References")]
    [SerializeField] private GameObject currencyUIPanel;

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
            
            // Инициализируем CurrencyManager
            if (initCurrencyManagerOnStart)
            {
                InitializeCurrencyManager();
            }
            
            // Инициализируем EmotionSystem
            if (initEmotionSystemOnStart)
            {
                InitializeEmotionSystem();
            }
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
            
            // Создаем UI валюты, если мы в сознании
            if (!isInReality)
            {
                SetupCurrencyUI();
            }
        }

        UpdateUIState();
    }
    
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == consciousnessSceneName)
        {
            Debug.Log("[GameManager] Загружена сцена Consciousness, настраиваем UI валюты");
            SetupCurrencyUI();
        }
    }
    
    /// <summary>
    /// Инициализирует CurrencyManager
    /// </summary>
    private void InitializeCurrencyManager()
    {
        if (CurrencyManager.Instance == null)
        {
            GameObject currencyManagerObj = new GameObject("CurrencyManager");
            currencyManagerObj.AddComponent<CurrencyManager>();
        }
    }
    
    /// <summary>
    /// Настраивает UI валюты
    /// </summary>
    private void SetupCurrencyUI()
    {
        // Создаем UI валюты только если мы в режиме Consciousness
        if (GetCurrentState() == GameState.Consciousness)
        {
            if (CurrencyManager.Instance == null)
            {
                InitializeCurrencyManager();
            }
            
            CurrencyPrefab.CreateCurrencyUI();
            Debug.Log("[GameManager] UI валюты настроен");
        }
    }

    /// <summary>
    /// Инициализирует EmotionSystem
    /// </summary>
    private void InitializeEmotionSystem()
    {
        if (EmotionSystem.Instance == null)
        {
            GameObject emotionSystemObj = new GameObject("EmotionSystem");
            emotionSystemObj.AddComponent<EmotionSystem>();
            DontDestroyOnLoad(emotionSystemObj);
            Debug.Log("[GameManager] EmotionSystem инициализирован");
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

        string targetScene = !isInReality ? realitySceneName : consciousnessSceneName;
        Debug.Log($"[GameManager] Целевая сцена: {targetScene}");
        
        StartCoroutine(SwitchWorldCoroutine(targetScene));
    }

    private IEnumerator SwitchWorldCoroutine(string targetScene)
    {
        // Начинаем загрузку новой сцены
        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(targetScene);
        if (loadOperation == null)
        {
            Debug.LogError($"[GameManager] Не удалось начать загрузку сцены {targetScene}");
            isTransitioning = false;
            yield break;
        }

        loadOperation.allowSceneActivation = false;

        // Очищаем текущую сцену
        CleanupCurrentScene();

        // Ждем загрузки сцены
        while (loadOperation.progress < 0.9f)
        {
            Debug.Log($"[GameManager] Прогресс загрузки: {loadOperation.progress:P0}");
            yield return null;
        }

        // Активируем новую сцену
        loadOperation.allowSceneActivation = true;
        yield return loadOperation;

        // Меняем состояние только после успешной загрузки
        isInReality = !isInReality;
        
        Debug.Log("[GameManager] Сцена активирована, переключение завершено");
        isTransitioning = false;

        UpdateUIState();
    }

    private void CleanupCurrentScene()
    {
        Debug.Log("[GameManager] Начало очистки текущей сцены");
        
        // Сбрасываем активные эмоции при переключении миров
        if (EmotionSystem.Instance != null)
        {
            EmotionSystem.Instance.ResetActiveEmotions();
            Debug.Log("[GameManager] Активные эмоции сброшены");
        }
        
        // Находим всех игроков в сцене
        RealityPlayerController[] players = FindObjectsOfType<RealityPlayerController>();
        foreach (RealityPlayerController player in players)
        {
            Debug.Log($"[GameManager] Уничтожаем игрока: {player.name}");
            Destroy(player.gameObject);
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

    private void UpdateUIState()
    {
        if (currencyUIPanel != null)
        {
            currencyUIPanel.SetActive(GetCurrentState() == GameState.Consciousness);
        }
    }

    public void SwitchState()
    {
        if (!isTransitioning)
        {
            StartCoroutine(TransitionState());
        }
    }

    private IEnumerator TransitionState()
    {
        isTransitioning = true;
        
        // Переключаем состояние
        isInReality = !isInReality;
        
        // Обновляем UI
        UpdateUIState();

        // ... rest of your transition code ...

        isTransitioning = false;
        yield break;
    }

    /// <summary>
    /// Обработка смерти игрока
    /// </summary>
    public void HandlePlayerDeath()
    {
        Debug.Log("[GameManager] Обработка смерти игрока");
        
        // Получаем текущий контроллер игрока
        RealityPlayerController playerController = FindObjectOfType<RealityPlayerController>();
        ConsciousnessController consciousnessController = FindObjectOfType<ConsciousnessController>();
        
        // Пытаемся воскресить игрока, если есть активная эмоция принятия
        bool revived = false;
        
       if (consciousnessController != null)
        {
            revived = consciousnessController.TryRevive();
        }
        
        // Если игрок не воскрес, сбрасываем активные эмоции
        if (!revived)
        {
            if (EmotionSystem.Instance != null)
            {
                EmotionSystem.Instance.ResetActiveEmotions();
                Debug.Log("[GameManager] Активные эмоции сброшены после смерти");
            }
            
            // Здесь можно добавить дополнительную логику смерти игрока
            // например, загрузку последнего чекпоинта или перезапуск уровня
        }
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