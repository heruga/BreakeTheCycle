using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BreakTheCycle;
using UnityEngine.SceneManagement;
using DungeonGeneration.Scripts;

/// <summary>
/// Контроллер игрока для режима "Сознание" с изометрическим видом
/// </summary>
public class ConsciousnessController : MonoBehaviour
{
    [Header("Движение")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float rotationSpeed = 15f;
    [SerializeField] private LayerMask groundLayerMask;
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
    [SerializeField] private Vector3 interactionBoxSize = new Vector3(2f, 2f, 2f);
    [SerializeField] private LayerMask interactionMask;
    [SerializeField] private KeyCode interactionKey = KeyCode.F;
    [SerializeField] private GameObject interactionPrompt;
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private KeyCode switchRealityKey = KeyCode.R;
    
    [Header("Бой")]
    [SerializeField] private float attackDamage = 80f;
    [SerializeField] private float attackSpeed = 1f;
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private float chaseDistance = 10f;
    [SerializeField] private float attackDistance = 2f;
    [SerializeField] private float attackRadius = 2f; // Радиус области атаки
    [SerializeField] private LayerMask enemyLayerMask = 256; // Layer 8 = 256 (1 << 8)
    private float lastAttackTime;

    [Header("Визуализация")]
    [SerializeField] private Color chaseGizmoColor = Color.red;
    [SerializeField] private Color attackGizmoColor = Color.yellow;
    
    private Vector3 moveDirection;
    private Vector3 lastMoveDirection;
    private Vector3 smoothedMoveDirection;
    private InteractableObject currentInteractable;
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
    private Canvas createdCanvas;
    private Vector3 currentVelocity;
    private Vector3 targetVelocity;
    
    private void Awake()
    {
        SetupPhysics();
        SetupCamera();
        SetupInteractionUI();
        lastMoveDirection = transform.forward;
        enemyLayerMask = 256; // Layer 8 = 256 (1 << 8)
    }
    
    private void SetupPhysics()
    {
        // Получаем CharacterController
        characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
            characterController = gameObject.AddComponent<CharacterController>();
        }
        
        // Настраиваем размеры CharacterController
        characterController.height = 2f;
        characterController.radius = 0.5f;
        characterController.center = new Vector3(0, 1f, 0);
        characterController.slopeLimit = 45f;
        characterController.stepOffset = 0.3f;
        
        // Устанавливаем позицию персонажа на правильную высоту
        Vector3 position = transform.position;
        position.y = 1f;
        transform.position = position;
    }
    
    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
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
    
    private void SetupInteractionUI()
    {
        if (interactionPrompt == null)
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            
            if (canvas == null)
            {
                GameObject canvasObject = new GameObject("ConsciousnessCanvas");
                createdCanvas = canvasObject.AddComponent<Canvas>();
                createdCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObject.AddComponent<CanvasScaler>();
                canvasObject.AddComponent<GraphicRaycaster>();
                canvas = createdCanvas;
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
        if (Input.GetKeyDown(interactionKey))
        {
            Debug.Log("[ConsciousnessController] Нажата клавиша F");
            ProcessInteraction();
        }

        if (Input.GetMouseButtonDown(0) && Time.time >= lastAttackTime + attackCooldown)
        {
            Attack();
        }

        CalculateMovementDirection();
        ProcessRealitySwitch();
        UpdateCameraPosition(false);
    }
    
    private void CalculateMovementDirection()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        
        Vector3 forward = Quaternion.Euler(0, cameraAngleY, 0) * Vector3.forward;
        Vector3 right = Quaternion.Euler(0, cameraAngleY, 0) * Vector3.right;
        
        Vector3 targetDirection = (forward * vertical + right * horizontal).normalized;
        
        // Плавное изменение направления движения
        moveDirection = Vector3.Lerp(moveDirection, targetDirection, Time.deltaTime * 10f);
    }
    
    private void FixedUpdate()
    {
        // Вычисляем целевую скорость
        targetVelocity = moveDirection * (isDashing ? dashSpeed : moveSpeed);

        // Плавное ускорение/замедление
        float speedToUse = targetVelocity.magnitude > currentVelocity.magnitude ? accelerationSpeed : decelerationSpeed;
        currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, Time.fixedDeltaTime * speedToUse);

        if (currentVelocity.magnitude > 0.1f)
        {
            // Поворот персонажа
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * rotationSpeed);
            
            // Движение через CharacterController
            characterController.Move(currentVelocity * Time.fixedDeltaTime);
        }

        // Обработка dash
        if (isDashing)
        {
            dashTimeLeft -= Time.fixedDeltaTime;
            if (dashTimeLeft <= 0)
            {
                isDashing = false;
                // Плавное замедление после dash
                currentVelocity = currentVelocity.normalized * moveSpeed;
            }
        }
        else if (moveDirection.magnitude > 0.1f)
        {
            // Проверяем возможность выполнить dash
            if (Input.GetKey(KeyCode.LeftShift) && Time.time >= lastDashTime + dashCooldown)
            {
                isDashing = true;
                dashTimeLeft = dashDuration;
                lastDashTime = Time.time;
                Debug.Log("[ConsciousnessController] Выполняем dash!");
            }
        }
    }
    
    private void UpdateCameraPosition(bool instant)
    {
        // Обновляем таймер движения
        if (moveDirection != Vector3.zero)
        {
            // Detect direction change
            float directionDot = Vector3.Dot(moveDirection.normalized, lastMoveDirection.normalized);
            bool hasChangedDirection = directionDot < 0.7f && lastMoveDirection != Vector3.zero;
            
            // Detect abrupt/opposite direction change (close to opposite direction)
            if (directionDot < -directionChangeDotThreshold && lastMoveDirection != Vector3.zero)
            {
                isAbruptDirectionChange = true;
                abruptChangeTime = Time.time;
            }
            
            if (hasChangedDirection)
            {
                directionChangeTime = Time.time;
            }

            // Save the last non-zero movement direction
            lastMoveDirection = moveDirection;
            timeSinceLastMovement = 0f;
            isReturningToStandardPosition = false;
            
            // Apply direction smoothing especially for abrupt changes
            if (isAbruptDirectionChange && Time.time - abruptChangeTime < abruptChangeSmoothing)
            {
                // Gradually transition for abrupt direction changes
                float abruptChangeFactor = (Time.time - abruptChangeTime) / abruptChangeSmoothing;
                smoothedMoveDirection = Vector3.Slerp(
                    smoothedMoveDirection,
                    moveDirection,
                    Time.deltaTime * (2.0f + abruptChangeFactor * 5.0f)
                );
            }
            else
            {
                // Normal smooth transition for typical movement
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
            
            // Quickly smooth direction to zero when not moving
            smoothedMoveDirection = Vector3.Lerp(smoothedMoveDirection, Vector3.zero, Time.deltaTime * 5.0f);
            isAbruptDirectionChange = false;
        }
        
        // Calculate desired camera position based on player position
        float yOffset = cameraHeight; // Absolute height from ground
        
        // Start with player position
        Vector3 basePosition = transform.position;
        
        // Calculate the forward direction of the camera in world space (ignoring Y)
        Vector3 cameraForward = Quaternion.Euler(0, cameraAngleY, 0) * Vector3.forward;
        cameraForward.y = 0;
        cameraForward.Normalize();
        
        // Get the base offset for the isometric camera - это стандартный офсет
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
        
        // Apply the calculated offset to the base position
        basePosition += cameraPositionOffset;
        
        // Now set the absolute height
        basePosition.y = yOffset;
        
        Vector3 targetPosition = basePosition;
        
        // Add general direction adjustment for movement with improved responsiveness
        if (adjustCameraDirection && lastMoveDirection != Vector3.zero && !isReturningToStandardPosition)
        {
            // Determine if we need to reduce responsiveness during abrupt changes
            float responsivenessFactor = 1f;
            
            // При резком изменении направления уменьшаем силу смещения
            if (isAbruptDirectionChange && Time.time - abruptChangeTime < abruptChangeSmoothing)
            {
                // Постепенно увеличиваем силу смещения по мере завершения сглаживания
                float smoothProgress = (Time.time - abruptChangeTime) / abruptChangeSmoothing;
                responsivenessFactor = Mathf.SmoothStep(0.2f, 1.0f, smoothProgress);
            }
            // Normal direction change behavior
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
        else if (instant)
        {
            cameraTransform.position = targetPosition;
            cameraVelocity = Vector3.zero;
        }
        else
        {
            // Smooth camera movement with appropriate speed
            cameraTransform.position = Vector3.Lerp(
                cameraTransform.position,
                targetPosition,
                Time.deltaTime * smoothSpeed
            );
        }
        
        // Сохраняем целевую позицию для следующего кадра
        previousTargetPosition = targetPosition;
        
        // Calculate rotation to look at player
        // First set fixed angle for isometric view
        cameraTransform.rotation = Quaternion.Euler(cameraAngleX, cameraAngleY, 0);
    }
    
    private void ProcessInteraction()
    {
        // Поиск порталов без учета слоя
        var portalColliders = Physics.OverlapSphere(transform.position, interactionRange);
        foreach (var collider in portalColliders)
        {
            var portal = collider.GetComponent<InteractablePortal>();
            if (portal == null)
            {
                portal = collider.GetComponentInParent<InteractablePortal>();
            }
            
            if (portal != null)
            {
                Debug.Log($"[ConsciousnessController] Найден портал: {collider.name}, слой: {collider.gameObject.layer}, расстояние: {Vector3.Distance(transform.position, collider.transform.position)}");
                if (portal.CheckInteractionRange(transform.position))
                {
                    Debug.Log("[ConsciousnessController] Вызываем OnInteract() у портала");
                    portal.OnInteract();
                    return;
                }
            }
        }
    }
    
    private void ShowInteractionPrompt(string text)
    {
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(true);
            if (promptText != null)
            {
                promptText.text = text;
            }
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
    
    private void Attack()
    {
        lastAttackTime = Time.time;

        // Получаем всех врагов в радиусе атаки
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, attackRadius, enemyLayerMask);

        foreach (var hitCollider in hitColliders)
        {
            // Сначала проверяем компонент на самом объекте
            var enemyHealth = hitCollider.GetComponent<DungeonGeneration.Scripts.Health.EnemyHealth>();
            if (enemyHealth == null)
            {
                // Если на самом объекте нет, ищем в родителе
                enemyHealth = hitCollider.GetComponentInParent<DungeonGeneration.Scripts.Health.EnemyHealth>();
            }

            if (enemyHealth != null)
            {
                float previousHealth = enemyHealth.GetCurrentHealth();
                enemyHealth.TakeDamage(attackDamage);
                Debug.Log($"<color=red>Нанесен урон {attackDamage} врагу {hitCollider.transform.parent.name}. Здоровье: {previousHealth} -> {enemyHealth.GetCurrentHealth()}</color>");
            }
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        // Отрисовка радиуса взаимодействия
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);

        // Отрисовка радиуса преследования
        Gizmos.color = chaseGizmoColor;
        Gizmos.DrawWireSphere(transform.position, chaseDistance);

        // Отрисовка радиуса атаки
        Gizmos.color = attackGizmoColor;
        Gizmos.DrawWireSphere(transform.position, attackRadius);
    }

    private void OnDestroy()
    {
        // Очищаем созданные объекты
        if (createdCamera != null)
        {
            Destroy(createdCamera);
        }
        if (createdCanvas != null)
        {
            Destroy(createdCanvas.gameObject);
        }
        if (interactionPrompt != null)
        {
            Destroy(interactionPrompt);
        }
    }
} 