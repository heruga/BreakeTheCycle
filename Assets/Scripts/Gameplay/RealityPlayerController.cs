using UnityEngine;
using TMPro;
using BreakTheCycle;

/// <summary>
/// Контроллер игрока для режима "Реальность" с видом от первого лица
/// </summary>
public class RealityPlayerController : MonoBehaviour
{
    [Header("Движение")]
    [SerializeField] private float walkSpeed = 5f;
    
    [Header("Взаимодействие")]
    [SerializeField] private float interactionRange = 3f;
    [SerializeField] private KeyCode interactionKey = KeyCode.F;
    [SerializeField] private LayerMask interactionMask;
    
    private CharacterController controller;
    private Vector3 velocity;
    private float nextInteractionCheck = 0f;
    private float interactionCheckInterval = 0.1f; // Проверяем взаимодействие каждые 0.1 секунды

    private Transform cachedTransform;
    private InteractableObject currentInteractable;

    private bool controlsEnabled = true;

    private void Awake()
    {
        // Кэшируем компоненты
        cachedTransform = transform;
        controller = GetComponent<CharacterController>();
        
        // Создаем компоненты, если их нет
        if (controller == null)
        {
            controller = gameObject.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.5f;
            controller.center = new Vector3(0, 0.9f, 0);
        }
    }
    
    private void Start()
    {
        // Блокируем и скрываем курсор
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnEnable()
    {
        if (PlayerControlManager.Instance != null)
            PlayerControlManager.Instance.OnControlStateChanged += OnControlStateChanged;
        controlsEnabled = PlayerControlManager.Instance == null || PlayerControlManager.Instance.ControlsEnabled;
    }

    private void OnDisable()
    {
        if (PlayerControlManager.Instance != null)
            PlayerControlManager.Instance.OnControlStateChanged -= OnControlStateChanged;
    }

    private void OnControlStateChanged(bool enabled)
    {
        controlsEnabled = enabled;
        Cursor.visible = !enabled;
        Cursor.lockState = enabled ? CursorLockMode.Locked : CursorLockMode.None;
        if (!enabled)
        {
            velocity = Vector3.zero;
            currentInteractable = null;
            Input.ResetInputAxes();
        }
    }

    private void Update()
    {
        if (!controlsEnabled) return;
        // Базовое движение игрока
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");
        Vector3 move = cachedTransform.right * moveX + cachedTransform.forward * moveZ;
        controller.Move(move * walkSpeed * Time.deltaTime);
        
        // Поиск интерактивного объекта перед игроком
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;
        InteractableObject interactable = null;
        
        bool hasInteractable = Physics.Raycast(ray, out hit, interactionRange, interactionMask) &&
                              hit.collider.TryGetComponent(out interactable) &&
                              interactable != null &&
                              interactable.IsPlayerInRange(transform);
        
        if (hasInteractable)
        {
            currentInteractable = interactable;
            if (Input.GetKeyDown(interactionKey))
            {
                interactable.OnInteract();
            }
        }
        else
        {
            currentInteractable = null;
        }
    }

    private void OnDestroy()
    {
        // Нет UI для очистки
    }
} 