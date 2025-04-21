using UnityEngine;

namespace Inspection
{
public class InspectionCamera : MonoBehaviour
{
        [Header("Camera Settings")]
        [SerializeField] private float rotationSpeed = 2f;
        [SerializeField] private float minVerticalAngle = -80f; // Минимальный угол вращения по вертикали
        [SerializeField] private float maxVerticalAngle = 80f;  // Максимальный угол вращения по вертикали
        
        [Header("Zoom Settings")]
        [SerializeField] private float zoomSpeed = 2f;
        [SerializeField] private float minDistance = 2f;
        [SerializeField] private float maxDistance = 10f;
        [SerializeField] private float initialDistance = 5f;
        
        [SerializeField] private bool showDebug = true;

        [Header("Background Settings")]
        [SerializeField] private Color backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
        [SerializeField] private bool isolatedView = true;
        [SerializeField] private LayerMask inspectableLayer;

        [Header("Camera Positioning")]
        [SerializeField] private float offsetX = -0.3f; // Смещение от -1 до 1, где 0 - центр
        [SerializeField] private bool useOffset = true;

        private Camera cam;
        private Transform target;
        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private float currentDistance;
        private bool isInspecting = false;
        private InspectableObject[] allInspectableObjects;
        private GameObject currentTarget;
        private Vector3 lookAtPoint;
        private Vector3 originalPosition;
        private Quaternion originalRotation;
        private float currentVerticalAngle = 0f;
        private float currentHorizontalAngle = 0f;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            if (cam == null)
            {
                Debug.LogError("Camera component not found on InspectionCamera!");
                enabled = false;
                return;
            }

            SaveInitialTransform();
            SetupCamera();
            FindAllInspectableObjects();
        }

        private void FindAllInspectableObjects()
        {
            allInspectableObjects = FindObjectsOfType<InspectableObject>();
            if (showDebug) Debug.Log($"Found {allInspectableObjects.Length} inspectable objects in scene");
        }

        private void SetupCamera()
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = backgroundColor;
            UpdateCameraCullingMask();
            
            // Настраиваем матрицу проекции камеры
            Matrix4x4 m = cam.projectionMatrix;
            m[0, 2] = offsetX; // Смещаем проекцию по X
            cam.projectionMatrix = m;
            
            if (showDebug) Debug.Log("Camera setup completed with isolated view settings");
        }

        private void UpdateCameraCullingMask()
        {
            if (isolatedView)
            {
                cam.cullingMask = inspectableLayer;
            }
            else
            {
                cam.cullingMask = ~(1 << LayerMask.NameToLayer("UI"));
            }
        }

        private void SaveInitialTransform()
        {
            initialPosition = transform.position;
            initialRotation = transform.rotation;
            if (showDebug) Debug.Log($"Saved initial transform - Pos: {initialPosition}, Rot: {initialRotation.eulerAngles}");
        }

        private Vector3 CalculateCameraPosition(Vector3 targetPosition, float viewportX)
        {
            // Вычисляем позицию камеры так, чтобы объект оказался в нужной точке экрана
            Vector3 basePosition = targetPosition + Vector3.back * initialDistance;
            float horizontalOffset = (viewportX - 0.5f) * initialDistance * 2f;
            return basePosition + Vector3.right * horizontalOffset;
        }

        public void StartInspecting(GameObject target)
        {
            if (target == null || !target.TryGetComponent<InspectableObject>(out var inspectable))
            {
                Debug.LogError("Target object is not inspectable!");
                return;
            }

            if (!target.activeInHierarchy)
            {
                Debug.LogError($"Target object {target.name} is not active in hierarchy!");
                return;
            }

            isInspecting = true;
            currentTarget = target;
            this.target = target.transform;
            
            // Сбрасываем углы вращения
            currentVerticalAngle = 0f;
            currentHorizontalAngle = 0f;
            
            // Сохраняем начальное положение объекта
            originalPosition = target.transform.position;
            originalRotation = target.transform.rotation;
            
            // Устанавливаем точку, на которую смотрит камера
            lookAtPoint = target.transform.position;
            
            // Устанавливаем начальное расстояние и ограничения зума из настроек объекта
            currentDistance = initialDistance * inspectable.defaultZoomValue;
            minDistance = initialDistance * inspectable.minMaxZoom.x;
            maxDistance = initialDistance * inspectable.minMaxZoom.y;
            
            if (showDebug) Debug.Log($"Zoom settings - Min: {minDistance}, Max: {maxDistance}, Current: {currentDistance}");
            
            // Устанавливаем смещение матрицы проекции
            float projectionOffset = useOffset ? offsetX : 0f;
            if (inspectable.spawnPositionOffset != Vector3.zero)
            {
                projectionOffset = inspectable.spawnPositionOffset.x / 5f;
            }
            
            Matrix4x4 m = cam.projectionMatrix;
            m[0, 2] = projectionOffset;
            cam.projectionMatrix = m;
            
            // Устанавливаем позицию камеры с учетом начального расстояния
            transform.position = lookAtPoint + Vector3.back * currentDistance;
            transform.LookAt(lookAtPoint);
            
            // Включаем камеру
            if (cam != null)
            {
                cam.enabled = true;
                UpdateCameraCullingMask();
                if (showDebug) Debug.Log($"Camera setup - Position: {transform.position}, LookAt: {lookAtPoint}, ProjectionOffset: {projectionOffset}");
            }
            
            // Применяем начальный поворот из настроек
            target.transform.rotation = Quaternion.Euler(inspectable.spawnRotationOffset);
            
            // Находим все инспектируемые объекты и скрываем их
            FindAllInspectableObjects();
            foreach (var obj in allInspectableObjects)
            {
                if (obj.gameObject != target)
                {
                    obj.gameObject.SetActive(false);
                }
            }
        }

        public void StopInspecting()
        {
            isInspecting = false;
            
            if (isolatedView)
            {
                foreach (var obj in allInspectableObjects)
                {
                    obj.gameObject.SetActive(true);
                }
            }
            
            // Возвращаем предмет в исходное положение и поворот
            if (currentTarget != null)
            {
                currentTarget.transform.position = originalPosition;
                currentTarget.transform.rotation = originalRotation;
            }
            
            target = null;
            
            if (cam != null)
            {
                cam.enabled = false;
                if (showDebug) Debug.Log("Inspection camera disabled");
            }

            transform.position = initialPosition;
            transform.rotation = initialRotation;
            
            if (showDebug) Debug.Log("Stopped inspection, returned to initial position");
        }
    
        private void Update()
        {
            if (!isInspecting || target == null) return;

            HandleRotation();
            HandleZoom();
        }

        private void HandleRotation()
        {
            if (Input.GetMouseButton(1))
            {
                float mouseX = Input.GetAxis("Mouse X") * rotationSpeed;
                float mouseY = Input.GetAxis("Mouse Y") * rotationSpeed;
                
                // Обновляем углы
                currentHorizontalAngle += mouseX;
                currentVerticalAngle = Mathf.Clamp(currentVerticalAngle - mouseY, minVerticalAngle, maxVerticalAngle);
                
                // Вычисляем новую позицию камеры
                Vector3 direction = Quaternion.Euler(currentVerticalAngle, currentHorizontalAngle, 0) * Vector3.back;
                transform.position = lookAtPoint + direction * currentDistance;
                transform.LookAt(lookAtPoint);
                
                if (showDebug) Debug.Log($"Rotating camera - Vertical: {currentVerticalAngle}, Horizontal: {currentHorizontalAngle}");
            }
        }

        private void HandleZoom()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0)
            {
                // Сохраняем текущее направление от объекта к камере
                Vector3 directionToCamera = (transform.position - lookAtPoint).normalized;
                
                // Изменяем дистанцию с учетом ограничений из InspectableObject
                currentDistance = Mathf.Clamp(currentDistance - scroll * zoomSpeed, minDistance, maxDistance);
                
                // Устанавливаем новую позицию камеры, сохраняя направление
                transform.position = lookAtPoint + directionToCamera * currentDistance;
                
                if (showDebug) Debug.Log($"Zooming - Distance: {currentDistance}, Min: {minDistance}, Max: {maxDistance}");
            }
        }
    }
}
