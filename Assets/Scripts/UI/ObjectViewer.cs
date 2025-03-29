using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ObjectViewer : MonoBehaviour
{
    public static ObjectViewer Instance { get; private set; }

    [Header("UI Elements")]
    [SerializeField] private GameObject viewerPanel; // Панель с информацией об объекте
    [SerializeField] private TextMeshProUGUI objectNameText; // Название объекта
    [SerializeField] private TextMeshProUGUI objectDescription; // Описание объекта
    [SerializeField] private Button closeButton; // Кнопка закрытия

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
        if (viewerPanel != null)
        {
            viewerPanel.SetActive(false);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseViewer);
        }
    }

    public void ShowObject(GameObject objectToView, string description)
    {
        if (objectToView == null) return;

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
        }
    }

    private void CloseViewer()
    {
        if (viewerPanel != null)
        {
            viewerPanel.SetActive(false);
        }
    }
} 