using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;
using BreakTheCycle;

public class ObjectViewer : MonoBehaviour
{
    public static ObjectViewer Instance { get; private set; }

    [Header("UI Elements")]
    [SerializeField] private GameObject viewerPanel; // Панель с информацией об объекте
    [SerializeField] private TextMeshProUGUI objectNameText; // Название объекта
    [SerializeField] private TextMeshProUGUI objectDescription; // Описание объекта
    [SerializeField] private Button closeButton; // Кнопка закрытия

    [Header("Player Control")]
    [SerializeField] private PlayerController playerController; // Ссылка на контроллер игрока

    private GameObject objectToView;

    private void Awake()
    {
        Debug.Log("ObjectViewer: Awake");
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        // Находим контроллер игрока, если не назначен
        if (playerController == null)
        {
            playerController = FindObjectOfType<PlayerController>();
            Debug.Log($"ObjectViewer: PlayerController найден автоматически: {playerController != null}");
        }
        else
        {
            Debug.Log("ObjectViewer: PlayerController назначен в инспекторе");
        }

        if (viewerPanel != null)
        {
            viewerPanel.SetActive(false);
            Debug.Log("ObjectViewer: Панель просмотра инициализирована");
        }
        else
        {
            Debug.LogError("ObjectViewer: Панель просмотра не назначена!");
        }

        // Проверяем кнопку закрытия
        if (closeButton != null)
        {
            Debug.Log($"ObjectViewer: Кнопка закрытия найдена: {closeButton.name}");
            
            // Проверяем, что кнопка интерактивна
            if (!closeButton.interactable)
            {
                closeButton.interactable = true;
                Debug.Log("ObjectViewer: Кнопка закрытия сделана интерактивной");
            }

            // Проверяем, что кнопка видима
            CanvasGroup canvasGroup = closeButton.GetComponent<CanvasGroup>();
            if (canvasGroup != null && canvasGroup.alpha == 0)
            {
                canvasGroup.alpha = 1;
                Debug.Log("ObjectViewer: Кнопка закрытия сделана видимой");
            }
        }
        else
        {
            Debug.LogError("ObjectViewer: Кнопка закрытия не назначена в инспекторе!");
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        Debug.Log("ObjectViewer: Start");
        // Скрываем панель при старте
        if (viewerPanel != null)
        {
            viewerPanel.SetActive(false);
        }
    }

    public void ShowObject(GameObject objectToView, string description)
    {
        Debug.Log($"ObjectViewer: Попытка показать объект {objectToView?.name}");
        if (objectToView == null) return;

        this.objectToView = objectToView;

        // Показываем название объекта
        if (objectNameText != null)
        {
            InteractableObject interactable = objectToView.GetComponent<InteractableObject>();
            objectNameText.text = interactable != null ? interactable.GetDisplayName() : "Объект";
        }

        // Показываем описание
        if (objectDescription != null)
        {
            objectDescription.text = description;
        }

        // Показываем панель
        if (viewerPanel != null)
        {
            viewerPanel.SetActive(true);
            Debug.Log("ObjectViewer: Панель просмотра активирована");
            
            // Блокируем управление
            if (playerController != null)
            {
                playerController.SetControlEnabled(false);
                Debug.Log("ObjectViewer: Управление заблокировано");
            }
            
            // Скрываем подсказку взаимодействия
            if (playerController != null)
            {
                playerController.HideInteractionPrompt();
                Debug.Log("ObjectViewer: Подсказка взаимодействия скрыта");
            }
        }
    }

    public void CloseViewer()
    {
        if (viewerPanel != null)
        {
            viewerPanel.SetActive(false);
            Debug.Log("ObjectViewer: Панель просмотра деактивирована");
            
            // Восстанавливаем управление
            if (playerController != null)
            {
                playerController.SetControlEnabled(true);
                Debug.Log("ObjectViewer: Управление восстановлено");
            }
        }
    }

    // Добавляем возможность закрыть окно по клавише Escape
    private void Update()
    {
        if (viewerPanel != null && viewerPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseViewer();
        }
    }

    public bool IsViewerActive()
    {
        return viewerPanel != null && viewerPanel.activeSelf;
    }
} 