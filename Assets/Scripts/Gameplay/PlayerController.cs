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
    
    [Header("UI")]
    [SerializeField] private InteractionPromptUI interactionPromptUI;
    [SerializeField] private Sprite interactionPromptSprite;
    [SerializeField] private Vector2 promptSize = new Vector2(200, 50);
    
    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;
    private float verticalRotation = 0f;
    private InteractableObject currentInteractable;
    private bool isControlEnabled = true; // Флаг для блокировки управления
    
    private GameObject createdCamera;
    private GameObject createdGroundCheck;
    private Canvas createdCanvas;
    
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
        if (!isControlEnabled) return; // Если управление отключено, пропускаем обновление

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
        if (!isControlEnabled) return; // Блокируем движение, если управление отключено
        
        // Получаем ввод пользователя
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        
        // Если управление отключено, обнуляем ввод
        if (!isControlEnabled)
        {
            x = 0;
            z = 0;
        }
        
        // Определяем скорость в зависимости от спринта
        float speed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : walkSpeed;
        
        // Создаем вектор движения относительно направления взгляда
        Vector3 move = transform.right * x + transform.forward * z;
        
        // Применяем движение
        controller.Move(move * speed * Time.deltaTime);
        
        // Прыжок
        if (Input.GetButtonDown("Jump") && isGrounded && isControlEnabled)
        {
            velocity.y = Mathf.Sqrt(jumpForce * 2f * gravity);
        }
        
        // Применяем гравитацию
        velocity.y -= gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
    
    private void ProcessCameraRotation()
    {
        if (!isControlEnabled) return; // Блокируем вращение камеры, если управление отключено
        
        // Получаем ввод мыши
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        
        // Если управление отключено, обнуляем ввод
        if (!isControlEnabled)
        {
            mouseX = 0;
            mouseY = 0;
        }
        
        // Вращение вокруг вертикальной оси (поворот персонажа)
        transform.Rotate(Vector3.up * mouseX);
        
        // Вращение вокруг горизонтальной оси (наклон камеры)
        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -verticalRotationLimit, verticalRotationLimit);
        
        playerCamera.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
    }
    
    private void ProcessInteraction()
    {
        if (!isControlEnabled) return; // Блокируем взаимодействие, если управление отключено
        
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
                UpdateInteractionPrompt();
                
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
    
    private void UpdateInteractionPrompt()
    {
        if (currentInteractable != null)
        {
            if (!interactionPromptUI.gameObject.activeSelf)
            {
                interactionPromptUI.gameObject.SetActive(true);
            }
            interactionPromptUI.ShowPrompt(currentInteractable.GetInteractionText());
        }
        else
        {
            if (interactionPromptUI.gameObject.activeSelf)
            {
                interactionPromptUI.HidePrompt();
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