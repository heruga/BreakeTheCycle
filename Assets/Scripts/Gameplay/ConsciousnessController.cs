using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Контроллер игрока для режима "Сознание" с изометрическим видом
/// </summary>
public class ConsciousnessController : MonoBehaviour
{
    [Header("Движение")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float rotationSpeed = 15f;
    [SerializeField] private LayerMask groundLayerMask;
    
    [Header("Физика")]
    [SerializeField] private float mass = 1f;
    [SerializeField] private float drag = 5f;
    [SerializeField] private float angularDrag = 0.05f;
    [SerializeField] private bool useGravity = true;
    [SerializeField] private bool isKinematic = false;
    
    [Header("Камера")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float cameraHeight = 15f;
    [SerializeField] private float cameraAngleX = 45f;
    [SerializeField] private float cameraAngleY = 45f;
    [SerializeField] private float cameraSmoothSpeed = 5f;
    [SerializeField] private Vector3 cameraOffset = new Vector3(-5f, 15f, -15f);
    
    [Header("Взаимодействие")]
    [SerializeField] private float interactionRange = 3f;
    [SerializeField] private KeyCode interactionKey = KeyCode.F;
    [SerializeField] private LayerMask interactionMask;
    [SerializeField] private GameObject interactionPrompt;
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private KeyCode switchRealityKey = KeyCode.R;
    
    private Vector3 moveDirection;
    private Vector3 lastMoveDirection;
    private InteractableObject currentInteractable;
    private Vector3 targetCameraPosition;
    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;
    
    private void Awake()
    {
        SetupPhysics();
        SetupCamera();
        SetupInteractionUI();
        lastMoveDirection = transform.forward;
    }
    
    private void SetupPhysics()
    {
        // Настройка Rigidbody
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        
        rb.mass = mass;
        rb.linearDamping = drag;
        rb.angularDamping = angularDrag;
        rb.useGravity = useGravity;
        rb.isKinematic = isKinematic;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        
        // Настройка CapsuleCollider
        capsuleCollider = GetComponent<CapsuleCollider>();
        if (capsuleCollider == null)
        {
            capsuleCollider = gameObject.AddComponent<CapsuleCollider>();
        }
        
        // Настраиваем размеры коллайдера под размер игрока
        capsuleCollider.height = 2f;
        capsuleCollider.radius = 0.5f;
        capsuleCollider.center = new Vector3(0, 1f, 0);
    }
    
    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    
    private void SetupCamera()
    {
        if (cameraTransform == null)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                cameraTransform = mainCamera.transform;
            }
            else
            {
                GameObject cameraObject = new GameObject("IsometricCamera");
                cameraTransform = cameraObject.transform;
                Camera newCamera = cameraObject.AddComponent<Camera>();
                newCamera.nearClipPlane = 0.1f;
                cameraObject.AddComponent<AudioListener>();
            }
        }
        
        UpdateCameraPosition(true);
    }
    
    private void SetupInteractionUI()
    {
        if (interactionPrompt == null)
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            
            if (canvas == null)
            {
                GameObject canvasObject = new GameObject("ConsciousnessCanvas");
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
            
            Image promptBg = interactionPrompt.AddComponent<Image>();
            promptBg.color = new Color(0, 0, 0, 0.7f);
            
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
            
            interactionPrompt.SetActive(false);
        }
    }
    
    private void Update()
    {
        CalculateMovementDirection();
        ProcessInteraction();
        ProcessRealitySwitch();
        UpdateCameraPosition(false);
    }
    
    private void CalculateMovementDirection()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        
        Vector3 forward = Quaternion.Euler(0, cameraAngleY, 0) * Vector3.forward;
        Vector3 right = Quaternion.Euler(0, cameraAngleY, 0) * Vector3.right;
        
        moveDirection = (forward * vertical + right * horizontal).normalized;
    }
    
    private void FixedUpdate()
    {
        if (moveDirection != Vector3.zero)
        {
            // Поворот персонажа
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.fixedDeltaTime * rotationSpeed);
            
            // Движение через velocity вместо MovePosition
            rb.linearVelocity = moveDirection * moveSpeed;
        }
        else
        {
            // Плавно останавливаем персонажа
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, Time.fixedDeltaTime * drag);
        }
    }
    
    private void UpdateCameraPosition(bool instant)
    {
        // Вычисляем целевую позицию камеры
        Vector3 targetPosition = transform.position + cameraOffset;
        
        if (instant)
        {
            cameraTransform.position = targetPosition;
        }
        else
        {
            // Плавно перемещаем камеру к целевой позиции
            cameraTransform.position = Vector3.Lerp(
                cameraTransform.position,
                targetPosition,
                Time.deltaTime * cameraSmoothSpeed
            );
        }
        
        // Устанавливаем фиксированный угол камеры
        cameraTransform.rotation = Quaternion.Euler(cameraAngleX, cameraAngleY, 0);
    }
    
    private void ProcessInteraction()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, interactionRange, interactionMask);
        bool foundInteractable = false;
        
        foreach (Collider collider in colliders)
        {
            InteractableObject interactable = collider.GetComponent<InteractableObject>();
            if (interactable != null && interactable.IsPlayerInRange(transform))
            {
                currentInteractable = interactable;
                foundInteractable = true;
                
                if (interactionPrompt != null)
                {
                    interactionPrompt.SetActive(true);
                    if (promptText != null)
                    {
                        promptText.text = interactable.GetInteractionText();
                    }
                }
                
                if (Input.GetKeyDown(interactionKey))
                {
                    interactable.Interact();
                }
                
                break;
            }
        }
        
        if (!foundInteractable)
        {
            HideInteractionPrompt();
            currentInteractable = null;
        }
    }
    
    private void HideInteractionPrompt()
    {
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(false);
        }
    }
    
    private void ProcessRealitySwitch()
    {
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
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
} 