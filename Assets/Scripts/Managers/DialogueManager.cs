using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Менеджер диалогов, управляющий показом текстовых реплик в игре
/// </summary>
public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }
    
    [Header("UI Элементы")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private Button continueButton;
    [SerializeField] private TextMeshProUGUI buttonText;
    
    [Header("Настройки")]
    [SerializeField] private float typingSpeed = 0.05f;
    [SerializeField] private float autoAdvanceDelay = 3f;
    [SerializeField] private bool useAutoAdvance = false;
    [SerializeField] private string continueText = "Далее";
    [SerializeField] private string finishText = "Закрыть";
    
    private Queue<string> dialogueLines = new Queue<string>();
    private bool isTyping = false;
    private bool isDialogueActive = false;
    private Coroutine typingCoroutine;
    private Coroutine autoAdvanceCoroutine;
    
    private Canvas createdCanvas;
    private GameObject createdDialoguePanel;
    private GameObject createdTextObject;
    private GameObject createdButtonObject;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SetupUI();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void SetupUI()
    {
        // Если UI элементы не назначены, ищем их в сцене или создаем
        if (dialoguePanel == null)
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            
            if (canvas == null)
            {
                GameObject canvasObject = new GameObject("DialogueCanvas");
                createdCanvas = canvasObject.AddComponent<Canvas>();
                createdCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObject.AddComponent<CanvasScaler>();
                canvasObject.AddComponent<GraphicRaycaster>();
                canvas = createdCanvas;
            }
            
            createdDialoguePanel = new GameObject("DialoguePanel");
            createdDialoguePanel.transform.SetParent(canvas.transform, false);
            dialoguePanel = createdDialoguePanel;
            Image panelImage = createdDialoguePanel.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.8f);
            
            RectTransform panelRect = createdDialoguePanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.1f, 0.1f);
            panelRect.anchorMax = new Vector2(0.9f, 0.3f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            
            createdTextObject = new GameObject("DialogueText");
            createdTextObject.transform.SetParent(createdDialoguePanel.transform, false);
            dialogueText = createdTextObject.AddComponent<TextMeshProUGUI>();
            dialogueText.fontSize = 24;
            dialogueText.alignment = TextAlignmentOptions.TopLeft;
            dialogueText.color = Color.white;
            
            RectTransform textRect = dialogueText.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.05f, 0.2f);
            textRect.anchorMax = new Vector2(0.95f, 0.9f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            createdButtonObject = new GameObject("ContinueButton");
            createdButtonObject.transform.SetParent(createdDialoguePanel.transform, false);
            
            continueButton = createdButtonObject.AddComponent<Button>();
            Image buttonImage = createdButtonObject.AddComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            
            RectTransform buttonRect = createdButtonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.8f, 0.05f);
            buttonRect.anchorMax = new Vector2(0.95f, 0.18f);
            buttonRect.offsetMin = Vector2.zero;
            buttonRect.offsetMax = Vector2.zero;
            
            GameObject buttonTextObj = new GameObject("ButtonText");
            buttonTextObj.transform.SetParent(createdButtonObject.transform, false);
            buttonText = buttonTextObj.AddComponent<TextMeshProUGUI>();
            buttonText.text = continueText;
            buttonText.fontSize = 18;
            buttonText.alignment = TextAlignmentOptions.Center;
            buttonText.color = Color.white;
            
            RectTransform buttonTextRect = buttonText.GetComponent<RectTransform>();
            buttonTextRect.anchorMin = Vector2.zero;
            buttonTextRect.anchorMax = Vector2.one;
            buttonTextRect.offsetMin = Vector2.zero;
            buttonTextRect.offsetMax = Vector2.zero;
        }
        
        // Настройка кнопки продолжения
        continueButton.onClick.AddListener(DisplayNextSentence);
        
        // Скрываем панель диалога при запуске
        dialoguePanel.SetActive(false);
    }
    
    /// <summary>
    /// Показать диалог с указанными репликами
    /// </summary>
    public void ShowDialogue(string[] lines)
    {
        // Если уже идет диалог, сначала завершаем его
        EndDialogue();
        
        // Добавляем строки в очередь
        dialogueLines = new Queue<string>();
        foreach (string line in lines)
        {
            dialogueLines.Enqueue(line);
        }
        
        // Активируем панель и показываем первую реплику
        isDialogueActive = true;
        dialoguePanel.SetActive(true);
        DisplayNextSentence();
    }
    
    /// <summary>
    /// Показать следующую реплику диалога
    /// </summary>
    public void DisplayNextSentence()
    {
        // Если идет печатание текста, показываем весь текст сразу
        if (isTyping)
        {
            // Остановить текущую анимацию печатания
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
                isTyping = false;
            }
            
            // Показать весь текст сразу
            dialogueText.text = dialogueText.text;
            
            // Обновить текст кнопки
            UpdateButtonText();
            return;
        }
        
        // Остановить автопереход, если он активен
        if (autoAdvanceCoroutine != null)
        {
            StopCoroutine(autoAdvanceCoroutine);
            autoAdvanceCoroutine = null;
        }
        
        // Если в очереди больше нет реплик, завершаем диалог
        if (dialogueLines.Count == 0)
        {
            EndDialogue();
            return;
        }
        
        // Получаем следующую реплику и запускаем печатание
        string sentence = dialogueLines.Dequeue();
        typingCoroutine = StartCoroutine(TypeSentence(sentence));
        
        // Обновляем текст кнопки в зависимости от того, 
        // последняя ли это реплика
        UpdateButtonText();
    }
    
    /// <summary>
    /// Корутина для постепенного печатания текста
    /// </summary>
    private IEnumerator TypeSentence(string sentence)
    {
        isTyping = true;
        dialogueText.text = "";
        
        foreach (char letter in sentence.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }
        
        isTyping = false;
        
        // Если включен автопереход, запускаем корутину
        if (useAutoAdvance)
        {
            autoAdvanceCoroutine = StartCoroutine(AutoAdvanceDialogue());
        }
    }
    
    /// <summary>
    /// Корутина для автоматического перехода к следующей реплике
    /// </summary>
    private IEnumerator AutoAdvanceDialogue()
    {
        yield return new WaitForSeconds(autoAdvanceDelay);
        DisplayNextSentence();
    }
    
    /// <summary>
    /// Завершение диалога
    /// </summary>
    public void EndDialogue()
    {
        // Остановить все активные корутины
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }
        
        if (autoAdvanceCoroutine != null)
        {
            StopCoroutine(autoAdvanceCoroutine);
            autoAdvanceCoroutine = null;
        }
        
        // Скрыть панель диалога
        dialoguePanel.SetActive(false);
        isDialogueActive = false;
        dialogueLines.Clear();
    }
    
    /// <summary>
    /// Обновление текста кнопки в зависимости от состояния диалога
    /// </summary>
    private void UpdateButtonText()
    {
        if (dialogueLines.Count == 0)
        {
            buttonText.text = finishText;
        }
        else
        {
            buttonText.text = continueText;
        }
    }
    
    // Для дебаг-целей можно добавить метод для тестирования диалогов
    private void Update()
    {
        // Для тестирования: показать диалог по нажатию клавиши T
        if (Input.GetKeyDown(KeyCode.T) && !isDialogueActive)
        {
            ShowDialogue(new string[] {
                "Это тестовый диалог для проверки системы.",
                "Вы можете добавить несколько строк текста,",
                "И они будут показаны последовательно."
            });
        }
    }

    private void OnDestroy()
    {
        // Очищаем созданные объекты
        if (createdButtonObject != null)
        {
            Destroy(createdButtonObject);
        }
        if (createdTextObject != null)
        {
            Destroy(createdTextObject);
        }
        if (createdDialoguePanel != null)
        {
            Destroy(createdDialoguePanel);
        }
        if (createdCanvas != null)
        {
            Destroy(createdCanvas.gameObject);
        }
    }
} 