using UnityEngine;

namespace Inspection
{
    public class ObjectInspector : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private InspectionCamera inspectionCamera;
        [SerializeField] private Camera mainCamera;
        
        [Header("Settings")]
        [SerializeField] private LayerMask inspectableLayer;
        [SerializeField] private float maxInspectionDistance = 5f;
        [SerializeField] private bool showDebug = true; // Для отладочных сообщений

        private InspectableObject currentObject;

        private void Start()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null)
                {
                    Debug.LogError("Main Camera not found! Please assign it manually in the inspector or make sure there is a camera tagged as 'MainCamera' in the scene.");
                    enabled = false;
                    return;
                }
            }

            if (inspectionCamera == null)
            {
                Debug.LogError("Inspection Camera not assigned! Please assign it in the inspector.");
                enabled = false;
                return;
            }

            // Проверяем настройку LayerMask
            if (inspectableLayer.value == 0)
            {
                Debug.LogWarning("Inspectable Layer is not set! Please select the layer for inspectable objects in the inspector.");
            }

            if (showDebug) Debug.Log("ObjectInspector initialized successfully. Press E while looking at an inspectable object to start inspection.");
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                if (showDebug) Debug.Log("E key pressed");
                
                if (currentObject == null)
                    TryStartInspection();
                else
                    StopInspection();
            }
        }

        private void TryStartInspection()
        {
            if (mainCamera == null) return;

            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (showDebug) Debug.DrawRay(ray.origin, ray.direction * maxInspectionDistance, Color.red, 1f);

            if (Physics.Raycast(ray, out RaycastHit hit, maxInspectionDistance, inspectableLayer))
            {
                if (showDebug) Debug.Log($"Hit object: {hit.collider.gameObject.name} on layer {LayerMask.LayerToName(hit.collider.gameObject.layer)}");
                
                var inspectable = hit.collider.GetComponent<InspectableObject>();
                if (inspectable != null)
                {
                    StartInspection(inspectable);
                }
                else if (showDebug)
                {
                    Debug.Log($"Object {hit.collider.gameObject.name} doesn't have InspectableObject component");
                }
            }
            else if (showDebug)
            {
                Debug.Log($"No hit detected. Make sure objects are on layer {LayerMask.LayerToName(Mathf.RoundToInt(Mathf.Log(inspectableLayer.value, 2)))}");
            }
        }

        private void StartInspection(InspectableObject obj)
        {
            if (inspectionCamera == null) return;

            currentObject = obj;
            inspectionCamera.StartInspecting(obj.transform);
            
            if (showDebug) Debug.Log($"Started inspecting: {obj.gameObject.name}");
        }

        private void StopInspection()
        {
            if (inspectionCamera == null) return;

            inspectionCamera.StopInspecting();
            currentObject = null;
            
            if (showDebug) Debug.Log("Stopped inspection");
        }

        // Визуализация в редакторе
        private void OnDrawGizmosSelected()
        {
            if (mainCamera != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(mainCamera.transform.position, maxInspectionDistance);
            }
        }
    }
}
