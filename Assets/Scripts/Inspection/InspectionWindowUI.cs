using UnityEngine;
using TMPro;

namespace Inspection
{
    public class InspectionWindowUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject windowPanel;
        [SerializeField] private TextMeshProUGUI itemNameText;
        [SerializeField] private TextMeshProUGUI itemDescriptionText;
        [SerializeField] private TextMeshProUGUI controlsHintText;

        [Header("Settings")]
        [SerializeField] private string controlsHintMessage = "Вращение: Удерживайте ПКМ\nЗум: Колесо мыши\nВыход: E";
        [SerializeField] private bool showOnStart = false;

        private CanvasGroup canvasGroup;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            // Настройка Canvas Group для контроля видимости
            HideWindow();
            
            // Установка подсказки по управлению
            if (controlsHintText != null)
            {
                controlsHintText.text = controlsHintMessage;
            }
        }

        private void Start()
        {
            // Прячем окно при старте, если не указано иное
            if (!showOnStart)
            {
                HideWindow();
            }
        }

        /// Показывает окно инспекции с информацией о предмете
        /// <param name="inspectableObject">Осматриваемый объект</param>
        public void ShowWindow(InspectableObject inspectableObject)
        {
            if (inspectableObject == null) return;

            // Установка информации о предмете
            if (itemNameText != null)
            {
                itemNameText.text = inspectableObject.ObjectName;
                itemNameText.color = inspectableObject.NameColor;
                itemNameText.fontSize = inspectableObject.NameFontSize;
            }

            if (itemDescriptionText != null)
            {
                itemDescriptionText.text = inspectableObject.Description;
                itemDescriptionText.color = inspectableObject.DescriptionColor;
                itemDescriptionText.fontSize = inspectableObject.DescriptionFontSize;
            }

            // Отображение окна
            canvasGroup.alpha = 1;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = false; // Не блокируем взаимодействие с объектами

            if (windowPanel != null)
            {
                windowPanel.SetActive(true);
            }
        }

        /// Скрывает окно инспекции
        public void HideWindow()
        {
            canvasGroup.alpha = 0;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            if (windowPanel != null)
            {
                windowPanel.SetActive(false);
            }
        }
    }
} 