using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BreakTheCycle;

/// <summary>
/// Контроллер игрока для режима "Сознание" с изометрическим видом
/// </summary>
public class ConsciousnessController : MonoBehaviour
{
    [Header("Движение")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float rotationSpeed = 15f;
    [SerializeField] private LayerMask groundLayerMask;
    
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
    [SerializeField] private float interactionRange = 3f;
    [SerializeField] private KeyCode interactionKey = KeyCode.F;
    [SerializeField] private LayerMask interactionMask;
    [SerializeField] private GameObject interactionPrompt;
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private KeyCode switchRealityKey = KeyCode.R;
    
    [Header("Бой")]
    [SerializeField] private float attackDamage = 1f;
    [SerializeField] private float attackSpeed = 1f;
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private float chaseDistance = 10f;
    [SerializeField] private float attackDistance = 2f;
    [SerializeField] private float stopDistance = 1f;
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
    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;
    private float timeSinceLastMovement = 0f;
    private bool isReturningToStandardPosition = false;
    private float directionChangeTime = 0f;
    private bool isAbruptDirectionChange = false;
    private float abruptChangeTime = 0f;
    private GameObject createdCamera;
    private Canvas createdCanvas;
    
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
        
        rb.mass = 1.5f;  // Увеличиваем массу для большей стабильности
        rb.linearDamping = 5f;    // Значительно увеличиваем сопротивление для предотвращения скольжения
        rb.angularDamping = 10f;  // Сильно увеличиваем сопротивление вращению
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate; // Интерполяция для плавного движения
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous; // Улучшаем определение коллизий
        rb.freezeRotation = true;  // Полностью замораживаем вращение
        rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;  // Замораживаем все оси вращения и Y позицию
        
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
        capsuleCollider.isTrigger = false; // Убеждаемся, что коллайдер не триггер
        
        // Устанавливаем позицию персонажа на правильную высоту
        Vector3 position = transform.position;
        position.y = 1f; // Устанавливаем высоту равной половине высоты коллайдера
        transform.position = position;
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
            // Поворот персонажа только когда есть ввод от игрока
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * rotationSpeed);
            
            // Движение через velocity с учетом текущего поворота персонажа
            rb.linearVelocity = moveDirection * moveSpeed;
        }
        else
        {
            // Плавно останавливаем персонажа
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, Time.fixedDeltaTime * 10f);
            
            // Если персонаж почти остановился, полностью сбрасываем скорость
            if (rb.linearVelocity.magnitude < 0.1f)
            {
                rb.linearVelocity = Vector3.zero;
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
        Vector3 halfExtents = new Vector3(interactionRange * 0.5f, interactionRange * 0.5f, interactionRange * 0.5f);
        Collider[] colliders = Physics.OverlapBox(transform.position, halfExtents, Quaternion.identity, interactionMask);
        
        InteractableObject nearestInteractable = null;
        float nearestDistance = float.MaxValue;
        
        foreach (Collider collider in colliders)
        {
            // Пропускаем объекты с компонентом DoorInteraction
            if (collider.GetComponent<DoorInteraction>() != null)
            {
                continue;
            }
            
            InteractableObject interactable = collider.GetComponent<InteractableObject>();
            if (interactable != null)
            {
                float distance = Vector3.Distance(transform.position, collider.transform.position);
                if (distance <= interactionRange && distance < nearestDistance)
                {
                    nearestInteractable = interactable;
                    nearestDistance = distance;
                }
            }
        }
        
        if (nearestInteractable != null)
        {
            currentInteractable = nearestInteractable;
            ShowInteractionPrompt(nearestInteractable.GetInteractionText());
            
            if (Input.GetKeyDown(interactionKey))
            {
                nearestInteractable.Interact();
            }
        }
        else
        {
            HideInteractionPrompt();
            currentInteractable = null;
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
        Gizmos.DrawWireSphere(transform.position, attackDistance);
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