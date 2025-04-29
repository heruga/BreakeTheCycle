using UnityEngine;

namespace Inspection
{
public class InspectionCamera : MonoBehaviour
{
        [Header("Camera Settings")]
        [SerializeField] private float rotationSpeed = 2f;
        [SerializeField] private float rotationSpeedRightMouse = 5f; // Скорость вращения для правой кнопки мыши
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

        [Header("Lighting")]
        [SerializeField] private Light dedicatedInspectionLight;

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
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
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

            if (dedicatedInspectionLight != null)
            {
                dedicatedInspectionLight.enabled = false;
            }
        }

        private void FindAllInspectableObjects()
        {
            allInspectableObjects = FindObjectsOfType<InspectableObject>();
            if (showDebug) Debug.Log($"Найдено объектов для осмотра: {allInspectableObjects.Length}");
        }

        private void SetupCamera()
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = backgroundColor;
            
            Matrix4x4 m = cam.projectionMatrix;
            m[0, 2] = offsetX;
            cam.projectionMatrix = m;
            
            if (showDebug) Debug.Log("Настройка камеры завершена");
        }

        private void SaveInitialTransform()
        {
            initialPosition = transform.position;
            initialRotation = transform.rotation;
            if (showDebug) Debug.Log($"Сохранён начальный трансформ — Позиция: {initialPosition}, Поворот: {initialRotation.eulerAngles}");
        }

        private Vector3 CalculateCameraPosition(Vector3 targetPosition, float viewportX)
        {
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
            
            currentVerticalAngle = 0f;
            currentHorizontalAngle = 0f;
            
            originalPosition = target.transform.position;
            originalRotation = target.transform.rotation;
            
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                foreach (Renderer r in renderers)
                    bounds.Encapsulate(r.bounds);
                lookAtPoint = bounds.center + inspectable.spawnPositionOffset;
                Debug.Log($"[InspectionCamera] pivot: {target.transform.position}, bounds.center: {bounds.center}, lookAtPoint (с offset): {lookAtPoint}");
            }
            else
            {
                lookAtPoint = target.transform.position + inspectable.spawnPositionOffset;
                Debug.Log($"[InspectionCamera] pivot: {target.transform.position}, bounds.center: (нет рендереров), lookAtPoint (с offset): {lookAtPoint}");
            }
            
            currentDistance = initialDistance * inspectable.defaultZoomValue;
            minDistance = initialDistance * inspectable.minMaxZoom.x;
            maxDistance = initialDistance * inspectable.minMaxZoom.y;
            
            if (showDebug) Debug.Log($"Настройки зума — Мин: {minDistance}, Макс: {maxDistance}, Текущий: {currentDistance}");
            
            float projectionOffset = useOffset ? offsetX : 0f;
            if (inspectable.spawnPositionOffset != Vector3.zero)
            {
                projectionOffset = inspectable.spawnPositionOffset.x / 5f;
            }
            
            Matrix4x4 m = cam.projectionMatrix;
            m[0, 2] = projectionOffset;
            cam.projectionMatrix = m;
            
            Vector3 direction = Quaternion.Euler(currentVerticalAngle, currentHorizontalAngle, 0) * Vector3.back;
            transform.position = lookAtPoint + direction * currentDistance;
            transform.LookAt(lookAtPoint);
            
            if (cam != null)
            {
                cam.enabled = true;
                cam.cullingMask = inspectableLayer;
                if (showDebug)
                {
                    string layerName = LayerMask.LayerToName(Mathf.RoundToInt(Mathf.Log(inspectableLayer.value, 2)));
                    Debug.Log($"Настройка камеры — Позиция: {transform.position}, Смотрит на: {lookAtPoint}, Смещение проекции: {projectionOffset}, Culling Mask: {layerName}");
                 }
            }
            
            if (dedicatedInspectionLight != null)
            {
                dedicatedInspectionLight.enabled = true;
                if (showDebug) Debug.Log($"Выделенный свет '{dedicatedInspectionLight.name}' включен.");
            }
            
            target.transform.rotation = Quaternion.Euler(inspectable.spawnRotationOffset);
            
            if (isolatedView)
            {
                FindAllInspectableObjects();
                foreach (var obj in allInspectableObjects)
                {
                    if (obj.gameObject != target)
                    {
                        obj.gameObject.SetActive(false);
                        if (showDebug) Debug.Log($"Скрыт другой объект: {obj.gameObject.name}");
                    }
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
                    if (obj != null && obj.gameObject != null)
                    {
                        obj.gameObject.SetActive(true);
                        if (showDebug) Debug.Log($"Показан другой объект: {obj.gameObject.name}");
                    }
                }
            }
            
            if (currentTarget != null)
            {
                currentTarget.transform.position = originalPosition;
                currentTarget.transform.rotation = originalRotation;
            }
            
            target = null;
            
            if (cam != null)
            {
                cam.enabled = false;
                if (showDebug) Debug.Log("Камера осмотра выключена");
            }

            if (dedicatedInspectionLight != null)
            {
                dedicatedInspectionLight.enabled = false;
                if (showDebug) Debug.Log($"Выделенный свет '{dedicatedInspectionLight.name}' выключен.");
            }

            transform.position = initialPosition;
            transform.rotation = initialRotation;
            
            if (showDebug) Debug.Log("Осмотр завершен, возвращено исходное состояние");
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
                float mouseX = Input.GetAxis("Mouse X") * rotationSpeedRightMouse;
                float mouseY = Input.GetAxis("Mouse Y") * rotationSpeedRightMouse;
                
                currentHorizontalAngle += mouseX;
                currentVerticalAngle = Mathf.Clamp(currentVerticalAngle - mouseY, minVerticalAngle, maxVerticalAngle);
                
                Vector3 direction = Quaternion.Euler(currentVerticalAngle, currentHorizontalAngle, 0) * Vector3.back;
                transform.position = lookAtPoint + direction * currentDistance;
                transform.LookAt(lookAtPoint);
            }
        }

        private void HandleZoom()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0)
            {
                Vector3 directionToCamera = (transform.position - lookAtPoint).normalized;
                
                currentDistance = Mathf.Clamp(currentDistance - scroll * zoomSpeed, minDistance, maxDistance);
                
                transform.position = lookAtPoint + directionToCamera * currentDistance;
                transform.LookAt(lookAtPoint);
            }
        }
    }
}
