using UnityEngine;
using BreakTheCycle.Dialogue;

namespace Inspection
{
    public class InspectableObject : MonoBehaviour
    {
        [Header("Initial Settings")] 
        [Tooltip("Смещение объекта на экране при осмотре.\nX: положительные значения сместят объект вправо, отрицательные - влево\nY: положительные значения сместят объект вверх, отрицательные - вниз\nZ: обычно оставляйте 0")]
        public Vector3 spawnPositionOffset;
        
        [Tooltip("Начальный поворот объекта при осмотре (в градусах Эйлера).\nX - поворот вокруг оси X (вверх/вниз)\nY - поворот вокруг оси Y (влево/вправо)\nZ - поворот вокруг оси Z (по часовой/против часовой)")]
        public Vector3 spawnRotationOffset;
        
        [Tooltip("Минимальное и максимальное значение зума")]
        public Vector2 minMaxZoom = new Vector2(0.5f,2);
        
        [Tooltip("Значение зума по умолчанию")]
        public float defaultZoomValue = 1f;

        [Header("Object Info")]
        [SerializeField] private string objectName = "Inspectable Object";
        [SerializeField, TextArea(3, 10)] private string description = "This is an inspectable object.";

        [Header("Text Settings")]
        [SerializeField] private Color nameColor = Color.white;
        [SerializeField] private Color descriptionColor = Color.white;
        [SerializeField] private int nameFontSize = 24;
        [SerializeField] private int descriptionFontSize = 18;

        [Header("Activation")]
        public GameObject objectToActivate;

        private DialogueNodeEnqueuer nodeEnqueuer;

        public string ObjectName => objectName;
        public string Description => description;
        public Color NameColor => nameColor;
        public Color DescriptionColor => descriptionColor;
        public int NameFontSize => nameFontSize;
        public int DescriptionFontSize => descriptionFontSize;

        private void Awake()
        {
            nodeEnqueuer = GetComponent<DialogueNodeEnqueuer>();
        }

        public void ActivateLinkedObject()
        {
            if (objectToActivate != null)
                objectToActivate.SetActive(true);
        }

        public void HandleInspectionActions()
        {
            ActivateLinkedObject();

            if (nodeEnqueuer != null)
            {
                Debug.Log($"[InspectableObject] Объект {gameObject.name} осмотрен. Пытаемся использовать DialogueNodeEnqueuer.");
                bool success = nodeEnqueuer.TryEnqueueNode();
                if (success)
                {
                    Debug.Log($"[InspectableObject] Узел для NPC '{nodeEnqueuer.npcId}' (startNodeId: {nodeEnqueuer.startNodeId}) успешно добавлен в очередь при осмотре {gameObject.name}.");
                }
                else
                {   
                    Debug.LogWarning($"[InspectableObject] Не удалось добавить узел в очередь при осмотре {gameObject.name}. Подробности см. в логах DialogueNodeEnqueuer.");
                }
            }
            else
            {
                Debug.Log($"[InspectableObject] На объекте {gameObject.name} не найден компонент DialogueNodeEnqueuer. Постановка узла в очередь не выполняется при осмотре.");
            }
        }
    }
}
