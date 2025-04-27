using UnityEngine;
using BreakTheCycle;
namespace Inspection
{
    public class ObjectInspector : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private InspectionCamera inspectionCamera;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private InspectionWindowUI inspectionUI; // Добавляем ссылку на UI-окно
        
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

            if (inspectionUI == null)
            {
                // Пытаемся найти UI в сцене
                inspectionUI = FindObjectOfType<InspectionWindowUI>();
                if (inspectionUI == null)
                {
                    Debug.LogWarning("InspectionWindowUI not assigned! UI features will be disabled.");
                }
            }

            // Проверяем настройку LayerMask
            if (inspectableLayer.value == 0)
            {
                Debug.LogWarning("Inspectable Layer is not set! Please select the layer for inspectable objects in the inspector.");
            }

            if (showDebug) Debug.Log("ObjectInspector успешно инициализирован. Нажмите E, глядя на осматриваемый объект, чтобы начать осмотр.");
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Escape))
            {

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
                
                var inspectable = hit.collider.GetComponentInParent<InspectableObject>();
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

            // Отключаем основную камеру игрока на время осмотра
            if (mainCamera != null)
            {
                mainCamera.enabled = false;
                Debug.Log($"[ObjectInspector] mainCamera отключена: {mainCamera.name}");
            }

            currentObject = obj;
            inspectionCamera.StartInspecting(obj.gameObject);
            Debug.Log($"[ObjectInspector] inspectionCamera активирована: {inspectionCamera.name}");

            // Активируем связанный объект, если он есть
            obj.ActivateLinkedObject();

            // Показываем UI с информацией об объекте
            if (inspectionUI != null)
            {
                inspectionUI.ShowWindow(obj);
                Debug.Log("[ObjectInspector] Окно осмотра показано");
            }
            
            if (showDebug) Debug.Log($"Started inspecting: {obj.gameObject.name}");

            // Блокируем управление игроком на время осмотра
            if (PlayerControlManager.Instance != null)
            {
                PlayerControlManager.Instance.SetControlsEnabled(false);
                Debug.Log("[ObjectInspector] Управление игроком заблокировано");
            }
        }

        private void StopInspection()
        {
            if (inspectionCamera == null) return;

            inspectionCamera.StopInspecting();
            Debug.Log($"[ObjectInspector] inspectionCamera деактивирована: {inspectionCamera.name}");
            
            // Включаем основную камеру игрока после осмотра
            if (mainCamera != null)
            {
                mainCamera.enabled = true;
                Debug.Log($"[ObjectInspector] mainCamera включена: {mainCamera.name}");
            }

            // Скрываем UI при выходе из режима осмотра
            if (inspectionUI != null)
            {
                inspectionUI.HideWindow();
                Debug.Log("[ObjectInspector] Окно осмотра скрыто");
            }

            // Воспроизводим монолог, если есть MonologueTriggerData
            if (currentObject != null)
            {
                var trigger = currentObject.GetComponent<BreakTheCycle.Dialogue.MonologueTriggerData>();
                if (trigger != null && trigger.monologueID >= 0)
                {
                    var manager = FindObjectOfType<BreakTheCycle.Dialogue.MonologueManager>();
                    if (manager != null)
                    {
                        manager.PlayMonologue(trigger.monologueID);
                    }
                }
            }

            currentObject = null;
            
            if (showDebug) Debug.Log("Осмотр завершён");

            // Возвращаем управление игроку после осмотра
            if (PlayerControlManager.Instance != null)
            {
                PlayerControlManager.Instance.SetControlsEnabled(true);
                Debug.Log("[ObjectInspector] Управление игроком восстановлено");
            }
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
