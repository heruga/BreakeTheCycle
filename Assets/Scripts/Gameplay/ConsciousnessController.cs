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
    [Tooltip("Высота камеры над игроком")]
    [SerializeField] private float cameraHeight = 15f;
    [Tooltip("Угол наклона камеры (X-ось)")]
    [SerializeField] private float cameraAngleX = 45f;
    [Tooltip("Угол поворота камеры (Y-ось)")]
    [SerializeField] private float cameraAngleY = 45f;
    [Tooltip("Скорость перемещения камеры")]
    [SerializeField] private float cameraSmoothSpeed = 3.5f;
    [Tooltip("Стандартное смещение камеры относительно игрока")]
    [SerializeField] private Vector3 cameraOffset = new Vector3(0f, 15f, -15f);
    [Tooltip("Включить смещение камеры в направлении движения")]
    [SerializeField] private bool adjustCameraDirection = true;
    [Tooltip("Сила смещения камеры в направлении движения")]
    [SerializeField] private float directionAdjustmentStrength = 5f;
    [Tooltip("Время возврата камеры в стандартное положение после остановки игрока")]
    [SerializeField] private float cameraReturnTime = 2.2f;
    [Tooltip("Сила демпфирования для плавного торможения камеры")]
    [SerializeField] private float cameraDampingStrength = 0.85f;
    [Tooltip("Скорость реакции на изменение направления движения")]
    [SerializeField] private float directionChangeResponsiveness = 1.2f;
    [Tooltip("Максимальное расстояние опережения камеры")]
    [SerializeField] private float maxLookAheadDistance = 7f;
    [Tooltip("Применить эффект 'пружины' к движению камеры")]
    [SerializeField] private bool useSpringEffect = true;
    [Tooltip("Порог для определения резкого изменения направления")]
    [SerializeField] private float directionChangeDotThreshold = 0.4f;
    [Tooltip("Время сглаживания резкого изменения направления")]
    [SerializeField] private float abruptChangeSmoothing = 0.8f;
    
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
    private Vector3 lastMoveDirection;
    private Vector3 smoothedMoveDirection;
    private Vector3 targetCameraPosition;
    private Vector3 cameraVelocity = Vector3.zero;
    private Vector3 previousTargetPosition;
    private CharacterController characterController;
    private float timeSinceLastMovement = 0f;
    private bool isReturningToStandardPosition = false;
    private float directionChangeTime = 0f;
    private bool isAbruptDirectionChange = false;
    private float abruptChangeTime = 0f;
    private GameObject createdCamera;
    private Vector3 currentVelocity;
    private Vector3 targetVelocity;
    private EmotionSystem emotionSystem;
    private Animator animator;

    // Поля для отслеживания объектов с активной обводкой
    private HashSet<BaseInteractable> previouslyInRangeInteractables = new HashSet<BaseInteractable>();
    private HashSet<MonoBehaviour> previouslyInRangeInspectables = new HashSet<MonoBehaviour>();
    private HashSet<EnemyHealth> enemiesHitThisDash = new HashSet<EnemyHealth>();
    
    private void Awake()
    {
        SetupPhysics();
        SetupCamera();
        lastMoveDirection = transform.forward;
        enemyLayerMask = 256; // Layer 8 = 256 (1 << 8)
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
        if (cameraTransform == null)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                cameraTransform = mainCamera.transform;
            }
            else
            {
                createdCamera = new GameObject("IsometricCamera");
                cameraTransform = createdCamera.transform;
                Camera newCamera = createdCamera.AddComponent<Camera>();
                newCamera.nearClipPlane = 0.1f;
                createdCamera.AddComponent<AudioListener>();
            }
        }
        
        UpdateCameraPosition(true);
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
            var inspectOutlineComponent = inspectableObject.GetComponent(typeof(MonoBehaviour)) as MonoBehaviour;
            
            // Проверяем по имени типа, чтобы избежать проблем с сборкой
            if (inspectOutlineComponent != null && 
                inspectOutlineComponent.GetType().Name == "InspectableOutline" && 
                mask == inspectableMask) // Убедимся что это именно для Inspectable объектов (клавиша E)
            {
                // Само взаимодействие (осмотр) должно быть частью IInteractable или отдельным методом
                // Здесь мы только пытаемся инициировать взаимодействие. Обводка управляется в UpdateOutlines.
                // Например, если InspectableObject реализует IInteractable:
                (inspectOutlineComponent.GetComponent<IInteractable>())?.OnInteract();
                break; 
            }
        }
    }
    
    // Метод для обновления подсветки объектов в зоне взаимодействия
    private void UpdateOutlines(bool clearAll)
    {
        HashSet<BaseInteractable> currentInRangeInteractables = new HashSet<BaseInteractable>();
        HashSet<MonoBehaviour> currentInRangeInspectables = new HashSet<MonoBehaviour>();

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
                var inspectOutlineComponent = col.GetComponent(typeof(MonoBehaviour)) as MonoBehaviour;
                if (inspectOutlineComponent != null && inspectOutlineComponent.GetType().Name == "InspectableOutline")
                {
                    currentInRangeInspectables.Add(inspectOutlineComponent);
                    if (!previouslyInRangeInspectables.Contains(inspectOutlineComponent))
                    {
                        var showMethod = inspectOutlineComponent.GetType().GetMethod("ShowOutline");
                        showMethod?.Invoke(inspectOutlineComponent, null); // Показать обводку
                    }
                }
            }
        }

        // Скрыть обводку для BaseInteractable, которые вышли из зоны
        foreach (var interactable in previouslyInRangeInteractables)
        {
            if (!currentInRangeInteractables.Contains(interactable))
            {
                interactable.OnPlayerExit(); // Скрыть обводку
            }
        }
        previouslyInRangeInteractables = currentInRangeInteractables;

        // Скрыть обводку для InspectableOutline, которые вышли из зоны
        foreach (var inspectable in previouslyInRangeInspectables)
        {
            if (!currentInRangeInspectables.Contains(inspectable))
            {
                var hideMethod = inspectable.GetType().GetMethod("HideOutline");
                hideMethod?.Invoke(inspectable, null); // Скрыть обводку
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
    
    private void UpdateCameraPosition(bool instant)
    {
        // Обновляем таймер движения
        if (moveDirection != Vector3.zero)
        {
            // Определяем изменение направления движения
            float directionDot = Vector3.Dot(moveDirection.normalized, lastMoveDirection.normalized);
            bool hasChangedDirection = directionDot < 0.7f && lastMoveDirection != Vector3.zero;
            
            // Определяем резкое/противоположное изменение направления (почти противоположное направление)
            if (directionDot < -directionChangeDotThreshold && lastMoveDirection != Vector3.zero)
            {
                isAbruptDirectionChange = true;
                abruptChangeTime = Time.time;
            }
            
            if (hasChangedDirection)
            {
                directionChangeTime = Time.time;
            }

            // Сохраняем последнее ненулевое направление движения
            lastMoveDirection = moveDirection;
            timeSinceLastMovement = 0f;
            isReturningToStandardPosition = false;
            
            // Применяем сглаживание направления, особенно для резких изменений
            if (isAbruptDirectionChange && Time.time - abruptChangeTime < abruptChangeSmoothing)
            {
                // Плавный переход для резких изменений направления
                float abruptChangeFactor = (Time.time - abruptChangeTime) / abruptChangeSmoothing;
                smoothedMoveDirection = Vector3.Slerp(
                    smoothedMoveDirection,
                    moveDirection,
                    Time.deltaTime * (2.0f + abruptChangeFactor * 5.0f)
                );
            }
            else
            {
                // Обычный плавный переход для стандартного движения
                smoothedMoveDirection = Vector3.Slerp(
                    smoothedMoveDirection, 
                    moveDirection, 
                    Time.deltaTime * 8.0f
                );
                isAbruptDirectionChange = false;
            }
        }
        else
        {
            // Увеличиваем таймер, когда игрок не движется
            timeSinceLastMovement += Time.deltaTime;
            
            // Быстрое сглаживание направления к нулю, когда игрок не движется
            smoothedMoveDirection = Vector3.Lerp(smoothedMoveDirection, Vector3.zero, Time.deltaTime * 5.0f);
            isAbruptDirectionChange = false;
        }
        
        // Вычисляем желаемую позицию камеры относительно позиции игрока
        float yOffset = cameraHeight; // Абсолютная высота над землей
        
        // Начинаем с позиции игрока
        Vector3 basePosition = transform.position;
        
        // Вычисляем направление вперёд камеры в мировых координатах (игнорируя Y)
        Vector3 cameraForward = Quaternion.Euler(0, cameraAngleY, 0) * Vector3.forward;
        cameraForward.y = 0;
        cameraForward.Normalize();
        
        // Получаем стандартное смещение для изометрической камеры
        Vector3 standardOffset = Quaternion.Euler(cameraAngleX, cameraAngleY, 0) * new Vector3(0, 0, -Mathf.Abs(cameraOffset.z));
        Vector3 cameraPositionOffset = standardOffset;
        
        // Определяем, нужно ли возвращать камеру к стандартному положению
        bool shouldReturnToStandard = timeSinceLastMovement > cameraReturnTime * 0.25f; // Начинаем возвращать камеру после небольшой задержки
        
        // Если игрок не движется и нужно вернуть камеру, плавно возвращаем к стандартному положению
        if (shouldReturnToStandard)
        {
            isReturningToStandardPosition = true;
            
            // Вычисляем фактор плавного возврата (0 = начало возврата, 1 = полностью вернулись)
            // Используем плавную Smoothstep интерполяцию для более естественного возврата
            float returnProgress = (timeSinceLastMovement - cameraReturnTime * 0.25f) / (cameraReturnTime * 0.75f);
            float returnFactor = Mathf.SmoothStep(0, 1, Mathf.Clamp01(returnProgress));
            
            // Вычисляем текущее смещение камеры от игрока (в локальных координатах относительно направления камеры)
            Vector3 currentOffset = cameraTransform.position - transform.position;
            currentOffset.y -= yOffset; // Убираем высоту, чтобы работать только с горизонтальным смещением
            
            // Интерполируем между текущим смещением и стандартным смещением
            cameraPositionOffset = Vector3.Lerp(currentOffset, standardOffset, returnFactor);
            cameraPositionOffset.y = 0; // Сбрасываем Y-компонент, так как высоту мы устанавливаем отдельно
        }
        
        // Применяем рассчитанное смещение к базовой позиции
        basePosition += cameraPositionOffset;
        
        // Теперь устанавливаем абсолютную высоту
        basePosition.y = yOffset;
        
        Vector3 targetPosition = basePosition;
        
        // Добавляем общее смещение камеры в направлении движения с улучшенной отзывчивостью
        if (adjustCameraDirection && smoothedMoveDirection != Vector3.zero && !isReturningToStandardPosition)
        {
            // Определяем, нужно ли уменьшить отзывчивость при резких изменениях
            float responsivenessFactor = 1f;
            
            // При резком изменении направления уменьшаем силу смещения
            if (isAbruptDirectionChange && Time.time - abruptChangeTime < abruptChangeSmoothing)
            {
                // Постепенно увеличиваем силу смещения по мере завершения сглаживания
                float smoothProgress = (Time.time - abruptChangeTime) / abruptChangeSmoothing;
                responsivenessFactor = Mathf.SmoothStep(0.2f, 1.0f, smoothProgress);
            }
            // Обычное поведение при смене направления
            else if (Time.time - directionChangeTime < 0.5f)
            {
                // Усиливаем эффект на короткое время после обычной смены направления
                responsivenessFactor = Mathf.Lerp(directionChangeResponsiveness, 1f, 
                    (Time.time - directionChangeTime) / 0.5f);
            }
            
            // Используем одинаковую силу смещения для всех направлений движения
            float adjustmentStrength = directionAdjustmentStrength * responsivenessFactor;
            
            // Ограничиваем максимальное расстояние смещения
            float lookAheadFactor = Mathf.Min(adjustmentStrength, maxLookAheadDistance);
            
            // Используем сглаженное направление для расчета опережения камеры
            Vector3 lookAheadOffset = new Vector3(
                smoothedMoveDirection.x * lookAheadFactor,
                0,
                smoothedMoveDirection.z * lookAheadFactor
            );
            
            targetPosition += lookAheadOffset;
        }
        
        // Сохраняем предыдущую целевую позицию, если это первый кадр
        if (previousTargetPosition == Vector3.zero)
        {
            previousTargetPosition = targetPosition;
        }
        
        // Определяем скорость плавности в зависимости от ситуации
        float smoothSpeed;
        if (isReturningToStandardPosition)
        {
            // Более медленный и плавный возврат к стандартному положению
            smoothSpeed = cameraSmoothSpeed * 0.6f;
        }
        else if (isAbruptDirectionChange && Time.time - abruptChangeTime < abruptChangeSmoothing * 0.7f)
        {
            // Медленнее и плавнее при резком изменении направления
            smoothSpeed = cameraSmoothSpeed * 0.85f;
        }
        else if (Time.time - directionChangeTime < 0.3f && !isAbruptDirectionChange)
        {
            // Быстрая реакция при плавном изменении направления движения
            smoothSpeed = cameraSmoothSpeed * 1.5f;
        }
        else
        {
            // Стандартная скорость для всех направлений движения
            smoothSpeed = cameraSmoothSpeed;
        }
        
        // Применяем эффект плавного ускорения и торможения камеры (эффект пружины)
        if (useSpringEffect && !instant)
        {
            if (Time.deltaTime > 0f)
        {
            // Вычисляем разницу между текущей и предыдущей целевыми позициями
            Vector3 targetVelocity = (targetPosition - previousTargetPosition) / Time.deltaTime;
            
            // Ограничиваем макс. скорость камеры при резких изменениях направления
            if (isAbruptDirectionChange)
            {
                // Ограничиваем скорость перемещения камеры при резком изменении направления
                float maxSpeed = 5.0f + (Time.time - abruptChangeTime) / abruptChangeSmoothing * 15.0f;
                if (targetVelocity.magnitude > maxSpeed)
                {
                    targetVelocity = targetVelocity.normalized * maxSpeed;
                }
            }
            
            // Применяем демпфирование к скорости камеры для плавного торможения
            float dampingFactor = cameraDampingStrength;
            if (isAbruptDirectionChange)
            {
                // Увеличиваем демпфирование при резком изменении направления
                dampingFactor *= 1.5f;
            }
            
            cameraVelocity = Vector3.Lerp(cameraVelocity, targetVelocity, Time.deltaTime * dampingFactor);
            
            // Добавляем скорость к текущей позиции камеры с одинаковым поведением для всех направлений
            Vector3 dampedPosition = Vector3.SmoothDamp(
                cameraTransform.position,
                targetPosition,
                ref cameraVelocity,
                1f / smoothSpeed,
                isAbruptDirectionChange ? 8.0f : Mathf.Infinity,  // Ограничение максимальной скорости при резких изменениях
                Time.deltaTime
            );
            
            // Обновляем позицию камеры
            cameraTransform.position = dampedPosition;
            }
            // Если Time.deltaTime == 0f (например, в меню паузы),
            // камера не будет обновляться этим блоком кода, предотвращая ошибку NaN.
            // Она сохранит свою последнюю валидную позицию.
        }
        else if (instant)
        {
            cameraTransform.position = targetPosition;
            cameraVelocity = Vector3.zero;
        }
        else
        {
            cameraTransform.position = Vector3.Lerp(
                cameraTransform.position,
                targetPosition,
                Time.deltaTime * smoothSpeed
            );
        }
        
        // Сохраняем целевую позицию для следующего кадра
        previousTargetPosition = targetPosition;
        
        cameraTransform.rotation = Quaternion.Euler(cameraAngleX, cameraAngleY, 0);
    }
    
    private void Attack()
    {
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

    private void OnDestroy()
    {
        // Очищаем созданные объекты
        if (createdCamera != null)
        {
            Destroy(createdCamera);
        }
    }

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

    private void LateUpdate()
    {
        if (PlayerControlManager.Instance.ControlsEnabled) { // Камера обновляется только если контролы включены
             UpdateCameraPosition(false);
        }
    }
} 