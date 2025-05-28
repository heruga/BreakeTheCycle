using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BreakTheCycle;
using UnityEngine.SceneManagement;
using DungeonGeneration.Scripts;
using DungeonGeneration.Scripts.Health;
using UnityEngine.EventSystems;
using Inspection;
using System.Collections.Generic;
using BreakTheCycle.Dialogue;

/// <summary>
/// Контроллер игрока для режима "Сознание" с изометрическим видом
/// </summary>
public class ConsciousnessController : MonoBehaviour
{
    [Header("Движение")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float rotationSpeed = 15f;
    [SerializeField] private float accelerationSpeed = 8f; // Скорость ускорения
    [SerializeField] private float decelerationSpeed = 12f; // Скорость замедления
    [Header("Dash")]
    [SerializeField] private float dashSpeed = 20f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 1f;
    private bool isDashing = false;
    private float dashTimeLeft = 0f;
    private float lastDashTime = -10f;
    
    [Header("Камера")]
    [SerializeField] private Transform cameraTransform;
    
    [Header("Взаимодействие")]
    [SerializeField] private float interactionRange = 2f;
    [SerializeField] private LayerMask interactionMask;
    [SerializeField] private LayerMask inspectableMask;
    
    [Header("Бой")]
    [SerializeField] private float attackDamage = 80f;
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private float attackRadius = 199f; // Радиус области атаки
    [SerializeField] private LayerMask enemyLayerMask = 256; // Layer 8 = 256 (1 << 8)
    [SerializeField] private float dashAttackDamage = 20f; // Урон во время дэша
    private float lastAttackTime;

    [Header("Визуализация")]
    [SerializeField] private Color chaseGizmoColor = Color.red;
    [SerializeField] private Color attackGizmoColor = Color.yellow;
    
    private Vector3 moveDirection;
    private CharacterController characterController;
    private Vector3 currentVelocity;
    private Vector3 targetVelocity;
    private EmotionSystem emotionSystem;
    private Animator animator;

    // Публичное свойство для доступа к направлению движения
    public Vector3 MoveDirection => moveDirection;

    // Поля для отслеживания объектов с активной обводкой
    private HashSet<BaseInteractable> previouslyInRangeInteractables = new HashSet<BaseInteractable>();
    private HashSet<Inspection.InspectableOutline> previouslyInRangeInspectables = new HashSet<Inspection.InspectableOutline>();
    private HashSet<EnemyHealth> enemiesHitThisDash = new HashSet<EnemyHealth>();
    
    private void Awake()
    {
        SetupPhysics();
        SetupCamera();
        animator = GetComponentInChildren<Animator>();
        Debug.Log($"[ConsciousnessController] Animator component: {(animator == null ? "NOT FOUND" : "FOUND")}");
        
        Debug.Log($"[ConsciousnessController] Awake: controlsEnabled = {PlayerControlManager.Instance?.ControlsEnabled}");
    }
    
    private void SetupPhysics()
    {
        // Получаем CharacterController
        characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
            characterController = gameObject.AddComponent<CharacterController>();
        }
        
        // Устанавливаем позицию персонажа на правильную высоту
        Vector3 position = transform.position;
        // position.y = 1f; // Эту строку тоже стоит проверить, нужна ли она, если центр берется из инспектора
        transform.position = position;
    }
    
    private void Start()
    {
        if (CursorManager.Instance != null)
        {
            CursorManager.Instance.SetGameplayState();
        }
        else
        {
            Debug.LogWarning("[ConsciousnessController] CursorManager.Instance is null. Using fallback cursor state.");
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        }

        if (PlayerControlManager.Instance != null)
            PlayerControlManager.Instance.SetControlsEnabled(true);
        Debug.Log($"[ConsciousnessController] Start: controlsEnabled = {PlayerControlManager.Instance?.ControlsEnabled}");
        emotionSystem = FindObjectOfType<EmotionSystem>();
    }
    
    private void SetupCamera()
    {
        // Базовая настройка камеры - основная логика теперь в CameraController
        if (cameraTransform == null)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                cameraTransform = mainCamera.transform;
                Debug.Log("[ConsciousnessController] Использую основную камеру");
            }
            else
            {
                Debug.LogWarning("[ConsciousnessController] Основная камера не найдена! Убедитесь, что в сцене есть камера с тегом MainCamera.");
            }
        }
    }
    
    private void OnEnable()
    {
        if (PlayerControlManager.Instance != null)
            PlayerControlManager.Instance.OnControlStateChanged += OnControlStateChanged;
    }

    private void OnDisable()
    {
        if (PlayerControlManager.Instance != null)
            PlayerControlManager.Instance.OnControlStateChanged -= OnControlStateChanged;
    }

    private void OnControlStateChanged(bool enabled)
    {
        Debug.Log($"[ConsciousnessController] OnControlStateChanged: controlsEnabled = {enabled}");
        if (!enabled)
        {
            currentVelocity = Vector3.zero;
            targetVelocity = Vector3.zero;
            isDashing = false;
            if (animator != null)
            {
                animator.SetFloat("ActualSpeed", 0f);
            }
            // Можно добавить сброс ввода, если потребуется
        }
    }
    
    private void Update()
    {
        if (!PlayerControlManager.Instance.ControlsEnabled)
        {
            UpdateOutlines(true); // Вызываем с флагом "очистить все", если контролы выключены
            return;
        }

        // Обработка ввода для движения относительно камеры
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");
        Vector3 inputDirection = new Vector3(moveX, 0, moveZ);
        if (inputDirection.magnitude > 0.1f && cameraTransform != null)
        {
            Vector3 camForward = cameraTransform.forward;
            camForward.y = 0;
            camForward.Normalize();
            Vector3 camRight = cameraTransform.right;
            camRight.y = 0;
            camRight.Normalize();
            moveDirection = (camForward * moveZ + camRight * moveX).normalized;
        }
        else
        {
            moveDirection = Vector3.zero;
        }

        // Dash теперь обрабатывается в Update
        if (Input.GetKeyDown(KeyCode.Space) && moveDirection != Vector3.zero && Time.time >= lastDashTime + dashCooldown)
        {
            StartDash();
        }
        
        // Обновляем подсветку объектов
        UpdateOutlines(false);

        // Взаимодействие с объектами (F — Interactable, E — Inspectable)
        if (Input.GetKeyDown(KeyCode.F))
        {
            TryInteractWithMask(interactionMask);
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            TryInteractWithMask(inspectableMask);
        }

        // Логика атаки
        if (Input.GetMouseButtonDown(0))
        {
            bool blockAttack = false;

            // 1. Сначала проверяем, не был ли клик на активном элементе UI
            if (EventSystem.current.IsPointerOverGameObject()) 
            {
                Debug.Log("[ConsciousnessController] Attack prevented: Click was on an active UI element.");
                blockAttack = true;
            }
            // 2. Если не на активном UI, проверяем, не включилось ли управление только что
            else if (PlayerControlManager.Instance != null && PlayerControlManager.Instance.JustBecameEnabledThisFrame)
            {
                Debug.Log("[ConsciousnessController] Attack prevented: Controls were just enabled this frame.");
                blockAttack = true;
            }

            // Атакуем, только если не было причин блокировать.
            // Проверка PlayerControlManager.Instance.ControlsEnabled здесь уже избыточна, 
            // так как в начале Update() есть return, если ControlsEnabled == false.
            // Но для дополнительной уверенности можно оставить, вреда не будет.
            if (!blockAttack && (PlayerControlManager.Instance == null || PlayerControlManager.Instance.ControlsEnabled) ) 
            {
                Debug.Log("[ConsciousnessController] Попытка атаки (нажатие ЛКМ)");
                Attack();
            }
        }
    }
    
    private void TryInteractWithMask(LayerMask mask)
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, interactionRange, mask);
        
        // Обрабатываем все коллайдеры в радиусе взаимодействия
        foreach (var col in colliders)
        {
            // Проверяем, является ли объект интерактивным
            var interactable = col.GetComponent<IInteractable>();
            if (interactable != null)
            {
                // Для BaseInteractable вызываем событие вхождения в зону (обводка уже должна быть обработана в UpdateOutlines)
                // var baseInteractable = interactable as BaseInteractable;
                // if (baseInteractable != null)
                // {
                //     baseInteractable.OnPlayerEnter(); // OnPlayerEnter здесь не должен отвечать за обводку напрямую
                // }
                
                interactable.OnInteract();
                break; // Взаимодействуем только с одним объектом за раз
            }
            
            // Проверяем, является ли объект осматриваемым
            // Используем GameObject.GetComponent вместо прямого вызова для избежания проблем с типом
            var inspectableObject = col.gameObject;
            Inspection.InspectableOutline inspectOutlineComponentTyped = inspectableObject.GetComponent<Inspection.InspectableOutline>();
            
            // Проверяем по имени типа, чтобы избежать проблем с сборкой - теперь прямой тип
            if (inspectOutlineComponentTyped != null && 
                mask == inspectableMask) // Убедимся что это именно для Inspectable объектов (клавиша E)
            {
                // Само взаимодействие (осмотр) должно быть частью IInteractable или отдельным методом
                // Здесь мы только пытаемся инициировать взаимодействие. Обводка управляется в UpdateOutlines.
                // Например, если InspectableObject реализует IInteractable:
                (inspectOutlineComponentTyped.GetComponent<IInteractable>())?.OnInteract(); // или col.GetComponent<IInteractable>()?.OnInteract();
                break; 
            }
        }
    }
    
    // Метод для обновления подсветки объектов в зоне взаимодействия
    private void UpdateOutlines(bool clearAll)
    {
        HashSet<BaseInteractable> currentInRangeInteractables = new HashSet<BaseInteractable>();
        HashSet<Inspection.InspectableOutline> currentInRangeInspectables = new HashSet<Inspection.InspectableOutline>();

        if (!clearAll) // Если контролы включены, ищем объекты в зоне
        {
            // --- Обработка BaseInteractable ---
            Collider[] interactableColliders = Physics.OverlapSphere(transform.position, interactionRange, interactionMask);
            foreach (var col in interactableColliders)
            {
                var baseInteractable = col.GetComponent<BaseInteractable>();
                if (baseInteractable != null)
                {
                    currentInRangeInteractables.Add(baseInteractable);
                    if (!previouslyInRangeInteractables.Contains(baseInteractable))
                    {
                        baseInteractable.OnPlayerEnter(); // Показать обводку
                    }
                }
            }

            // --- Обработка InspectableOutline ---
            Collider[] inspectableColliders = Physics.OverlapSphere(transform.position, interactionRange, inspectableMask);
            foreach (var col in inspectableColliders)
            {
                Inspection.InspectableOutline inspectOutlineTyped = col.GetComponent<Inspection.InspectableOutline>();
                if (inspectOutlineTyped != null)
                {
                    currentInRangeInspectables.Add(inspectOutlineTyped);
                    if (!previouslyInRangeInspectables.Contains(inspectOutlineTyped))
                    {
                        inspectOutlineTyped.ShowOutline(); // Прямой вызов
                    }
                }
            }
        }

        // Скрыть обводку для BaseInteractable, которые вышли из зоны
        foreach (var interactable in previouslyInRangeInteractables)
        {
            if (interactable != null && !currentInRangeInteractables.Contains(interactable))
            {
                interactable.OnPlayerExit(); // Скрыть обводку
            }
        }
        previouslyInRangeInteractables = currentInRangeInteractables;

        // Скрыть обводку для InspectableOutline, которые вышли из зоны
        foreach (Inspection.InspectableOutline inspectableTyped in previouslyInRangeInspectables)
        {
            if (inspectableTyped != null && !currentInRangeInspectables.Contains(inspectableTyped))
            {
                inspectableTyped.HideOutline(); // Прямой вызов
            }
        }
        previouslyInRangeInspectables = currentInRangeInspectables;
    }
    
    private void FixedUpdate()
    {
        if (!PlayerControlManager.Instance.ControlsEnabled) return;

        // Удаляю обработку dash из FixedUpdate
        if (isDashing)
        {
            HandleDash();
        }
        else
        {
            HandleMovement();
        }

        // Поворот модели игрока
        if (moveDirection != Vector3.zero && !isDashing) // Не поворачиваем во время dash
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }

        // UpdateCameraPosition(false); // Убрано, так как вызывается в LateUpdate
    }

    private void HandleMovement()
    {
        float currentMoveSpeed = moveSpeed;
        if (emotionSystem != null)
        {
            float speedModifier = emotionSystem.GetEmotionSpeedModifier();
            currentMoveSpeed *= (1f + speedModifier);
        }

        if (moveDirection != Vector3.zero)
        {
            // Ускорение
            targetVelocity = moveDirection * currentMoveSpeed;
            currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, accelerationSpeed * Time.fixedDeltaTime);
        }
        else
        {
            // Замедление
            currentVelocity = Vector3.Lerp(currentVelocity, Vector3.zero, decelerationSpeed * Time.fixedDeltaTime);
        }

        // Получаем текущую фактическую скорость персонажа
        float actualSpeed = currentVelocity.magnitude;

        if (animator != null)
        {
            // Передаем фактическую скорость в параметр "ActualSpeed" аниматора
            animator.SetFloat("ActualSpeed", actualSpeed);
            // Debug.Log("[ConsciousnessController] Animator: ActualSpeed = " + actualSpeed); // Оставить для отладки, если нужно
        }
        
        // Применяем движение
        characterController.Move(currentVelocity * Time.fixedDeltaTime);
    }

    private void HandleDash()
    {
        if (dashTimeLeft > 0)
        {
            characterController.Move(moveDirection * dashSpeed * Time.fixedDeltaTime);
            dashTimeLeft -= Time.fixedDeltaTime;

            // Логика атаки во время дэша
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, attackRadius, enemyLayerMask);
            float damage = dashAttackDamage;
            if (emotionSystem != null)
            {
                damage = emotionSystem.ModifyOutgoingDamage(damage); // Модифицируем урон от дэша эмоциями
            }

            foreach (var hitCollider in hitColliders)
            {
                EnemyHealth enemy = hitCollider.GetComponent<EnemyHealth>();
                if (enemy == null)
                    enemy = hitCollider.GetComponentInParent<EnemyHealth>();

                if (enemy != null && !enemiesHitThisDash.Contains(enemy))
                {
                    float prevHealth = enemy.GetCurrentHealth();
                    enemy.TakeDamage(damage);
                    enemiesHitThisDash.Add(enemy);
                    float newHealth = enemy.GetCurrentHealth();
                    Debug.Log($"[Dash Attack] Враг {enemy.gameObject.name}: урон {damage}. Было HP={prevHealth}, стало HP={newHealth}");

                    if (newHealth <= 0 && emotionSystem != null)
                    {
                        emotionSystem.OnEnemyDefeated();
                    }
                }
            }
        }
        else
        {
            if (animator != null)
            {
                animator.SetBool("isDashing", false);
            }
            isDashing = false;
            currentVelocity = Vector3.zero;
        }
    }

    private void StartDash()
    {
        if (moveDirection == Vector3.zero) return;
                isDashing = true;
                dashTimeLeft = dashDuration;
                lastDashTime = Time.time;
        enemiesHitThisDash.Clear(); // Очищаем список атакованных врагов при начале нового дэша
        if (animator != null)
        {
            animator.SetBool("isDashing", true);
            Debug.Log("[ConsciousnessController] Animator: isDashing set to true");
        }
    }
    
    private void Attack()
    {
        // Проверяем, открыт ли монолог - в этом случае блокируем атаку
        MonologueManager monologueManager = FindObjectOfType<MonologueManager>();
        if (monologueManager != null && monologueManager.IsMonologueActive)
        {
            Debug.Log("[ConsciousnessController] Атака заблокирована: открыт монолог");
            return;
        }
        
        if (Time.time < lastAttackTime + attackCooldown)
        {
            Debug.Log($"[Attack] Не атакуем: кулдаун. Time={Time.time:F2}, lastAttackTime={lastAttackTime:F2}, attackCooldown={attackCooldown}");
                    return;
        }
        lastAttackTime = Time.time;

        if (animator != null)
        {
            animator.SetTrigger("isAttack");
            Debug.Log("[ConsciousnessController] Animator: isAttack triggered");
        }

        Debug.Log($"[Attack] Вызван. enemyLayerMask={enemyLayerMask.value}, attackRadius={attackRadius}");

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, attackRadius, enemyLayerMask);
        Debug.Log($"[Attack] Найдено коллайдеров: {hitColliders.Length}");

        float damage = attackDamage;
        // Модифицируем урон с учетом эффектов эмоций
        if (emotionSystem != null)
        {
            damage = emotionSystem.ModifyOutgoingDamage(damage);
        }
        Debug.Log($"[Attack] Базовый урон: {attackDamage}, После модификаций: {damage}");

        foreach (var hitCollider in hitColliders)
        {
            EnemyHealth enemy = hitCollider.GetComponent<EnemyHealth>();
            if (enemy == null)
                enemy = hitCollider.GetComponentInParent<EnemyHealth>();

            if (enemy != null)
            {
                float prevHealth = enemy.GetCurrentHealth();
                enemy.TakeDamage(damage);
                float newHealth = enemy.GetCurrentHealth();
                Debug.Log($"[Attack] Враг {enemy.gameObject.name}: урон {damage}. Было HP={prevHealth}, стало HP={newHealth}");

                // Если враг умер, вызываем обработку победы
                if (newHealth <= 0 && emotionSystem != null)
                {
                    emotionSystem.OnEnemyDefeated();
                }
            }
            else
            {
                Debug.LogWarning($"[Attack] Не найден EnemyHealth у объекта {hitCollider.name} (родитель: {hitCollider.transform.parent?.name})");
            }
        }
    }
    
    #if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Отрисовка радиуса взаимодействия (OverlapSphere)
        Gizmos.color = Color.green; // Зеленый цвет для области взаимодействия
        Gizmos.DrawWireSphere(transform.position, interactionRange);

        // Отрисовка радиуса атаки
        Gizmos.color = attackGizmoColor;
        Gizmos.DrawWireSphere(transform.position, attackRadius);
    }
    #endif

    // Методы для интеграции с системой эмоций
    
    /// Обрабатывает получение урона с учетом эмоций
    public float TakeDamage(float damage)
    {
        return damage;
    }
    
    /// Проверяет, должен ли игрок уклониться от атаки
    public bool ShouldDodgeAttack()
    {
        return false;
    }
    
    /// Вызывается при входе в новую комнату
    public void OnRoomEntered()
    {
        if (emotionSystem != null)
        {
            emotionSystem.OnEnterNewRoom();
        }
    }
    
    /// Вызывается при победе над врагом
    public void OnEnemyDefeated()
    {
        // Empty implementation
    }
    
    /// Пытается воскресить игрока после смерти
    public bool TryRevive()
    {
        return false;
    }
} 