using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Контроллер игрока для режима "Реальность" с видом от первого лица
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("Движение")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float gravity = 20f;
    
    [Header("Вращение камеры")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float verticalRotationLimit = 80f;
    [SerializeField] private Transform playerCamera;
    
    [Header("Проверка земли")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundDistance = 0.4f;
    [SerializeField] private LayerMask groundMask;
    
    [Header("Взаимодействие")]
    [SerializeField] private float interactionRange = 3f;
    [SerializeField] private KeyCode interactionKey = KeyCode.F;
    [SerializeField] private LayerMask interactionMask;
    [SerializeField] private GameObject interactionPrompt;
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private KeyCode switchRealityKey = KeyCode.R;
    
    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;
    private float verticalRotation = 0f;
    private InteractableObject currentInteractable;
    
    private void Awake()
    {
        // Получаем компоненты
        controller = GetComponent<CharacterController>();
        
        // Создаем компоненты, если их нет
        if (controller == null)
        {
            controller = gameObject.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.5f;
            controller.center = new Vector3(0, 0.9f, 0);
        }
        
        // Настройка камеры
        if (playerCamera == null)
        {
            // Ищем камеру, если она уже есть
            Camera mainCamera = Camera.main;
            if (mainCamera != null && mainCamera.transform.parent == transform)
            {
                playerCamera = mainCamera.transform;
            }
            else
            {
                // Создаем новую камеру
                GameObject cameraObject = new GameObject("FirstPersonCamera");
                cameraObject.transform.SetParent(transform);
                cameraObject.transform.localPosition = new Vector3(0, 1.6f, 0);
                playerCamera = cameraObject.transform;
                
                Camera newCamera = cameraObject.AddComponent<Camera>();
                newCamera.nearClipPlane = 0.1f;
                
                // Добавляем аудиослушатель
                cameraObject.AddComponent<AudioListener>();
            }
        }
        
        // Настройка проверки земли
        if (groundCheck == null)
        {
            GameObject groundCheckObj = new GameObject("GroundCheck");
            groundCheckObj.transform.SetParent(transform);
            groundCheckObj.transform.localPosition = new Vector3(0, -0.9f, 0);
            groundCheck = groundCheckObj.transform;
        }
        
        // Настройка UI для взаимодействия
        SetupInteractionUI();
    }
    
    private void Start()
    {
        // Блокируем и скрываем курсор
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    private void SetupInteractionUI()
    {
        // Если UI подсказки не назначен, создаем его
        if (interactionPrompt == null)
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            
            if (canvas == null)
            {
                GameObject canvasObject = new GameObject("PlayerCanvas");
                canvas = canvasObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObject.AddComponent<CanvasScaler>();
                canvasObject.AddComponent<GraphicRaycaster>();
            }
            
            interactionPrompt = new GameObject("InteractionPrompt");
            interactionPrompt.transform.SetParent(canvas.transform, false);
            
            RectTransform promptRect = interactionPrompt.AddComponent<RectTransform>();
            promptRect.anchorMin = new Vector2(0.5f, 0.2f);
            promptRect.anchorMax = new Vector2(0.5f, 0.2f);
            promptRect.sizeDelta = new Vector2(400, 50);
            promptRect.anchoredPosition = Vector2.zero;
            
            // Фон подсказки
            Image promptBg = interactionPrompt.AddComponent<Image>();
            promptBg.color = new Color(0, 0, 0, 0.7f);
            
            // Текст подсказки
            GameObject textObject = new GameObject("PromptText");
            textObject.transform.SetParent(interactionPrompt.transform, false);
            
            promptText = textObject.AddComponent<TextMeshProUGUI>();
            promptText.text = "Нажмите F для взаимодействия";
            promptText.alignment = TextAlignmentOptions.Center;
            promptText.color = Color.white;
            promptText.fontSize = 24;
            
            RectTransform textRect = promptText.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            // Скрываем подсказку по умолчанию
            interactionPrompt.SetActive(false);
        }
    }
    
    private void Update()
    {
        // Проверка касания земли
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
        
        // Обработка ввода для движения
        ProcessMovementInput();
        
        // Обработка ввода для вращения камеры
        ProcessCameraRotation();
        
        // Обработка взаимодействия с объектами
        ProcessInteraction();
        
        // Обработка переключения реальностей
        ProcessRealitySwitch();
    }
    
    private void ProcessMovementInput()
    {
        // Получаем ввод пользователя
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        
        // Определяем скорость в зависимости от спринта
        float speed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : walkSpeed;
        
        // Создаем вектор движения относительно направления взгляда
        Vector3 move = transform.right * x + transform.forward * z;
        
        // Применяем движение
        controller.Move(move * speed * Time.deltaTime);
        
        // Прыжок
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpForce * 2f * gravity);
        }
        
        // Применяем гравитацию
        velocity.y -= gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
    
    private void ProcessCameraRotation()
    {
        // Получаем ввод мыши
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        
        // Вращение вокруг вертикальной оси (поворот персонажа)
        transform.Rotate(Vector3.up * mouseX);
        
        // Вращение вокруг горизонтальной оси (наклон камеры)
        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -verticalRotationLimit, verticalRotationLimit);
        
        playerCamera.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
    }
    
    private void ProcessInteraction()
    {
        // Raycast для определения объекта под прицелом
        Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, interactionRange, interactionMask))
        {
            // Проверяем, есть ли у объекта компонент InteractableObject
            InteractableObject interactable = hit.collider.GetComponent<InteractableObject>();
            
            if (interactable != null && interactable.IsPlayerInRange(transform))
            {
                // Показываем подсказку
                currentInteractable = interactable;
                interactionPrompt.SetActive(true);
                promptText.text = interactable.GetInteractionText();
                
                // Взаимодействие при нажатии клавиши
                if (Input.GetKeyDown(interactionKey))
                {
                    interactable.Interact();
                }
            }
            else
            {
                // Скрываем подсказку, если объект не интерактивный
                HideInteractionPrompt();
            }
        }
        else
        {
            // Скрываем подсказку, если нет объекта под прицелом
            HideInteractionPrompt();
        }
    }
    
    private void HideInteractionPrompt()
    {
        if (interactionPrompt.activeSelf)
        {
            interactionPrompt.SetActive(false);
            currentInteractable = null;
        }
    }
    
    private void ProcessRealitySwitch()
    {
        // Переключение между реальностями
        if (Input.GetKeyDown(switchRealityKey))
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SwitchGameState();
            }
            else
            {
                Debug.LogWarning("GameManager не найден! Переключение реальностей невозможно.");
            }
        }
    }
    
    // Визуализация радиуса взаимодействия в редакторе
    private void OnDrawGizmosSelected()
    {
        if (playerCamera != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(playerCamera.position, playerCamera.forward * interactionRange);
        }
    }
} 