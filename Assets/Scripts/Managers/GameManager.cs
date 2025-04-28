using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using DungeonGeneration;
using DungeonGeneration.Scripts;
using UnityEngine.UI;
using System;

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

    [Header("Monologue Settings")]
    public int bossReturnMonologueID = 0; // ID монолога для показа при возвращении в реальность после убийства босса

    private bool isTransitioning = false;
    private bool isInReality = true;
    private AsyncOperation sceneLoadOperation;
    private bool playerRestored = false;

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
    }
    
    private void OnDisable()
    {
        Debug.Log("[GameManager] OnDisable вызван, активен: " + gameObject.activeSelf);
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
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
        // Переключение по клавише R
        if (Input.GetKeyDown(KeyCode.R) && !isTransitioning)
        {
            Debug.Log("[GameManager] Нажата клавиша R, начало переключения мира");
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

        string targetScene = !isInReality ? realitySceneName : consciousnessSceneName;
        Debug.Log($"[GameManager] Целевая сцена: {targetScene}");
        StartCoroutine(SwitchWorldCoroutine(targetScene));
    }

    public IEnumerator SwitchWorldCoroutine(string targetScene)
    {
        Debug.Log($"[GameManager] SwitchWorldCoroutine стартует для сцены: {targetScene}");
        yield return ScreenFader.Instance.FadeOut(transitionDuration, fadeColor);
        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(targetScene);
        if (loadOperation == null)
        {
            Debug.LogError($"[GameManager] Не удалось начать загрузку сцены {targetScene}");
            isTransitioning = false;
            yield break;
        }

        loadOperation.allowSceneActivation = false;
        CleanupCurrentScene();
        while (loadOperation.progress < 0.9f)
        {
            Debug.Log($"[GameManager] Прогресс загрузки: {loadOperation.progress:P0}");
            yield return null;
        }
        loadOperation.allowSceneActivation = true;
        yield return loadOperation;
        isInReality = !isInReality;
        Debug.Log("[GameManager] Сцена активирована, переключение завершено");
        isTransitioning = false;
        UpdateUIState();

        // Сохраняем имя уже активной сцены после загрузки!
        PlayerPrefs.SetString("LastScene", SceneManager.GetActiveScene().name);
        PlayerPrefs.Save();

        // Универсальное восстановление состояния сцены
        playerRestored = false;
        RestoreSceneState(SceneManager.GetActiveScene().name);

        // Ждём, пока игрок будет полностью восстановлен (жёсткое восстановление через 2 кадра)
        float waitTime = 0f;
        float maxWait = 2f; // максимум 2 секунды ожидания
        while (!playerRestored && waitTime < maxWait)
        {
            waitTime += Time.unscaledDeltaTime;
            yield return null;
        }
        
        // --- Показываем монолог при возвращении в реальность ПОСЛЕ восстановления состояния, если босс был убит ---
        if (SceneManager.GetActiveScene().name == realitySceneName && PlayerPrefs.GetInt("BossDefeated", 0) == 1)
        {
            var monologueManager = FindObjectOfType<BreakTheCycle.Dialogue.MonologueManager>();
            if (monologueManager != null)
            {
                Debug.Log("[GameManager] Показываем монолог после убийства босса (SwitchWorldCoroutine)");
                monologueManager.PlayMonologue(bossReturnMonologueID);
            }
            else
            {
                Debug.LogWarning("[GameManager] MonologueManager не найден в сцене!");
            }
            PlayerPrefs.SetInt("BossDefeated", 0); // Сбросить флаг, чтобы монолог не показывался повторно
            PlayerPrefs.Save();
        }

        yield return ScreenFader.Instance.FadeIn(transitionDuration, fadeColor);
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

    public IEnumerator LoadSceneAsync(string sceneName)
    {
        Debug.Log($"[GameManager] Начало fade-out перед загрузкой сцены: {sceneName}");
        yield return ScreenFader.Instance.FadeOut(transitionDuration, fadeColor);
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
        Debug.Log($"[GameManager] Fade-in после загрузки сцены: {sceneName}");

        // Универсальное восстановление состояния сцены с ожиданием, как в SwitchWorldCoroutine
        playerRestored = false;
        RestoreSceneState(sceneName);
        float waitTime = 0f;
        float maxWait = 2f;
        while (!playerRestored && waitTime < maxWait)
        {
            waitTime += Time.unscaledDeltaTime;
            yield return null;
        }
        
        // --- Показываем монолог при возвращении в реальность ПОСЛЕ восстановления состояния, если босс был убит ---
        if (sceneName == realitySceneName && PlayerPrefs.GetInt("BossDefeated", 0) == 1)
        {
            var monologueManager = FindObjectOfType<BreakTheCycle.Dialogue.MonologueManager>();
            if (monologueManager != null)
            {
                Debug.Log("[GameManager] Показываем монолог после убийства босса (LoadSceneAsync)");
                monologueManager.PlayMonologue(bossReturnMonologueID);
            }
            else
            {
                Debug.LogWarning("[GameManager] MonologueManager не найден в сцене!");
            }
            PlayerPrefs.SetInt("BossDefeated", 0); // Сбросить флаг, чтобы монолог не показывался повторно
            PlayerPrefs.Save();
        }

        yield return ScreenFader.Instance.FadeIn(transitionDuration, fadeColor);
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
        yield return ScreenFader.Instance.FadeOut(transitionDuration, fadeColor);
        
        // Очищаем текущую сцену
        CleanupCurrentScene();
        
        // Загружаем новую сцену
        string targetScene = isInReality ? consciousnessSceneName : realitySceneName;
        yield return StartCoroutine(LoadSceneAsync(targetScene));
        
        isInReality = !isInReality;
        isTransitioning = false;
        UpdateUIState();
        yield return ScreenFader.Instance.FadeIn(transitionDuration, fadeColor);
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
            if (player != null)
            {
                var pos = player.transform.position;
                var rot = player.transform.eulerAngles;
                PlayerPrefs.SetFloat($"{sceneName}_Player_Pos_X", pos.x);
                PlayerPrefs.SetFloat($"{sceneName}_Player_Pos_Y", pos.y);
                PlayerPrefs.SetFloat($"{sceneName}_Player_Pos_Z", pos.z);
                PlayerPrefs.SetFloat($"{sceneName}_Player_Rot_X", rot.x);
                PlayerPrefs.SetFloat($"{sceneName}_Player_Rot_Y", rot.y);
                PlayerPrefs.SetFloat($"{sceneName}_Player_Rot_Z", rot.z);
                Debug.Log($"[GameManager] SaveSceneState: Сохраняю позицию и вращение игрока для {sceneName}: ({pos.x}, {pos.y}, {pos.z}), rotation: ({rot.x}, {rot.y}, {rot.z})");
            }
            else
            {
                if (PlayerPrefs.GetInt("IsNewGame", 0) == 0)
                    Debug.LogError($"[GameManager] SaveSceneState: RealityPlayerController не найден при сохранении для {sceneName}!");
            }
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
            Debug.Log($"[GameManager] Состояние игрока и SceneTransitionTrigger сохранено в PlayerPrefs для {sceneName}");
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
        for (int i = 0; i < 30; i++)
        {
            player = FindObjectOfType<RealityPlayerController>();
            if (player != null)
                break;
            yield return null;
        }
        if (player != null)
        {
            bool foundAny = false;
            float x = PlayerPrefs.GetFloat($"{sceneName}_Player_Pos_X", float.NaN);
            float y = PlayerPrefs.GetFloat($"{sceneName}_Player_Pos_Y", float.NaN);
            float z = PlayerPrefs.GetFloat($"{sceneName}_Player_Pos_Z", float.NaN);
            float rx = PlayerPrefs.GetFloat($"{sceneName}_Player_Rot_X", float.NaN);
            float ry = PlayerPrefs.GetFloat($"{sceneName}_Player_Rot_Y", float.NaN);
            float rz = PlayerPrefs.GetFloat($"{sceneName}_Player_Rot_Z", float.NaN);
            if (!float.IsNaN(x) && !float.IsNaN(y) && !float.IsNaN(z) && !float.IsNaN(rx) && !float.IsNaN(ry) && !float.IsNaN(rz))
            {
                foundAny = true;
                Debug.Log($"[GameManager] RestorePlayerStateCoroutineUniversal: Восстанавливаю позицию: ({x}, {y}, {z}), rotation: ({rx}, {ry}, {rz}) для {sceneName}");
                player.transform.position = new Vector3(x, y, z);
                player.transform.eulerAngles = new Vector3(rx, ry, rz);
                // Синхронизируем вращение камеры с transform игрока
                var cameraController = FindObjectOfType<FirstPersonCameraController>();
                if (cameraController != null)
                {
                    cameraController.SyncRotationWithTransform();
                    Debug.Log("[GameManager] Синхронизировал вращение камеры с transform игрока после восстановления");
                }
                Debug.Log($"[GameManager] RestorePlayerStateCoroutineUniversal: Восстановление позиции и вращения игрока успешно для {sceneName}");
                Debug.Log($"[GameManager] RestorePlayerStateCoroutineUniversal: Позиция игрока сразу после восстановления: {player.transform.position}");
                yield return null;
                Debug.Log($"[GameManager] RestorePlayerStateCoroutineUniversal: Позиция игрока через кадр после восстановления: {player.transform.position}");
                // Жёсткое восстановление позиции и вращения через 2 кадра
                StartCoroutine(ForceRestorePlayerPositionCoroutine(player, new Vector3(x, y, z), new Vector3(rx, ry, rz)));
            }
            else
            {
                if (PlayerPrefs.GetInt("IsNewGame", 0) == 0)
                    Debug.LogWarning($"[GameManager] RestorePlayerStateCoroutineUniversal: Нет сохранённых данных позиции/вращения игрока для {sceneName} в PlayerPrefs!");
            }
        }
        else
        {
            if (PlayerPrefs.GetInt("IsNewGame", 0) == 0)
                Debug.LogError($"[GameManager] RestorePlayerStateCoroutineUniversal: RealityPlayerController не найден даже после ожидания для {sceneName}!");
        }
    }

    // Жёсткое восстановление позиции и вращения игрока через 2 кадра
    private IEnumerator ForceRestorePlayerPositionCoroutine(RealityPlayerController player, Vector3 pos, Vector3 rot)
    {
        yield return null;
        yield return null;
        player.transform.position = pos;
        player.transform.eulerAngles = rot;
        Debug.Log($"[GameManager] ForceRestorePlayerPositionCoroutine: Жёстко восстановил позицию: {player.transform.position}, rotation: {player.transform.eulerAngles}");
        var cameraController = FindObjectOfType<FirstPersonCameraController>();
        if (cameraController != null)
        {
            cameraController.SyncRotationWithTransform();
            Debug.Log("[GameManager] ForceRestorePlayerPositionCoroutine: Синхронизировал вращение камеры с transform игрока после жёсткого восстановления");
        }
        playerRestored = true;
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
        var allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (var t in allTransforms)
        {
            if (t.hideFlags == HideFlags.None && t.name == name)
                return t.gameObject;
        }
        return null;
    }
}

/// Класс для хранения состояния игрового мира
[System.Serializable]
public class GameWorldState
{
    public Vector3 playerPosition;
    public Quaternion playerRotation;
    public System.Collections.Generic.Dictionary<string, object> interactableStates = 
        new System.Collections.Generic.Dictionary<string, object>();
    
    // Добавьте другие параметры состояния мира по необходимости
} 