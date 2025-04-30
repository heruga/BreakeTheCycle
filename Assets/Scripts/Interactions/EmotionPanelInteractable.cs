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
            closeButton = emotionPanel.transform.Find("Close")?.GetComponent<Button>();
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(CloseEmotionPanel);
            }
        }

        emotionPanel.SetActive(false);
        // Убедимся, что курсор скрыт при старте
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public override void OnPlayerEnter()
    {
    }

    public override void OnPlayerExit()
    {
    }

    public override void OnInteract()
    {
        if (!isPanelOpen)
        {
            OpenEmotionPanel();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && isPanelOpen)
        {
            CloseEmotionPanel();
        }
    }

    private void OpenEmotionPanel()
    {
        isPanelOpen = true;
        if (emotionPanel != null)
        {
            emotionPanel.SetActive(true);
            // Показываем и разблокируем курсор
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            
            // Обновляем UI эмоций
            if (emotionSystem != null)
            {
                emotionSystem.OnPanelOpened();
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
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
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
        // Убедимся, что при отключении скрипта курсор возвращается в нужное состояние
        if (isPanelOpen)
        {
            CloseEmotionPanel();
        }
    }
} 