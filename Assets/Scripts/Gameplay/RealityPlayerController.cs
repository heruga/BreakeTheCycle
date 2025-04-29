using UnityEngine;
using TMPro;
using BreakTheCycle;
using BreakTheCycle.Dialogue;

/// <summary>
/// Контроллер игрока для режима "Реальность" с видом от первого лица
/// </summary>
public class RealityPlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 5.0f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float jumpHeight = 1.5f;

    [Header("Interaction")]
    [SerializeField] private float interactionRange = 2.0f;
    [SerializeField] private KeyCode interactionKey = KeyCode.F;
    [SerializeField] private LayerMask interactionMask;
    [SerializeField] private Camera playerCamera; // Добавляем поле для камеры

    private CharacterController controller;
    private Vector3 playerVelocity;
    private float nextInteractionCheck = 0f;
    private float interactionCheckInterval = 0.1f; // Проверяем взаимодействие каждые 0.1 секунды

    private Transform cachedTransform;
    private IInteractable currentInteractable;

    private bool controlsEnabled = true;

    private bool firstMoveMonologuePlayed = false;

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
            playerVelocity = Vector3.zero;
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
        controller.Move(move * speed * Time.deltaTime);
        
        // Первая реплика при первом движении
        if (!firstMoveMonologuePlayed && (Mathf.Abs(moveX) > 0.01f || Mathf.Abs(moveZ) > 0.01f))
        {
            var trigger = GetComponent<BreakTheCycle.Dialogue.MonologueTriggerData>();
            if (trigger != null && trigger.monologueID >= 0)
            {
                var manager = FindObjectOfType<BreakTheCycle.Dialogue.MonologueManager>();
                if (manager != null)
                {
                    manager.PlayMonologue(trigger.monologueID);
                    firstMoveMonologuePlayed = true;
                }
            }
        }
        
        // Поиск интерактивного объекта перед игроком
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;
        IInteractable interactable = null;
        bool hasInteractable = false;

        if (Physics.Raycast(ray, out hit, interactionRange, interactionMask))
        {
            interactable = hit.collider.GetComponent<IInteractable>();
            if (interactable != null)
            {
                // Если это BaseInteractable, проверяем радиус
                var baseInteractable = interactable as BaseInteractable;
                if (baseInteractable != null)
                {
                    float distance = Vector3.Distance(baseInteractable.transform.position, transform.position);
                    if (distance <= baseInteractable.InteractionRadius)
                    {
                        hasInteractable = true;
                    }
                }
                else
                {
                    // Если не BaseInteractable, считаем, что объект доступен
                    hasInteractable = true;
                }
            }
        }
        
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
             if (currentInteractable != null)
             {
                 currentInteractable = null;
             }
        }
    }

    private void OnDestroy()
    {
        // Нет UI для очистки
    }
} 