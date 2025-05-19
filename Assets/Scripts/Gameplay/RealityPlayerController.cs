using UnityEngine;
using TMPro;
using BreakTheCycle;
using BreakTheCycle.Dialogue;
using Inspection;

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
    [SerializeField] private LayerMask interactionMask; // Для обычных интерактивных объектов
    [SerializeField] private LayerMask inspectableLayerMask; // Новая маска для осматриваемых объектов
    [SerializeField] private Camera playerCamera;

    private CharacterController controller;
    private Vector3 playerVelocity;
    private float nextInteractionCheck = 0f;
    private float interactionCheckInterval = 0.1f; // Проверяем взаимодействие каждые 0.1 секунды

    private Transform cachedTransform;
    private IInteractable currentInteractable;
    private BaseInteractable lastBaseInteractable;
    private GameObject lastInspectableObject;

    private bool controlsEnabled = true;

    private bool firstMoveMonologuePlayed = false;

    // Флаг для Debug вывода, чтобы не спамить в консоль каждый кадр
    private GameObject previousHitObject = null;

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
        if (CursorManager.Instance != null)
        {
            CursorManager.Instance.SetGameplayState();
        }
        else
        {
            Debug.LogWarning("[RealityPlayerController] CursorManager.Instance is null. Using fallback cursor state.");
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
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
        // Скрываем обводку, если контроллер отключается
        ClearAllOutlines();
    }

    private void OnControlStateChanged(bool enabled)
    {
        controlsEnabled = enabled;
        if (CursorManager.Instance != null)
        {
            if (enabled)
            {
                CursorManager.Instance.SetGameplayState();
            }
            else
            {
                CursorManager.Instance.SetUIState();
            }
        }
        else
        {
            Debug.LogWarning("[RealityPlayerController] CursorManager.Instance is null in OnControlStateChanged. Using fallback cursor state.");
            // Fallback
            Cursor.visible = !enabled;
            Cursor.lockState = enabled ? CursorLockMode.Locked : CursorLockMode.None;
        }

        if (!enabled)
        {
            playerVelocity = Vector3.zero;
            currentInteractable = null;
            Input.ResetInputAxes();
            ClearAllOutlines();
            previousHitObject = null; // Сбрасываем для Debug
        }
    }

    private void Update()
    {
        if (!controlsEnabled) return;

        HandleMovement();
        HandleInteractionRaycast();
    }

    private void HandleMovement()
    {
        if (controller == null || !controller.enabled) return;

        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");
        Vector3 move = cachedTransform.right * moveX + cachedTransform.forward * moveZ;
        controller.Move(move * speed * Time.deltaTime);
        
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
    }
    
    private void HandleInteractionRaycast()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;
        Debug.DrawRay(ray.origin, ray.direction * interactionRange, Color.cyan);

        IInteractable foundInteractable = null;
        BaseInteractable foundBaseInteractable = null;
        GameObject foundInspectableGameObject = null;
        MonoBehaviour foundInspectableOutlineComponent = null;
        bool canInteractWithFound = false;
        bool newObjectDetectedThisFrame = (previousHitObject == null); // Определяем сразу, если до этого ничего не было

        // Сначала проверяем осматриваемые объекты
        if (Physics.Raycast(ray, out hit, interactionRange, inspectableLayerMask))
        {
            if (previousHitObject != hit.collider.gameObject) 
            {
                Debug.Log($"[RealityPlayerController] Inspectable Raycast HIT: {hit.collider.name} on layer {LayerMask.LayerToName(hit.collider.gameObject.layer)}");
                newObjectDetectedThisFrame = true; // Обновился объект
            }
            previousHitObject = hit.collider.gameObject;
            foundInspectableGameObject = hit.collider.gameObject;

            // Пытаемся получить все MonoBehaviour компоненты и найти среди них InspectableOutline
            var allComponents = hit.collider.GetComponents<MonoBehaviour>();
            bool foundOurOutline = false;
            foreach (var component in allComponents)
            {
                if (component.GetType().Name == "InspectableOutline")
                {
                    if (newObjectDetectedThisFrame || foundInspectableOutlineComponent != component) 
                        Debug.Log($"[RealityPlayerController] Found InspectableOutline component on {hit.collider.name}: {component.GetType().FullName}");
                    foundInspectableOutlineComponent = component;
                    foundOurOutline = true;
                    break; // Нашли нужный, выходим из цикла
                }
            }
            if (!foundOurOutline && newObjectDetectedThisFrame)
            {
                 Debug.LogWarning($"[RealityPlayerController] Did NOT find 'InspectableOutline' component on {hit.collider.name}, though it was hit by inspectable raycast.");
            }
        }
        else
        {
            if (previousHitObject != null && (inspectableLayerMask.value & (1 << previousHitObject.layer)) != 0) 
            {
                 Debug.Log("[RealityPlayerController] Inspectable Raycast MISS. Previously was looking at: " + previousHitObject.name);
                 previousHitObject = null;
                 newObjectDetectedThisFrame = true; // Объект пропал
            }
        }

        // Затем проверяем обычные интерактивные объекты
        bool hitInteractableLayerThisFrame = false;
        if (Physics.Raycast(ray, out hit, interactionRange, interactionMask))
        {
            hitInteractableLayerThisFrame = true;
            if (previousHitObject != hit.collider.gameObject && !newObjectDetectedThisFrame && foundInspectableGameObject != hit.collider.gameObject) 
            {
                 Debug.Log($"[RealityPlayerController] Interaction Raycast HIT: {hit.collider.name} on layer {LayerMask.LayerToName(hit.collider.gameObject.layer)}");
                 newObjectDetectedThisFrame = true; 
            }
            if (foundInspectableGameObject != hit.collider.gameObject) previousHitObject = hit.collider.gameObject;

            IInteractable interactableComponent = hit.collider.GetComponent<IInteractable>();
            if (interactableComponent != null)
            {
                foundInteractable = interactableComponent;
                foundBaseInteractable = interactableComponent as BaseInteractable;

                if (foundBaseInteractable != null)
                {
                    float distance = Vector3.Distance(foundBaseInteractable.transform.position, transform.position);
                    if (distance <= foundBaseInteractable.InteractionRadius)
                    {
                        canInteractWithFound = true;
                    }
                }
                else
                {
                    canInteractWithFound = true; 
                }
            }
        }
        else
        {
            // Логируем промах по Interaction слою только если не было попадания по Inspectable слою И предыдущий объект был на Interaction слое
            if (!foundInspectableGameObject && previousHitObject != null && (interactionMask.value & (1 << previousHitObject.layer)) != 0)
            {
                 Debug.Log("[RealityPlayerController] Interaction Raycast MISS. Previously was looking at: " + previousHitObject.name);
                 previousHitObject = null;
                 newObjectDetectedThisFrame = true; // Объект пропал
            }
        }
        
        // Если луч не попал никуда (ни в inspectable, ни в interactable ИЛИ не было попадания в предыдущем условии)
        if (!foundInspectableGameObject && !hitInteractableLayerThisFrame) 
        {
            if (previousHitObject != null) // Если вообще был какой-то объект до этого
            {
                Debug.Log("[RealityPlayerController] Raycast MISS (both masks). Previously was looking at: " + previousHitObject.name);
                previousHitObject = null;
            }
        }

        // --- Управление обводкой для InspectableObject ---
        if (lastInspectableObject != foundInspectableGameObject)
        {
            if (lastInspectableObject != null)
            {
                // Ищем именно InspectableOutline на старом объекте для скрытия
                var componentsOnOld = lastInspectableObject.GetComponents<MonoBehaviour>();
                foreach(var comp in componentsOnOld)
                {
                    if (comp.GetType().Name == "InspectableOutline")
                    {
                        Debug.Log($"[RealityPlayerController] Hiding InspectableOutline on {lastInspectableObject.name}");
                        var hideMethod = comp.GetType().GetMethod("HideOutline");
                        hideMethod?.Invoke(comp, null);
                        break;
                    }
                }
            }
            lastInspectableObject = foundInspectableGameObject;
        }

        if (foundInspectableOutlineComponent != null) // Теперь эта переменная содержит именно InspectableOutline
        {
            Debug.Log($"[RealityPlayerController] Showing InspectableOutline on {foundInspectableGameObject.name}");
            var showMethod = foundInspectableOutlineComponent.GetType().GetMethod("ShowOutline");
            showMethod?.Invoke(foundInspectableOutlineComponent, null);
        }

        // --- Управление обводкой для BaseInteractable ---
        if (lastBaseInteractable != foundBaseInteractable)
        {
            if (lastBaseInteractable != null)
            {
                 Debug.Log($"[RealityPlayerController] Calling OnPlayerExit for BaseInteractable: {lastBaseInteractable.name}");
                lastBaseInteractable.OnPlayerExit(); 
            }
            lastBaseInteractable = foundBaseInteractable;
        }
        if (foundBaseInteractable != null) 
        {
            Debug.Log($"[RealityPlayerController] Calling OnPlayerEnter for BaseInteractable: {foundBaseInteractable.name}");
            foundBaseInteractable.OnPlayerEnter(); 
        }

        // --- Логика взаимодействия ---
                 currentInteractable = null;
        if (canInteractWithFound && foundInteractable != null) 
        {
            currentInteractable = foundInteractable;
        }

        if (currentInteractable != null && Input.GetKeyDown(interactionKey))
        {
            Debug.Log($"[RealityPlayerController] Interacting with {currentInteractable.GetType().Name} on {((MonoBehaviour)currentInteractable).gameObject.name}");
            currentInteractable.OnInteract();
        }
    }
    
    private void ClearAllOutlines()
    {
        if (lastInspectableObject != null)
        {
            var lastOutline = lastInspectableObject.GetComponent(typeof(MonoBehaviour)) as MonoBehaviour;
            if (lastOutline != null && lastOutline.GetType().Name == "InspectableOutline")
            {
                Debug.Log($"[RealityPlayerController] ClearAllOutlines: Hiding InspectableOutline on {lastInspectableObject.name}");
                var hideMethod = lastOutline.GetType().GetMethod("HideOutline");
                hideMethod?.Invoke(lastOutline, null);
            }
            lastInspectableObject = null;
        }
        if (lastBaseInteractable != null)
        {
            Debug.Log($"[RealityPlayerController] ClearAllOutlines: Calling OnPlayerExit for BaseInteractable: {lastBaseInteractable.name}");
            lastBaseInteractable.OnPlayerExit();
            lastBaseInteractable = null;
        }
        previousHitObject = null; // Также сбрасываем для Debug
    }

    private void OnDestroy()
    {
        ClearAllOutlines();
        // Отписываемся от событий, если это не сделано в OnDisable (на всякий случай)
         if (PlayerControlManager.Instance != null)
            PlayerControlManager.Instance.OnControlStateChanged -= OnControlStateChanged;
    }
} 