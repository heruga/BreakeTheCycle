using UnityEngine;
using Inspection;

namespace Inspection 
{
    [RequireComponent(typeof(InspectableObject))]
    public class InspectableOutline : MonoBehaviour
    {
        [Header("Настройки обводки")]
        [SerializeField] private Color outlineColor = Color.cyan;
        [SerializeField] private float outlineWidth = 5f;
        [SerializeField] private bool enableOnStart = false;

        private Outline outline;
        private bool isInRange = false;

        private void Awake()
        {
            // Добавляем компонент Outline, если его еще нет
            outline = GetComponent<Outline>();
            if (outline == null)
            {
                outline = gameObject.AddComponent<Outline>();
            }
            
            // Настраиваем параметры обводки
            outline.OutlineMode = Outline.Mode.OutlineVisible;
            outline.OutlineColor = outlineColor;
            outline.OutlineWidth = outlineWidth;
            
            // По умолчанию отключаем обводку
            outline.enabled = enableOnStart;
        }

        public void ShowOutline()
        {
            isInRange = true;
            if (outline != null)
            {
                outline.enabled = true;
            }
        }

        public void HideOutline()
        {
            isInRange = false;
            if (outline != null)
            {
                outline.enabled = false;
            }
        }

        // Геттер для внешнего доступа
        public bool IsInRange()
        {
            return isInRange;
        }
    }
} 