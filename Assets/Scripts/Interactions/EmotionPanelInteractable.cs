using UnityEngine;
using BreakTheCycle;
using UnityEngine.UI;

public class EmotionPanelInteractable : BaseInteractable
{
    [SerializeField] private GameObject emotionPanel; // Reference to existing EmotionPanel
    [SerializeField] private EmotionSystem emotionSystem; // Reference to EmotionSystem
    [SerializeField] private Button closeButton; // Кнопка закрытия
    private bool isPanelOpen = false;

    private void Start()
    {
        if (emotionPanel == null)
        {
            emotionPanel = GameObject.Find("CanvasEmotion");
            if (emotionPanel == null)
            {
                Debug.LogError("EmotionPanel (CanvasEmotion) not found in scene!");
                return;
            }
        }

        if (emotionSystem == null)
        {
            emotionSystem = FindObjectOfType<EmotionSystem>();
            if (emotionSystem == null)
            {
                Debug.LogError("EmotionSystem not found in scene!");
            }
        }

        // Находим кнопку Close, если она не назначена
        if (closeButton == null)
        {
            Transform closeButtonTransform = emotionPanel.transform.Find("Close");
            if (closeButtonTransform != null)
            {
                closeButton = closeButtonTransform.GetComponent<Button>();
            }
            
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(CloseEmotionPanel);
            }
            else
            {
                Debug.LogWarning("Close button not found on EmotionPanel or not assigned.");
            }
        }
        else // Если кнопка назначена в инспекторе, все равно добавляем слушатель
        {
            closeButton.onClick.RemoveAllListeners(); // Убираем старые, чтобы не было дублей
            closeButton.onClick.AddListener(CloseEmotionPanel);
        }

        emotionPanel.SetActive(false);
        // Убедимся, что курсор скрыт при старте
        if (CursorManager.Instance != null)
        {
            CursorManager.Instance.SetGameplayState();
        }
        else
        {
            Debug.LogWarning("[EmotionPanelInteractable] CursorManager.Instance is null in Start. Using fallback cursor state.");
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        }

        // Привязка обработчиков к кнопкам эмоций и LevelUp
        string[] emotionNames = { "Determination", "Compassion", "Fear", "Acceptance", "Anger", "Denial" };
        foreach (string emotionName in emotionNames)
        {
            Transform btnTransform = emotionPanel.transform.Find(emotionName);
            if (btnTransform != null)
            {
                Button btn = btnTransform.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    // Используем локальную переменную для замыкания, чтобы избежать проблем с тем же emotionName в лямбде
                    string currentEmotionName = emotionName; 
                    switch (currentEmotionName)
                    {
                        case "Determination":
                            btn.onClick.AddListener(() => emotionSystem.ToggleDetermination());
                            break;
                        case "Compassion":
                            btn.onClick.AddListener(() => emotionSystem.ToggleCompassion());
                            break;
                        case "Fear":
                            btn.onClick.AddListener(() => emotionSystem.ToggleFear());
                            break;
                        case "Acceptance":
                            btn.onClick.AddListener(() => emotionSystem.ToggleAcceptance());
                            break;
                        case "Anger":
                            btn.onClick.AddListener(() => emotionSystem.ToggleAnger());
                            break;
                        case "Denial":
                            btn.onClick.AddListener(() => emotionSystem.ToggleDenial());
                            break;
                    }
                }
            }
        }

        // Привязка обработчика к кнопке LevelUp
        Transform levelUpTransform = emotionPanel.transform.Find("LevelUp");
        if (levelUpTransform != null)
        {
            Debug.Log($"[EmotionPanelInteractable] Найден Transform кнопки LevelUp: {levelUpTransform.name}");
            Button levelUpBtn = levelUpTransform.GetComponent<Button>();
            if (levelUpBtn != null)
            {
                Debug.Log($"[EmotionPanelInteractable] Найдена кнопка LevelUp, настраиваю обработчик. Interactable: {levelUpBtn.interactable}");
                
                // Проверяем настройки кнопки
                var image = levelUpBtn.GetComponent<Image>();
                if (image != null)
                {
                    Debug.Log($"[EmotionPanelInteractable] Image компонент: raycastTarget={image.raycastTarget}, color={image.color}");
                }
                
                levelUpBtn.onClick.RemoveAllListeners();
                levelUpBtn.onClick.AddListener(() => {
                    Debug.Log("[EmotionPanelInteractable] Кнопка LevelUp нажата");
                    if (emotionSystem != null)
                    {
                        emotionSystem.UpgradeMaxActiveEmotions();
                    }
                    else
                    {
                        Debug.LogError("[EmotionPanelInteractable] emotionSystem is null when trying to upgrade!");
            }
                });
                
                // Проверяем, что обработчик действительно добавился
                Debug.Log($"[EmotionPanelInteractable] Количество обработчиков на кнопке LevelUp: {levelUpBtn.onClick.GetPersistentEventCount()}");
            }
            else
            {
                Debug.LogError("[EmotionPanelInteractable] Кнопка LevelUp найдена, но на ней нет компонента Button!");
            }
        }
        else
        {
            Debug.LogError("[EmotionPanelInteractable] Не найдена кнопка LevelUp в панели эмоций!");
        }
    }

    public override void OnPlayerEnter()
    {
        base.OnPlayerEnter(); // ВЫЗЫВАЕМ БАЗОВУЮ РЕАЛИЗАЦИЮ
        // Debug.Log($"Player entered zone of {gameObject.name}"); // Для отладки, если нужно
    }

    public override void OnPlayerExit()
    {
        base.OnPlayerExit(); // ВЫЗЫВАЕМ БАЗОВУЮ РЕАЛИЗАЦИЮ
        // Debug.Log($"Player exited zone of {gameObject.name}"); // Для отладки, если нужно
    }

    public override void OnInteract()
    {
        if (!base.IsInteractable) return; // Проверка из базового класса

        if (!isPanelOpen)
        {
            OpenEmotionPanel();
        }
        // base.OnInteract(); // Вызываем, если в базовом OnInteract есть общая логика (например, onInteractEvent)
    }

    private void Update()
    {
        // if (Input.GetKeyDown(KeyCode.Escape) && isPanelOpen)
        // {
        //     CloseEmotionPanel();
        // }
    }

    private void OpenEmotionPanel()
    {
        isPanelOpen = true;
        if (emotionPanel != null)
        {
            emotionPanel.SetActive(true);
            
            // Проверяем состояние кнопки LevelUp
            Transform levelUpTransform = emotionPanel.transform.Find("LevelUp");
            if (levelUpTransform != null)
            {
                Button levelUpBtn = levelUpTransform.GetComponent<Button>();
                if (levelUpBtn != null)
                {
                    Debug.Log($"[EmotionPanelInteractable] При открытии панели: LevelUp кнопка interactable={levelUpBtn.interactable}");
                    var image = levelUpBtn.GetComponent<Image>();
                    if (image != null)
                    {
                        Debug.Log($"[EmotionPanelInteractable] При открытии панели: LevelUp image raycastTarget={image.raycastTarget}, color={image.color}");
                    }
                }
            }
            
            // Показываем и разблокируем курсор
            if (CursorManager.Instance != null)
            {
                CursorManager.Instance.SetUIState();
            }
            else
            {
                Debug.LogWarning("[EmotionPanelInteractable] CursorManager.Instance is null in OpenEmotionPanel. Using fallback cursor state.");
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            }
            
            // Обновляем UI эмоций
            if (emotionSystem != null)
            {
                emotionSystem.OnPanelOpened();
            }
            else
            {
                Debug.LogError("[EmotionPanelInteractable] emotionSystem is null when trying to open panel!");
            }
            
            // Опционально: отключаем управление персонажем
            if (PlayerControlManager.Instance != null)
            {
                PlayerControlManager.Instance.SetControlsEnabled(false);
            }
        }
    }

    public void CloseEmotionPanel()
    {
        isPanelOpen = false;
        if (emotionPanel != null)
        {
            emotionPanel.SetActive(false);
            Debug.Log("[EmotionPanelInteractable] Закрытие панели эмоций. Скрытие курсора...");
            // Скрываем и блокируем курсор
            if (CursorManager.Instance != null)
            {
                CursorManager.Instance.SetGameplayState();
            }
            else
            {
                Debug.LogWarning("[EmotionPanelInteractable] CursorManager.Instance is null in CloseEmotionPanel. Using fallback cursor state.");
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            }
            Debug.Log($"[EmotionPanelInteractable] Состояние курсора после скрытия: visible={Cursor.visible}, lockState={Cursor.lockState}");
            
            // Опционально: включаем управление персонажем
            if (PlayerControlManager.Instance != null)
            {
                PlayerControlManager.Instance.SetControlsEnabled(true);
            }
        }
    }

    private void OnDisable()
    {
        if (isPanelOpen) // Если панель была открыта при отключении объекта/компонента
        {
            // Убедимся, что UI скрыт и управление возвращено
            if (emotionPanel != null) emotionPanel.SetActive(false);
            
            if (CursorManager.Instance != null)
            {
                CursorManager.Instance.SetGameplayState();
            }
            else
            {
                Debug.LogWarning("[EmotionPanelInteractable] CursorManager.Instance is null in OnDisable. Using fallback cursor state.");
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            }

            if (PlayerControlManager.Instance != null && !PlayerControlManager.Instance.ControlsEnabled)
            {
                PlayerControlManager.Instance.SetControlsEnabled(true);
            }
            isPanelOpen = false; // Сбрасываем флаг
        }
    }
} 