using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using DungeonGeneration;
using DungeonGeneration.Scripts;
using UnityEngine.UI;
using System;
using BreakTheCycle.Dialogue; // Добавляем неймспейс для MonologueManager

/// Менеджер игры, управляющий состояниями и переходами между "Реальностью" и "Сознанием"
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

    [Header("UI References")]
    [SerializeField] private GameObject currencyUIPanel;
    [SerializeField] private GameObject endScreenPrefab; // Ссылка на префаб финального экрана

    [Header("Monologue Settings")]
    [SerializeField] public int bossReturnMonologueID = 0; // ID монолога для показа при возвращении в реальность после убийства босса

    [SerializeField] private bool isTransitioning = false;
    private bool isInReality = true;
    private Coroutine _lastForceRestoreCoroutine = null;
    private bool _loadingErrorOccurred = false; // Флаг для отслеживания ошибок во вложенной корутине
    private bool isPlayerInTransitionZone = false; // Флаг нахождения игрока в зоне перехода

    // Поля для отслеживания финального монолога
    private int listeningForMonologueID = -1;
    private MonologueManager monologueManagerInstance; // Кэшируем ссылку

    /// Возможные состояния игры
    public enum GameState
    {
        Reality,
        Consciousness
    }

    private void Awake()
    {
        Debug.Log("[GameManager] Awake вызван, активен: " + gameObject.activeSelf);
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[GameManager] Instance создан и сохранен");
            if (initCurrencyManagerOnStart)
            {
                InitializeCurrencyManager();
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
        Debug.Log($"[GameManager] Start начат, активен: {gameObject.activeSelf}");
        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene == "Init")
        {
            Debug.Log("[GameManager] Init загружен, переходим на MainMenu");
            SceneManager.LoadScene("MainMenu");
            return;
        }
        // Проверяем, что мы находимся в правильной сцене
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
        Debug.Log("[GameManager] OnEnable вызван, активен: " + gameObject.activeSelf);
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
        // Подписка на монолог происходит в ListenForFinalMonologue
    }
    
    private void OnDisable()
    {
        Debug.Log("[GameManager] OnDisable вызван, активен: " + gameObject.activeSelf);
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;

        // Отписываемся от события завершения монолога при отключении
        if (monologueManagerInstance != null)
        {
            monologueManagerInstance.OnMonologueComplete -= HandleFinalMonologueCompletion;
        }
    }
    
    private void OnActiveSceneChanged(Scene oldScene, Scene newScene)
    {
        Debug.Log($"[GameManager] OnActiveSceneChanged: {newScene.name}");
        int gmCount = FindObjectsOfType<GameManager>().Length;
        Debug.Log($"[GameManager] Количество GameManager в сцене: {gmCount}");
        if (gmCount > 1)
        {
            Debug.LogWarning("[GameManager] В сцене найдено несколько экземпляров GameManager! Это может привести к ошибкам.");
        }
        if (newScene.name == consciousnessSceneName)
        {
            Debug.Log("[GameManager] Загружена сцена Consciousness, настраиваем UI валюты");
            SetupCurrencyUI();
        }
    }
    
    /// Инициализирует CurrencyManager
    public void InitializeCurrencyManager()
    {
        if (CurrencyManager.Instance == null)
        {
            GameObject currencyManagerObj = new GameObject("CurrencyManager");
            currencyManagerObj.AddComponent<CurrencyManager>();
        }
    }
    
    /// Настраивает UI валюты
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

    private void Update()
    {
        // Переключение по клавише R - ТОЛЬКО если игрок в РЕАЛЬНОСТИ и в зоне перехода
        if (isInReality && Input.GetKeyDown(KeyCode.R) && !isTransitioning && isPlayerInTransitionZone) 
        {
            Debug.Log("[GameManager] Нажата клавиша R В РЕАЛЬНОСТИ И В ЗОНЕ ПЕРЕХОДА, начало переключения мира");
            SwitchWorld();
        }
    }

    public void SwitchWorld()
    {
        Debug.Log("[GameManager] SwitchWorld вызван");
        if (isTransitioning)
        {
            Debug.Log("[GameManager] Переключение уже выполняется, игнорируем запрос");
            return;
        }

        Debug.Log("[GameManager] Начало переключения мира");
        isTransitioning = true;

        // Автосохранение состояния перед переходом
        SaveGame();

        // Универсальное сохранение состояния сцены
        SaveSceneState(SceneManager.GetActiveScene().name);

        // Получаем актуальное имя текущей сцены
        string currentSceneName = SceneManager.GetActiveScene().name;
        bool currentlyInReality = (currentSceneName == realitySceneName);

        // Проверка на рассогласование состояния (опционально, но полезно для отладки)
        if (currentlyInReality != isInReality)
        {
            Debug.LogWarning($"[GameManager] Рассогласование состояния! Флаг isInReality ({isInReality}) не совпадает с текущей сценой ({currentSceneName}). Используем имя сцены.");
            // Можно принудительно синхронизировать флаг: isInReality = currentlyInReality; (но лучше найти причину рассогласования)
        }

        string targetScene = currentlyInReality ? consciousnessSceneName : realitySceneName; // Определяем цель на основе ФАКТИЧЕСКОЙ текущей сцены
        Debug.Log($"[GameManager] Текущая сцена: {currentSceneName}, Целевая сцена: {targetScene}");
        StartCoroutine(SwitchWorldCoroutine(targetScene));
    }

    public IEnumerator SwitchWorldCoroutine(string targetScene)
    {
        Debug.Log($"[GameManager] SwitchWorldCoroutine стартует для сцены: {targetScene}, вызывая LoadSceneInternalCoroutine");
        yield return StartCoroutine(LoadSceneInternalCoroutine(targetScene, true));
    }

    public IEnumerator LoadSceneAsync(string sceneName)
    {
        Debug.Log($"[GameManager] LoadSceneAsync стартует для сцены: {sceneName}, вызывая LoadSceneInternalCoroutine");
        yield return StartCoroutine(LoadSceneInternalCoroutine(sceneName, false));
    }

    // --- Универсальная корутина загрузки сцены (Обертка) --- 
    private IEnumerator LoadSceneInternalCoroutine(string sceneToLoad, bool cleanupPreviousScene)
    {
        Debug.Log($"[GameManager] LoadSceneInternalCoroutine: НАЧАЛО для {sceneToLoad}");
        isTransitioning = true; 
        _loadingErrorOccurred = false;

        Debug.Log("[GameManager] LoadSceneInternalCoroutine: Шаг 1 - FadeOut...");
        yield return ScreenFader.Instance.FadeOut(transitionDuration, fadeColor);
        Debug.Log("[GameManager] LoadSceneInternalCoroutine: FadeOut завершен, перед запуском ExecuteLoadingSequence...");

        Debug.Log("[GameManager] LoadSceneInternalCoroutine: Шаг 2 - Запуск ExecuteLoadingSequence...");
        yield return StartCoroutine(ExecuteLoadingSequence(sceneToLoad, cleanupPreviousScene));
        Debug.Log("[GameManager] LoadSceneInternalCoroutine: ExecuteLoadingSequence ЗАВЕРШЕН."); // <-- Важный лог
       
        bool errorOccurred = _loadingErrorOccurred;
        Debug.Log($"[GameManager] LoadSceneInternalCoroutine: Шаг 3 - Проверка ошибки. errorOccurred = {errorOccurred}");

        if (!errorOccurred)
        {
             Debug.Log("[GameManager] LoadSceneInternalCoroutine: Дополнительная задержка (1 кадр)...");
             yield return null; 
        }

        Debug.Log("[GameManager] LoadSceneInternalCoroutine: Шаг 4 - StartFadeIn...");
        yield return ScreenFader.Instance.StartFadeIn(transitionDuration, fadeColor); 
        Debug.Log("[GameManager] LoadSceneInternalCoroutine: StartFadeIn ЗАВЕРШЕН."); // <-- Важный лог

        Debug.Log("[GameManager] LoadSceneInternalCoroutine: Шаг 5 - Установка isTransitioning = false...");
        isTransitioning = false;
        Debug.Log(errorOccurred
            ? $"[GameManager] LoadSceneInternalCoroutine: Переход на сцену {sceneToLoad} ПРЕРВАН (после FadeIn)."
            : $"[GameManager] LoadSceneInternalCoroutine: Переход на сцену {sceneToLoad} УСПЕШНО ЗАВЕРШЕН (после FadeIn).");
        
        if (errorOccurred) 
        {
            yield break; 
        }
    }

    // --- Вложенная корутина для выполнения фактической загрузки и подготовки --- 
    // ПРИМЕЧАНИЕ: Эта корутина должна сама обрабатывать ожидаемые ошибки и устанавливать флаг _loadingErrorOccurred.
    // Необработанные исключения приведут к остановке родительской корутины.
    private IEnumerator ExecuteLoadingSequence(string sceneToLoad, bool cleanupPreviousScene)
    {
        Debug.Log($"[GameManager] ExecuteLoadingSequence: НАЧАЛО для {sceneToLoad}");
        
        if (cleanupPreviousScene)
        {
            Debug.Log("[GameManager] ExecuteLoadingSequence: Вызов CleanupCurrentScene...");
            CleanupCurrentScene();
            Debug.Log("[GameManager] ExecuteLoadingSequence: CleanupCurrentScene завершен.");
        }

        AsyncOperation operation = null;
        Debug.Log("[GameManager] ExecuteLoadingSequence: Попытка LoadSceneAsync...");
        try
        {
            operation = SceneManager.LoadSceneAsync(sceneToLoad); 
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[GameManager] ExecuteLoadingSequence: ИСКЛЮЧЕНИЕ при запуске LoadSceneAsync: {ex.Message}");
            _loadingErrorOccurred = true;
            yield break;
        }
        Debug.Log("[GameManager] ExecuteLoadingSequence: LoadSceneAsync вызван.");

        if (operation == null)
        {
            Debug.LogError("[GameManager] ExecuteLoadingSequence: LoadSceneAsync вернул null!");
            _loadingErrorOccurred = true;
            yield break;
        }
        
        operation.allowSceneActivation = false;
        Debug.Log("[GameManager] ExecuteLoadingSequence: Ожидание загрузки (allowSceneActivation = false)...");
        while (operation.progress < 0.9f)
        {
            // Debug.Log($"[GameManager] ExecuteLoadingSequence: Прогресс загрузки: {operation.progress:P0}"); // Можно раскомментировать для детального лога
            yield return null;
        }
        Debug.Log("[GameManager] ExecuteLoadingSequence: Загрузка >= 90%. Активация сцены...");
        operation.allowSceneActivation = true;
        yield return operation; 
        Debug.Log("[GameManager] ExecuteLoadingSequence: Сцена активирована.");

        // --- ИЗМЕНЕНО: Устанавливаем isInReality напрямую по имени сцены ---
        isInReality = (sceneToLoad == realitySceneName);
        Debug.Log($"[GameManager] ExecuteLoadingSequence: Флаг isInReality установлен: {isInReality} (на основе сцены '{sceneToLoad}')");
        // --- КОНЕЦ ИЗМЕНЕНИЯ ---

        Debug.Log("[GameManager] ExecuteLoadingSequence: Вызов UpdateUIState и SaveGame...");
        UpdateUIState(); 
        PlayerPrefs.SetString("LastScene", sceneToLoad); 
        PlayerPrefs.Save();
        Debug.Log("[GameManager] ExecuteLoadingSequence: UpdateUIState и SaveGame завершены.");

        Debug.Log("[GameManager] ExecuteLoadingSequence: Вызов RestoreSceneState...");
        RestoreSceneState(sceneToLoad);
        Debug.Log("[GameManager] ExecuteLoadingSequence: RestoreSceneState вызван (запустил корутины восстановления)...");

        if (sceneToLoad == realitySceneName && _lastForceRestoreCoroutine != null)
        {
            Debug.Log("[GameManager] ExecuteLoadingSequence: Ожидание ForceRestorePlayerPositionCoroutine...");
            yield return _lastForceRestoreCoroutine;
            Debug.Log("[GameManager] ExecuteLoadingSequence: ForceRestorePlayerPositionCoroutine завершен.");
            _lastForceRestoreCoroutine = null; 
        }
        
        Debug.Log("[GameManager] ExecuteLoadingSequence: Явная проверка зоны триггера...");
        if (sceneToLoad == realitySceneName)
        {
            yield return null; 
            var triggerObj = FindObjectByNameIncludingInactive("SceneTransitionTrigger");
            var player = FindObjectOfType<RealityPlayerController>();

            if (triggerObj != null && player != null)
            {
                Collider triggerCollider = triggerObj.GetComponent<Collider>();
                if (triggerCollider != null)
                {
                    // Проверяем, активен ли сам триггер и его коллайдер
                    if (triggerObj.activeInHierarchy && triggerCollider.enabled)
                    {
                        bool isInside = triggerCollider.bounds.Contains(player.transform.position);
                        SetPlayerInTransitionZone(isInside);
                        Debug.Log($"[GameManager] ExecuteLoadingSequence: Явная проверка зоны триггера. Игрок {(isInside ? "ВНУТРИ" : "СНАРУЖИ")}. Позиция игрока: {player.transform.position}, Границы триггера: {triggerCollider.bounds}");
                    }
                    else
                    {
                        // Если триггер или коллайдер неактивны, считаем, что игрок не в зоне
                        SetPlayerInTransitionZone(false);
                        Debug.LogWarning($"[GameManager] ExecuteLoadingSequence: Явная проверка зоны триггера. Триггер ({triggerObj.activeInHierarchy}) или его коллайдер ({triggerCollider.enabled}) неактивны. Установлено isPlayerInTransitionZone = false.");
                    }
                }
                 else
                {
                    Debug.LogError("[GameManager] ExecuteLoadingSequence: Не найден Collider на SceneTransitionTrigger!");
                    SetPlayerInTransitionZone(false); // На всякий случай
                }
            }
            else
            {
                if (triggerObj == null) Debug.LogError("[GameManager] ExecuteLoadingSequence: Не найден SceneTransitionTrigger для проверки зоны!");
                if (player == null) Debug.LogError("[GameManager] ExecuteLoadingSequence: Не найден RealityPlayerController для проверки зоны!");
                SetPlayerInTransitionZone(false); // Считаем, что не в зоне, если кого-то не нашли
            }
        }
        Debug.Log("[GameManager] ExecuteLoadingSequence: Явная проверка зоны триггера ЗАВЕРШЕНА.");
        
        Debug.Log("[GameManager] ExecuteLoadingSequence: Проверка и показ монолога босса...");
        if (sceneToLoad == realitySceneName && PlayerPrefs.GetInt("BossDefeated", 0) == 1 && PlayerPrefs.GetInt("PlayedBossReturnMonologue", 0) == 0)
        {
            try 
            {
                 var monologueManager = FindObjectOfType<BreakTheCycle.Dialogue.MonologueManager>();
                if (monologueManager != null)
                {
                    Debug.Log("[GameManager] ExecuteLoadingSequence: Показываем монолог после убийства босса (первый раз)");
                    monologueManager.PlayMonologue(bossReturnMonologueID);
                    // Устанавливаем флаг, что монолог проигран
                    PlayerPrefs.SetInt("PlayedBossReturnMonologue", 1);
                    PlayerPrefs.Save(); // Сохраняем изменение флага
                }
                else
                {
                    Debug.LogWarning("[GameManager] ExecuteLoadingSequence: MonologueManager не найден в сцене!");
                }
            }
            catch (System.Exception monoEx)
            {
                Debug.LogError($"[GameManager] ExecuteLoadingSequence: Ошибка при показе монолога босса: {monoEx.Message}");
                // Не устанавливаем _loadingErrorOccurred = true, т.к. это не критично для перехода
            }
        }
        Debug.Log("[GameManager] ExecuteLoadingSequence: Проверка и показ монолога босса ЗАВЕРШЕНЫ.");
        
        Debug.Log("[GameManager] ExecuteLoadingSequence: КОНЕЦ успешного выполнения (флаг ошибки не установлен).");
    }

    private void CleanupCurrentScene()
    {
        Debug.Log("[GameManager] Начало очистки текущей сцены");
        
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

    public bool IsInReality()
    {
        return isInReality;
    }

    /// Получение текущего состояния игры
    public GameState GetCurrentState()
    {
        return isInReality ? GameState.Reality : GameState.Consciousness;
    }

    /// Переключение между "Реальностью" и "Сознанием"
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

    /// Обработка смерти игрока
    public void HandlePlayerDeath()
    {
        Debug.Log("[GameManager] Обработка смерти игрока");
        
        // Получаем текущий контроллер игрока
        RealityPlayerController playerController = FindObjectOfType<RealityPlayerController>();
        ConsciousnessController consciousnessController = FindObjectOfType<ConsciousnessController>();
        
        // Пытаемся воскресить игрока
        bool revived = false;
        
        if (consciousnessController != null)
        {
            revived = consciousnessController.TryRevive();
        }
        
        // Если игрок не воскрес, загружаем последний чекпоинт
        if (!revived)
        {
            // Здесь можно добавить логику загрузки последнего чекпоинта
            Debug.Log("[GameManager] Игрок не воскрес, загрузка последнего чекпоинта");
        }
    }

    public void StartNewGame()
    {
        Debug.Log("[GameManager] StartNewGame: очистка всех PlayerPrefs и запуск новой игры");
        PlayerPrefs.DeleteAll();
        PlayerPrefs.SetInt("IsNewGame", 1);
        PlayerPrefs.Save();
        if (!isTransitioning)
        {
            StartCoroutine(LoadSceneAsync(realitySceneName));
        }
    }

    public void ContinueGame()
    {
        Debug.Log("[GameManager] ContinueGame: загрузка сохранённой игры");
        string lastScene = PlayerPrefs.GetString("LastScene", realitySceneName);
        if (!isTransitioning)
        {
            StartCoroutine(LoadSceneAsync(lastScene));
        }
    }

    public void ExitGame()
    {
        Debug.Log("[GameManager] Выход из приложения");
        Application.Quit();
    }

    // --- Универсальное сохранение состояния сцены ---
    public void SaveSceneState(string sceneName)
    {
        if (sceneName == realitySceneName)
        {
            var player = FindObjectOfType<RealityPlayerController>();
            
            // ---> ПРОВЕРКА ПЕРЕД СОХРАНЕНИЕМ <--- 
            if (player != null && player.gameObject.activeInHierarchy)
            {
                var pos = player.transform.position;
                var rot = player.transform.eulerAngles;
                Debug.Log($"[GameManager] SaveSceneState({sceneName}): === СОХРАНЕНИЕ ПОЗИЦИИ ИГРОКА: {pos} | Вращение: {rot} ==="); 
                PlayerPrefs.SetFloat($"{sceneName}_Player_Pos_X", pos.x);
                PlayerPrefs.SetFloat($"{sceneName}_Player_Pos_Y", pos.y);
                PlayerPrefs.SetFloat($"{sceneName}_Player_Pos_Z", pos.z);
                PlayerPrefs.SetFloat($"{sceneName}_Player_Rot_X", rot.x);
                PlayerPrefs.SetFloat($"{sceneName}_Player_Rot_Y", rot.y);
                PlayerPrefs.SetFloat($"{sceneName}_Player_Rot_Z", rot.z);

                // Сохранение поворота камеры
                var cameraController = player.GetComponentInChildren<FirstPersonCameraController>(); // Ищем камеру в дочерних объектах игрока
                if (cameraController != null && cameraController.gameObject.activeInHierarchy)
                {  
                    float camRotX = cameraController.GetVerticalRotation();
                    Debug.Log($"[GameManager] SaveSceneState({sceneName}): === СОХРАНЕНИЕ ПОВОРОТА КАМЕРЫ (X): {camRotX} ===");
                    PlayerPrefs.SetFloat($"{sceneName}_Camera_Rot_X", camRotX);
                }
                else 
                {   
                    Debug.LogWarning($"[GameManager] SaveSceneState({sceneName}): FirstPersonCameraController не найден или неактивен у игрока! Поворот камеры не сохранен.");
                }
            }
            else
            {
                 Debug.LogWarning($"[GameManager] SaveSceneState({sceneName}): RealityPlayerController не найден или неактивен! Состояние игрока и камеры не сохранено.");
            }

            // Сохранение состояния SceneTransitionTrigger (независимо от игрока)
            var triggerObj = FindObjectByNameIncludingInactive("SceneTransitionTrigger");
            if (triggerObj != null)
            {
                PlayerPrefs.SetInt($"{sceneName}_SceneTransitionTrigger_Active", triggerObj.activeSelf ? 1 : 0);
                Debug.Log($"[GameManager] SaveSceneState: SceneTransitionTrigger найден. Сохраняю активность для {sceneName}: {triggerObj.activeSelf}");
            }
            else
            {
                if (PlayerPrefs.GetInt("IsNewGame", 0) == 0)
                    Debug.LogError($"[GameManager] SaveSceneState: SceneTransitionTrigger не найден при сохранении для {sceneName}!");
            }
            
            PlayerPrefs.SetInt("IsNewGame", 0); // После первого сохранения сбрасываем флаг
            PlayerPrefs.Save();
            Debug.Log($"[GameManager] Состояние сцены {sceneName} сохранено в PlayerPrefs."); // Изменено сообщение
        }
        else
        {
             Debug.Log($"[GameManager] SaveSceneState: Сохранение состояния для сцены '{sceneName}' не реализовано.");
        }
    }

    // --- Универсальное восстановление состояния сцены ---
    private void RestoreSceneState(string sceneName)
    {
        if (sceneName == realitySceneName)
        {
            StartCoroutine(RestorePlayerStateCoroutineUniversal(sceneName));
            StartCoroutine(RestoreSceneTransitionTriggerActiveUniversal(sceneName));
            Debug.Log($"[GameManager] Восстановление состояния игрока и SceneTransitionTrigger запущено для {sceneName}");
        }
    }

    private IEnumerator RestorePlayerStateCoroutineUniversal(string sceneName)
    {
        RealityPlayerController player = null;
        FirstPersonCameraController cameraController = null; // Найдем камеру заранее
        for (int i = 0; i < 30; i++)
        {
            player = FindObjectOfType<RealityPlayerController>();
            cameraController = FindObjectOfType<FirstPersonCameraController>();
            if (player != null && cameraController != null) // Ждем и игрока, и камеру
                break;
            yield return null;
        }

        if (player != null && cameraController != null)
        {
            bool foundAny = false;
            float x = PlayerPrefs.GetFloat($"{sceneName}_Player_Pos_X", float.NaN);
            float y = PlayerPrefs.GetFloat($"{sceneName}_Player_Pos_Y", float.NaN);
            float z = PlayerPrefs.GetFloat($"{sceneName}_Player_Pos_Z", float.NaN);
            float rx = PlayerPrefs.GetFloat($"{sceneName}_Player_Rot_X", float.NaN);
            float ry = PlayerPrefs.GetFloat($"{sceneName}_Player_Rot_Y", float.NaN);
            float rz = PlayerPrefs.GetFloat($"{sceneName}_Player_Rot_Z", float.NaN);
            float camX = PlayerPrefs.GetFloat($"{sceneName}_Camera_Rot_X", float.NaN); // Читаем поворот камеры

            if (!float.IsNaN(x) && !float.IsNaN(y) && !float.IsNaN(z) && !float.IsNaN(rx) && !float.IsNaN(ry) && !float.IsNaN(rz))
            {
                foundAny = true;
                Vector3 restoredPos = new Vector3(x, y, z);
                Vector3 restoredRot = new Vector3(rx, ry, rz);
                Debug.Log($"[GameManager] RestorePlayerStateCoroutineUniversal({sceneName}): === ЧТЕНИЕ ПОЗИЦИИ ИЗ PREFS: PlayerPos={restoredPos} PlayerRot={restoredRot} CamRotX={camX} ===");
                
                // Восстанавливаем игрока
                Debug.Log($"[GameManager] RestorePlayerStateCoroutineUniversal: Восстанавливаю игрока Pos={restoredPos}, Rot={restoredRot}");
                player.transform.position = restoredPos;
                player.transform.eulerAngles = restoredRot;

                // ---> Восстанавливаем поворот камеры <---
                if (!float.IsNaN(camX))
                {
                    Debug.Log($"[GameManager] RestorePlayerStateCoroutineUniversal: Восстанавливаю поворот камеры CamRotX={camX}");
                    cameraController.SetVerticalRotation(camX); 
                }
                else
                {
                     Debug.LogWarning($"[GameManager] RestorePlayerStateCoroutineUniversal({sceneName}): Нет сохраненного поворота камеры (Camera_Rot_X)!");
                }

                // Синхронизируем вращение камеры (на всякий случай, хотя SetVerticalRotation уже должен был применить)
                cameraController.SyncRotationWithTransform();
                Debug.Log("[GameManager] Синхронизировал вращение камеры с transform игрока после восстановления");
                
                Debug.Log($"[GameManager] RestorePlayerStateCoroutineUniversal: Восстановление позиции и вращения игрока/камеры успешно для {sceneName}");
                Debug.Log($"[GameManager] RestorePlayerStateCoroutineUniversal: Позиция игрока сразу после восстановления: {player.transform.position}");
                yield return null;
                Debug.Log($"[GameManager] RestorePlayerStateCoroutineUniversal: Позиция игрока через кадр после восстановления: {player.transform.position}");
                
                // Жёсткое восстановление - передаем и поворот камеры
                _lastForceRestoreCoroutine = StartCoroutine(ForceRestorePlayerPositionCoroutine(player, restoredPos, restoredRot, camX));
            }
            else
            {
                if (PlayerPrefs.GetInt("IsNewGame", 0) == 0)
                    Debug.LogWarning($"[GameManager] RestorePlayerStateCoroutineUniversal: Нет сохранённых данных позиции/вращения игрока для {sceneName} в PlayerPrefs!");
            }
        }
        else
        {
             if (PlayerPrefs.GetInt("IsNewGame", 0) == 0) {
                if(player == null) Debug.LogError($"[GameManager] RestorePlayerStateCoroutineUniversal: RealityPlayerController не найден даже после ожидания для {sceneName}!");
                if(cameraController == null) Debug.LogError($"[GameManager] RestorePlayerStateCoroutineUniversal: FirstPersonCameraController не найден даже после ожидания для {sceneName}!");
             }
        }
    }

    // Жёсткое восстановление - добавляем параметр для поворота камеры
    private IEnumerator ForceRestorePlayerPositionCoroutine(RealityPlayerController player, Vector3 pos, Vector3 rot, float camRotX)
    {
        yield return null;
        yield return null;
        player.transform.position = pos;
        player.transform.eulerAngles = rot;
        Debug.Log($"[GameManager] ForceRestorePlayerPositionCoroutine: Жёстко восстановил позицию игрока: {player.transform.position}, rotation: {player.transform.eulerAngles}");
        
        var cameraController = FindObjectOfType<FirstPersonCameraController>();
        if (cameraController != null)
        {
            // ---> Повторно устанавливаем поворот камеры <---
            if (!float.IsNaN(camRotX))
            {
                cameraController.SetVerticalRotation(camRotX);
                Debug.Log($"[GameManager] ForceRestorePlayerPositionCoroutine: Жёстко восстановил поворот камеры CamRotX={camRotX}");
            }
            cameraController.SyncRotationWithTransform(); // Синхронизируем после установки
            Debug.Log("[GameManager] ForceRestorePlayerPositionCoroutine: Синхронизировал вращение камеры с transform игрока после жёсткого восстановления");
        }
        Debug.Log($"[GameManager] ForceRestorePlayerPositionCoroutine: === ФИНАЛЬНАЯ ПОЗИЦИЯ ПОСЛЕ ЖЁСТКОГО ВОССТАНОВЛЕНИЯ: {player.transform.position} ===");
    }

    private IEnumerator RestoreSceneTransitionTriggerActiveUniversal(string sceneName)
    {
        GameObject triggerObj = null;
        for (int i = 0; i < 30; i++)
        {
            triggerObj = FindObjectByNameIncludingInactive("SceneTransitionTrigger");
            if (triggerObj != null)
                break;
            yield return null;
        }
        if (triggerObj != null)
        {
            int stored = PlayerPrefs.GetInt($"{sceneName}_SceneTransitionTrigger_Active", -1);
            if (stored == -1)
            {
                if (PlayerPrefs.GetInt("IsNewGame", 0) == 0)
                    Debug.LogWarning($"[GameManager] RestoreSceneTransitionTriggerActiveUniversal: Нет сохранённого значения активности SceneTransitionTrigger для {sceneName} в PlayerPrefs!");
            }
            bool isActive = stored == 1;
            Debug.Log($"[GameManager] RestoreSceneTransitionTriggerActiveUniversal: PlayerPrefs[{sceneName}_SceneTransitionTrigger_Active]={stored}, восстанавливаю активность: {isActive} для {sceneName}");
            triggerObj.SetActive(isActive);
            Debug.Log($"[GameManager] RestoreSceneTransitionTriggerActiveUniversal: Восстановление активности SceneTransitionTrigger завершено для {sceneName}");
        }
        else
        {
            if (PlayerPrefs.GetInt("IsNewGame", 0) == 0)
                Debug.LogError($"[GameManager] RestoreSceneTransitionTriggerActiveUniversal: SceneTransitionTrigger не найден даже после ожидания для {sceneName}!");
        }
    }

    public void SaveGame()
    {
        Debug.Log("[GameManager] SaveGame: автосохранение состояния");
        // Сохраняем имя текущей сцены
        PlayerPrefs.SetString("LastScene", SceneManager.GetActiveScene().name);
        PlayerPrefs.Save();
    }

    // Универсальный поиск объекта по имени, включая неактивные
    private GameObject FindObjectByNameIncludingInactive(string name)
    {
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.name == name && obj.scene.isLoaded)
            {
                return obj;
            }
        }
        return null;
    }

    #region Final Monologue Handling

    // Метод для поиска и кэширования MonologueManager
    private void FindMonologueManager()
    {
        if (monologueManagerInstance == null)
        {
            monologueManagerInstance = FindObjectOfType<MonologueManager>();
            if (monologueManagerInstance == null)
            {
                Debug.LogError("[GameManager] MonologueManager не найден в сцене!");
            }
        }
    }

    // Публичный метод, вызываемый из FinalInteractable
    public void ListenForFinalMonologue(int monologueID)
    {
        if (monologueID < 0) return;
        FindMonologueManager(); // Убедимся, что ссылка есть
        if (monologueManagerInstance == null) return; // Не можем слушать без менеджера

        Debug.Log($"[GameManager] Начинаем слушать завершение монолога ID: {monologueID}");
        listeningForMonologueID = monologueID;

        // Отписываемся на всякий случай, потом подписываемся
        monologueManagerInstance.OnMonologueComplete -= HandleFinalMonologueCompletion;
        monologueManagerInstance.OnMonologueComplete += HandleFinalMonologueCompletion;
    }

    // Обработчик события завершения монолога
    private void HandleFinalMonologueCompletion(int completedMonologueID)
    {
        Debug.Log($"[GameManager] Получено событие OnMonologueComplete для ID: {completedMonologueID}. Ожидаем: {listeningForMonologueID}");
        if (completedMonologueID == listeningForMonologueID && listeningForMonologueID != -1)
        {
            Debug.Log($"[GameManager] Завершился ожидаемый монолог {completedMonologueID}. Запускаем финальную последовательность.");

            // Отписываемся
            if (monologueManagerInstance != null)
            {
                monologueManagerInstance.OnMonologueComplete -= HandleFinalMonologueCompletion;
            }
            listeningForMonologueID = -1; // Сбрасываем ожидание

            // Запускаем корутину сохранения и показа экрана
            StartCoroutine(EndGameSequenceCoroutine());
        }
    }

    // Корутина финальной последовательности
    private IEnumerator EndGameSequenceCoroutine()
    {
        Debug.Log("[GameManager] Начинаем сохранение перед финальным экраном...");
        SaveGame(); // Сохраняем последнюю сцену
        // Убедись, что мы сохраняем состояние нужной сцены (вероятно, Reality)
        string currentSceneName = SceneManager.GetActiveScene().name;
        if (currentSceneName == realitySceneName)
        {
            SaveSceneState(realitySceneName); // Сохраняем позицию и т.д.
        }
        else
        {
            Debug.LogWarning($"[GameManager] Попытка сохранить состояние сцены '{currentSceneName}', но сохранение реализовано только для '{realitySceneName}'.");
        }
        PlayerPrefs.Save(); // Принудительно записываем PlayerPrefs

        yield return null; // Ждем кадр

        Debug.Log("[GameManager] Показываем финальный экран...");
        if (endScreenPrefab != null)
        {
            Instantiate(endScreenPrefab); // Создаем экран
        }
        else
        {
            Debug.LogError("[GameManager] Префаб финального экрана (endScreenPrefab) не назначен в инспекторе GameManager!");
            // Как запасной вариант, можно сразу загрузить меню?
            // SceneManager.LoadScene("MainMenu");
        }
        // Дальше скрипт на самом endScreenPrefab обработает клик и загрузит меню.
    }

    #endregion

    // ---> Добавляем публичный метод для управления флагом извне <--- 
    public void SetPlayerInTransitionZone(bool isInZone)
    {
        isPlayerInTransitionZone = isInZone;
        Debug.Log($"[GameManager] Игрок {(isInZone ? "вошел в" : "вышел из")} зоны перехода. isPlayerInTransitionZone = {isPlayerInTransitionZone}");
    }
} 