using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BreakTheCycle;

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
    [SerializeField] private float rotationSmoothSpeed = 15f; // Скорость сглаживания поворота
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
    
    [Header("UI")]
    [SerializeField] private InteractionPromptUI interactionPromptUI;
    [SerializeField] private Sprite interactionPromptSprite;
    [SerializeField] private Vector2 promptSize = new Vector2(200, 50);
    
    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;
    private float verticalRotation = 0f;
    private float targetVerticalRotation = 0f;
    private float targetHorizontalRotation = 0f;
    private InteractableObject currentInteractable;
    private bool isControlEnabled = true; // Флаг для блокировки управления
    
    private GameObject createdCamera;
    private GameObject createdGroundCheck;
    private Canvas createdCanvas;
    
    private Vector2 mouseInput;

    private Transform cachedTransform;
    private Vector3 currentEulerAngles;
    private float currentVerticalRotation;

    private float nextGroundCheck = 0f;
    private float groundCheckInterval = 0.1f; // Проверяем землю каждые 0.1 секунды
    private float nextInteractionCheck = 0f;
    private float interactionCheckInterval = 0.1f; // Проверяем взаимодействие каждые 0.1 секунды

    private Vector3 moveDirection;
    private Vector3 cachedRight;
    private Vector3 cachedForward;

    private void Awake()
    {
        // Кэшируем компоненты
        cachedTransform = transform;
        controller = GetComponent<CharacterController>();
        currentEulerAngles = transform.eulerAngles;
        targetHorizontalRotation = currentEulerAngles.y;
        
        // Кэшируем векторы для движения
        moveDirection = Vector3.zero;
        cachedRight = Vector3.right;
        cachedForward = Vector3.forward;
        
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
                createdCamera = new GameObject("FirstPersonCamera");
                createdCamera.transform.SetParent(transform);
                createdCamera.transform.localPosition = new Vector3(0, 1.6f, 0);
                playerCamera = createdCamera.transform;
                
                Camera newCamera = createdCamera.AddComponent<Camera>();
                newCamera.nearClipPlane = 0.1f;
                
                // Добавляем аудиослушатель
                createdCamera.AddComponent<AudioListener>();
            }
        }
        
        // Настройка проверки земли
        if (groundCheck == null)
        {
            createdGroundCheck = new GameObject("GroundCheck");
            createdGroundCheck.transform.SetParent(transform);
            createdGroundCheck.transform.localPosition = new Vector3(0, -0.9f, 0);
            groundCheck = createdGroundCheck.transform;
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
        if (interactionPromptUI == null)
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            
            if (canvas == null)
            {
                GameObject canvasObject = new GameObject("PlayerCanvas");
                createdCanvas = canvasObject.AddComponent<Canvas>();
                createdCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObject.AddComponent<CanvasScaler>();
                canvasObject.AddComponent<GraphicRaycaster>();
                canvas = createdCanvas;
            }
            
            // Создаем префаб подсказки
            GameObject promptPrefab = new GameObject("InteractionPrompt");
            promptPrefab.transform.SetParent(canvas.transform, false);
            
            // Добавляем компоненты
            promptPrefab.AddComponent<CanvasGroup>();
            RectTransform promptRect = promptPrefab.AddComponent<RectTransform>();
            promptRect.anchorMin = new Vector2(0.5f, 0.2f);
            promptRect.anchorMax = new Vector2(0.5f, 0.2f);
            promptRect.sizeDelta = promptSize;
            promptRect.anchoredPosition = Vector2.zero;
            
            // Фон
            Image promptBg = promptPrefab.AddComponent<Image>();
            promptBg.color = new Color(1, 1, 1, 1);
            promptBg.sprite = interactionPromptSprite;
            promptBg.type = Image.Type.Simple;
            promptBg.preserveAspect = true;
            promptBg.raycastTarget = false;
            
            // Добавляем компонент управления UI
            interactionPromptUI = promptPrefab.AddComponent<InteractionPromptUI>();
            interactionPromptUI.backgroundImage = promptBg;
            
            promptPrefab.SetActive(false);
        }
    }
    
    private void Update()
    {
        if (!isControlEnabled) return;
        
        float currentTime = Time.time;
        
        // Проверка касания земли с интервалом
        if (currentTime >= nextGroundCheck)
        {
            isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
            nextGroundCheck = currentTime + groundCheckInterval;
            
            if (isGrounded && velocity.y < 0)
            {
                velocity.y = -2f;
            }
        }
        
        // Обработка вращения камеры
        ProcessCameraRotation();
        
        // Обработка ввода для движения
        ProcessMovementInput();
        
        // Проверка взаимодействия с интервалом
        if (currentTime >= nextInteractionCheck)
        {
            ProcessInteraction();
            nextInteractionCheck = currentTime + interactionCheckInterval;
        }
        
        // Обработка переключения реальностей
        if (Input.GetKeyDown(switchRealityKey))
        {
            ProcessRealitySwitch();
        }
    }
    
    private void FixedUpdate()
    {
        if (!isControlEnabled) return;
    }
    
    private void ProcessMovementInput()
    {
        if (!isControlEnabled) return;

        // Получаем ввод пользователя
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        // Определяем скорость в зависимости от спринта
        float speed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : walkSpeed;

        // Обновляем направление движения без создания новых векторов
        moveDirection.x = x;
        moveDirection.z = z;
        moveDirection.y = 0;

        if (moveDirection.x != 0 || moveDirection.z != 0)
        {
            // Нормализуем вектор движения только если он не нулевой
            moveDirection.Normalize();
            
            // Применяем поворот и скорость
            moveDirection = cachedTransform.TransformDirection(moveDirection) * speed * Time.deltaTime;
            
            // Добавляем вертикальную составляющую
            moveDirection.y = velocity.y * Time.deltaTime;
            
            // Применяем движение
            controller.Move(moveDirection);
        }
        else if (velocity.y != 0)
        {
            // Если нет горизонтального движения, но есть вертикальное
            moveDirection.x = 0;
            moveDirection.z = 0;
            moveDirection.y = velocity.y * Time.deltaTime;
            controller.Move(moveDirection);
        }

        // Обработка прыжка
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpForce * 2f * gravity);
        }

        // Применяем гравитацию
        velocity.y -= gravity * Time.deltaTime;
    }
    
    private void ProcessCameraRotation()
    {
        if (!isControlEnabled) return;

        // Получаем ввод мыши и масштабируем его
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Обновляем целевые углы поворота
        targetHorizontalRotation += mouseX;
        targetVerticalRotation = Mathf.Clamp(targetVerticalRotation - mouseY, -verticalRotationLimit, verticalRotationLimit);

        // Плавно интерполируем текущие углы к целевым
        currentEulerAngles.y = Mathf.LerpAngle(currentEulerAngles.y, targetHorizontalRotation, Time.deltaTime * rotationSmoothSpeed);
        currentVerticalRotation = Mathf.Lerp(currentVerticalRotation, targetVerticalRotation, Time.deltaTime * rotationSmoothSpeed);

        // Применяем повороты без создания новых векторов
        cachedTransform.eulerAngles = currentEulerAngles;
        playerCamera.localRotation = Quaternion.Euler(currentVerticalRotation, 0f, 0f);
    }
    
    private void ProcessInteraction()
    {
        if (!isControlEnabled || interactionPromptUI == null) return;
        
        Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        RaycastHit hit;
        InteractableObject interactable = null;
        
        bool hasInteractable = Physics.Raycast(ray, out hit, interactionRange, interactionMask) &&
                              hit.collider.TryGetComponent(out interactable) &&
                              interactable != null &&
                              interactable.IsPlayerInRange(transform);
        
        if (hasInteractable)
        {
            if (currentInteractable != interactable)
            {
                currentInteractable = interactable;
                UpdateInteractionPrompt();
            }
            
            if (Input.GetKeyDown(interactionKey))
            {
                interactable.Interact();
            }
        }
        else if (currentInteractable != null)
        {
            HideInteractionPrompt();
        }
    }
    
    public void HideInteractionPrompt()
    {
        if (interactionPromptUI != null && interactionPromptUI.gameObject.activeSelf)
        {
            interactionPromptUI.HidePrompt();
            currentInteractable = null;
        }
    }
    
    private void ProcessRealitySwitch()
    {
        // Переключение между реальностями
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SwitchGameState();
        }
        else
        {
            Debug.LogWarning("GameManager не найден! Переключение реальностей невозможно.");
        }
    }
    
    private void UpdateInteractionPrompt()
    {
        if (interactionPromptUI == null) return;
        
        if (currentInteractable != null)
        {
            if (!interactionPromptUI.gameObject.activeSelf)
            {
                interactionPromptUI.gameObject.SetActive(true);
            }
            interactionPromptUI.ShowPrompt(currentInteractable.GetInteractionText());
        }
        else if (interactionPromptUI.gameObject.activeSelf)
        {
            interactionPromptUI.HidePrompt();
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

    public void SetControlEnabled(bool enabled)
    {
        isControlEnabled = enabled;
        
        // Управление курсором
        Cursor.visible = !enabled;
        Cursor.lockState = enabled ? CursorLockMode.Locked : CursorLockMode.None;
        
        // Сбрасываем ввод при отключении управления
        if (!enabled)
        {
            // Сбрасываем скорость движения
            velocity = Vector3.zero;
            
            // Сбрасываем текущее взаимодействие
            if (currentInteractable != null)
            {
                HideInteractionPrompt();
            }
            
            // Сбрасываем ввод мыши
            Input.ResetInputAxes();
        }
        
        Debug.Log($"PlayerController: Управление {(enabled ? "включено" : "отключено")}");
    }

    // Получение текущей позиции камеры
    public Vector3 GetCameraPosition()
    {
        if (playerCamera != null)
        {
            return playerCamera.position;
        }
        return Vector3.zero;
    }

    // Установка позиции камеры
    public void SetCameraPosition(Vector3 position)
    {
        if (playerCamera != null)
        {
            playerCamera.position = position;
        }
    }

    public Quaternion GetCameraRotation()
    {
        if (playerCamera != null)
        {
            return playerCamera.rotation;
        }
        return Quaternion.identity;
    }

    public void SetCameraRotation(Quaternion rotation)
    {
        if (playerCamera != null)
        {
            playerCamera.rotation = rotation;
        }
    }

    private void OnDestroy()
    {
        // Очищаем созданные объекты
        if (createdCamera != null)
        {
            Destroy(createdCamera);
        }
        if (createdGroundCheck != null)
        {
            Destroy(createdGroundCheck);
        }
        if (createdCanvas != null)
        {
            Destroy(createdCanvas.gameObject);
        }
        if (interactionPromptUI != null)
        {
            Destroy(interactionPromptUI.gameObject);
        }
    }
} 