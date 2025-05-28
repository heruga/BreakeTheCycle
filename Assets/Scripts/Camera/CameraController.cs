using UnityEngine;

/// <summary>
/// Контроллер камеры для изометрического вида в режиме "Сознание"
/// Использует CameraSettings ScriptableObject для настроек
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("Настройки")]
    [SerializeField] private CameraSettings cameraSettings;
    [SerializeField] private Transform targetToFollow; // Цель для слежения (игрок)
    [SerializeField] private Transform cameraTransform;
    
    // Внутренние переменные для отслеживания движения
    private Vector3 lastMoveDirection;
    private Vector3 smoothedMoveDirection;
    private Vector3 targetCameraPosition;
    private Vector3 cameraVelocity = Vector3.zero;
    private Vector3 previousTargetPosition;
    private float timeSinceLastMovement = 0f;
    private bool isReturningToStandardPosition = false;
    private float directionChangeTime = 0f;
    private bool isAbruptDirectionChange = false;
    private float abruptChangeTime = 0f;
    
    // Ссылка на контроллер игрока для получения направления движения
    private ConsciousnessController playerController;
    
    private void Awake()
    {
        // Если камера не назначена, используем основную камеру
        if (cameraTransform == null)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                cameraTransform = mainCamera.transform;
            }
            else
            {
                Debug.LogError("[CameraController] Основная камера не найдена!");
            }
        }
        
        // Если цель не назначена, ищем игрока
        if (targetToFollow == null)
        {
            var player = FindObjectOfType<ConsciousnessController>();
            if (player != null)
            {
                targetToFollow = player.transform;
                playerController = player;
            }
        }
        
        lastMoveDirection = Vector3.forward;
    }
    
    private void Start()
    {
        if (cameraSettings == null)
        {
            Debug.LogError("[CameraController] CameraSettings не назначены! Создайте ScriptableObject CameraSettings.");
            return;
        }
        
        UpdateCameraPosition(true); // Устанавливаем начальную позицию
    }
    
    private void LateUpdate()
    {
        if (cameraSettings == null || targetToFollow == null) return;
        
        UpdateCameraPosition(false);
    }
    
    /// <summary>
    /// Обновляет позицию камеры на основе настроек и движения игрока
    /// </summary>
    /// <param name="instant">Мгновенное позиционирование без плавности</param>
    public void UpdateCameraPosition(bool instant)
    {
        if (cameraSettings == null || targetToFollow == null || cameraTransform == null)
        {
            return;
        }
        
        // Получаем направление движения от контроллера игрока
        Vector3 moveDirection = Vector3.zero;
        if (playerController != null)
        {
            moveDirection = playerController.MoveDirection;
        }
        
        // Обновляем таймер движения
        if (moveDirection != Vector3.zero)
        {
            // Определяем изменение направления движения
            float directionDot = Vector3.Dot(moveDirection.normalized, lastMoveDirection.normalized);
            bool hasChangedDirection = directionDot < 0.7f && lastMoveDirection != Vector3.zero;
            
            // Определяем резкое/противоположное изменение направления
            if (directionDot < -cameraSettings.directionChangeDotThreshold && lastMoveDirection != Vector3.zero)
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
            
            // Применяем сглаживание направления
            if (isAbruptDirectionChange && Time.time - abruptChangeTime < cameraSettings.abruptChangeSmoothing)
            {
                float abruptChangeFactor = (Time.time - abruptChangeTime) / cameraSettings.abruptChangeSmoothing;
                smoothedMoveDirection = Vector3.Slerp(
                    smoothedMoveDirection,
                    moveDirection,
                    Time.deltaTime * (2.0f + abruptChangeFactor * 5.0f)
                );
            }
            else
            {
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
            timeSinceLastMovement += Time.deltaTime;
            smoothedMoveDirection = Vector3.Lerp(smoothedMoveDirection, Vector3.zero, Time.deltaTime * 5.0f);
            isAbruptDirectionChange = false;
        }
        
        // Вычисляем желаемую позицию камеры
        float yOffset = cameraSettings.cameraHeight;
        Vector3 basePosition = targetToFollow.position;
        
        // Получаем стандартное смещение для изометрической камеры
        Vector3 standardOffset = Quaternion.Euler(cameraSettings.cameraAngleX, cameraSettings.cameraAngleY, 0) * 
                                new Vector3(0, 0, -Mathf.Abs(cameraSettings.cameraOffset.z));
        Vector3 cameraPositionOffset = standardOffset;
        
        // Определяем, нужно ли возвращать камеру к стандартному положению
        bool shouldReturnToStandard = timeSinceLastMovement > cameraSettings.cameraReturnTime * 0.25f;
        
        if (shouldReturnToStandard)
        {
            isReturningToStandardPosition = true;
            float returnProgress = (timeSinceLastMovement - cameraSettings.cameraReturnTime * 0.25f) / 
                                 (cameraSettings.cameraReturnTime * 0.75f);
            float returnFactor = Mathf.SmoothStep(0, 1, Mathf.Clamp01(returnProgress));
            
            Vector3 currentOffset = cameraTransform.position - targetToFollow.position;
            currentOffset.y -= yOffset;
            
            cameraPositionOffset = Vector3.Lerp(currentOffset, standardOffset, returnFactor);
            cameraPositionOffset.y = 0;
        }
        
        basePosition += cameraPositionOffset;
        basePosition.y = yOffset;
        Vector3 targetPosition = basePosition;
        
        // Добавляем смещение в направлении движения
        if (cameraSettings.adjustCameraDirection && smoothedMoveDirection != Vector3.zero && !isReturningToStandardPosition)
        {
            float responsivenessFactor = 1f;
            
            if (isAbruptDirectionChange && Time.time - abruptChangeTime < cameraSettings.abruptChangeSmoothing)
            {
                float smoothProgress = (Time.time - abruptChangeTime) / cameraSettings.abruptChangeSmoothing;
                responsivenessFactor = Mathf.SmoothStep(0.2f, 1.0f, smoothProgress);
            }
            else if (Time.time - directionChangeTime < 0.5f)
            {
                responsivenessFactor = Mathf.Lerp(cameraSettings.directionChangeResponsiveness, 1f, 
                    (Time.time - directionChangeTime) / 0.5f);
            }
            
            float adjustmentStrength = cameraSettings.directionAdjustmentStrength * responsivenessFactor;
            float lookAheadFactor = Mathf.Min(adjustmentStrength, cameraSettings.maxLookAheadDistance);
            
            Vector3 lookAheadOffset = new Vector3(
                smoothedMoveDirection.x * lookAheadFactor,
                0,
                smoothedMoveDirection.z * lookAheadFactor
            );
            
            targetPosition += lookAheadOffset;
        }
        
        if (previousTargetPosition == Vector3.zero)
        {
            previousTargetPosition = targetPosition;
        }
        
        // Определяем скорость плавности
        float smoothSpeed = cameraSettings.cameraSmoothSpeed;
        if (isReturningToStandardPosition)
        {
            smoothSpeed *= 0.6f;
        }
        else if (isAbruptDirectionChange && Time.time - abruptChangeTime < cameraSettings.abruptChangeSmoothing * 0.7f)
        {
            smoothSpeed *= 0.85f;
        }
        else if (Time.time - directionChangeTime < 0.3f && !isAbruptDirectionChange)
        {
            smoothSpeed *= 1.5f;
        }
        
        // Применяем движение камеры
        if (cameraSettings.useSpringEffect && !instant)
        {
            if (Time.deltaTime > 0f)
            {
                Vector3 targetVelocity = (targetPosition - previousTargetPosition) / Time.deltaTime;
                
                if (isAbruptDirectionChange)
                {
                    float maxSpeed = 5.0f + (Time.time - abruptChangeTime) / cameraSettings.abruptChangeSmoothing * 15.0f;
                    if (targetVelocity.magnitude > maxSpeed)
                    {
                        targetVelocity = targetVelocity.normalized * maxSpeed;
                    }
                }
                
                float dampingFactor = cameraSettings.cameraDampingStrength;
                if (isAbruptDirectionChange)
                {
                    dampingFactor *= 1.5f;
                }
                
                cameraVelocity = Vector3.Lerp(cameraVelocity, targetVelocity, Time.deltaTime * dampingFactor);
                
                Vector3 dampedPosition = Vector3.SmoothDamp(
                    cameraTransform.position,
                    targetPosition,
                    ref cameraVelocity,
                    1f / smoothSpeed,
                    isAbruptDirectionChange ? 8.0f : Mathf.Infinity,
                    Time.deltaTime
                );
                
                cameraTransform.position = dampedPosition;
            }
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
        
        previousTargetPosition = targetPosition;
        cameraTransform.rotation = Quaternion.Euler(cameraSettings.cameraAngleX, cameraSettings.cameraAngleY, 0);
    }
    
    /// <summary>
    /// Устанавливает новые настройки камеры
    /// </summary>
    public void SetCameraSettings(CameraSettings newSettings)
    {
        cameraSettings = newSettings;
    }
    
    /// <summary>
    /// Устанавливает цель для слежения
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        targetToFollow = newTarget;
        if (newTarget != null)
        {
            playerController = newTarget.GetComponent<ConsciousnessController>();
        }
    }
} 