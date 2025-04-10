using UnityEngine;

namespace Inspection
{
public class InspectionCamera : MonoBehaviour
{
        [Header("Camera Settings")]
        [SerializeField] private float rotationSpeed = 2f;
        
        [Header("Zoom Settings")]
        [SerializeField] private float zoomSpeed = 2f;
        [SerializeField] private float minDistance = 1f;
        [SerializeField] private float maxDistance = 10f;
        [SerializeField] private float initialDistance = 5f;
        
        [SerializeField] private bool showDebug = true;

        [Header("Background Settings")]
        [SerializeField] private Color backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
        [SerializeField] private bool isolatedView = true;
        [SerializeField] private LayerMask inspectableLayer;

        [Header("Camera Positioning")]
        [SerializeField] private float offsetX = -2f;
        [SerializeField] private bool useOffset = true;

        private Camera cam;
        private Transform target;
        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private float currentDistance;
        private bool isInspecting = false;
        private InspectableObject[] allInspectableObjects;

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
            if (showDebug) Debug.Log("Camera setup completed with isolated view settings");
        }

        private void SaveInitialTransform()
        {
            initialPosition = transform.position;
            initialRotation = transform.rotation;
            if (showDebug) Debug.Log($"Saved initial transform - Pos: {initialPosition}, Rot: {initialRotation.eulerAngles}");
        }

        public void StartInspecting(Transform newTarget)
        {
            if (newTarget == null)
            {
                Debug.LogError("Trying to inspect null target!");
                return;
            }
            
            target = newTarget;
            isInspecting = true;

            if (isolatedView)
            {
                foreach (var obj in allInspectableObjects)
                {
                    if (obj.transform != target)
                    {
                        obj.gameObject.SetActive(false);
                    }
                }
                
                cam.cullingMask = inspectableLayer;
            }

            if (cam != null)
            {
                cam.enabled = true;
                if (showDebug) Debug.Log("Inspection camera enabled");
            }

            // Устанавливаем начальное расстояние
            currentDistance = initialDistance;
            
            // Устанавливаем начальную позицию камеры со смещением влево
            Vector3 offset = useOffset ? Vector3.right * offsetX : Vector3.zero;
            transform.position = target.position + Vector3.back * currentDistance + offset;
            transform.LookAt(target.position);
            
            if (showDebug) Debug.Log($"Started inspecting {target.name} from position {transform.position}");
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
                
                Vector3 pivotPoint = target.position;
                transform.RotateAround(pivotPoint, Vector3.up, mouseX);
                transform.RotateAround(pivotPoint, transform.right, -mouseY);
                
                transform.LookAt(pivotPoint);
                
                if (showDebug) Debug.Log($"Rotating camera - X: {mouseX}, Y: {mouseY}");
            }
        }

        private void HandleZoom()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.001f)
            {
                // Изменяем расстояние
                currentDistance = Mathf.Clamp(currentDistance - scroll * zoomSpeed, minDistance, maxDistance);
                
                // Обновляем позицию камеры с учетом смещения
                Vector3 directionToTarget = (transform.position - target.position).normalized;
                Vector3 rightOffset = useOffset ? transform.right * offsetX : Vector3.zero;
                transform.position = target.position + directionToTarget * currentDistance + rightOffset;
                
                // Обновляем LookAt для сохранения фокуса на объекте
                transform.LookAt(target.position);
                
                if (showDebug) Debug.Log($"Zoom updated - Distance: {currentDistance}, Scroll: {scroll}");
            }
        }
    }
}
