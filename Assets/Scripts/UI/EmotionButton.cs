using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace UI
{
    [RequireComponent(typeof(Button))]
    public class EmotionButton : MonoBehaviour
    {
        [SerializeField] private EmotionSystem.EmotionType emotionType;
        [SerializeField] private bool isLevelUpButton = false;
        
        private Button button;
        private ToolTip tooltip;
        private LongHoldHandler longHoldHandler;
        private Image buttonImage;
        private float originalAlpha;
        private EmotionSystem emotionSystemInstance;
        private Color baseColor;

        // Поля для хранения ссылок на делегаты для отписки
        private UnityEngine.Events.UnityAction onLevelUpButtonClickAction;
        private UnityEngine.Events.UnityAction onEmotionButtonClickAction;
        private UnityEngine.Events.UnityAction onLongHoldAction;

        private void Awake()
        {
            button = GetComponent<Button>();
            buttonImage = GetComponent<Image>();
            originalAlpha = buttonImage.color.a;
            baseColor = buttonImage.color;

            if (!isLevelUpButton)
            {
                tooltip = GetComponent<ToolTip>();
                if (tooltip == null)
                {
                    tooltip = gameObject.AddComponent<ToolTip>();
                }

                longHoldHandler = GetComponent<LongHoldHandler>();
                if (longHoldHandler == null)
                {
                    longHoldHandler = gameObject.AddComponent<LongHoldHandler>();
                    longHoldHandler.holdTime = 2f;
                }
            }
        }

        public void Initialize(EmotionSystem emotionSystem)
        {
            this.emotionSystemInstance = emotionSystem;

            // Удаляем предыдущие слушатели, если есть (на случай повторной инициализации)
            if (onLevelUpButtonClickAction != null) button.onClick.RemoveListener(onLevelUpButtonClickAction);
            if (onEmotionButtonClickAction != null) button.onClick.RemoveListener(onEmotionButtonClickAction);
            if (longHoldHandler != null && longHoldHandler.onLongHold != null && onLongHoldAction != null) longHoldHandler.onLongHold.RemoveListener(onLongHoldAction);

            if (isLevelUpButton)
            {
                onLevelUpButtonClickAction = () => {
                    Debug.Log("[EmotionButton] LevelUp button clicked");
                    emotionSystem.UpgradeMaxActiveEmotions();
                };
                button.onClick.AddListener(onLevelUpButtonClickAction);
            }
            else
            {
                onLongHoldAction = () => {
                    emotionSystem.UpgradeEmotion(emotionType);
                    UpdateVisual(emotionSystem.IsEmotionActive(emotionType), emotionSystem.activeColor, emotionSystem.inactiveColor);
                };
                // Убедимся что onLongHold существует, если он мог быть не создан в Awake
                if (longHoldHandler.onLongHold == null) 
                {
                    longHoldHandler.onLongHold = new UnityEngine.Events.UnityEvent();
                }
                longHoldHandler.onLongHold.AddListener(onLongHoldAction);

                onEmotionButtonClickAction = () => emotionSystem.ToggleEmotion(emotionType);
                button.onClick.AddListener(onEmotionButtonClickAction);
            }
        }

        public void UpdateVisual(bool isActive, Color activeColor, Color inactiveColor)
        {
            if (buttonImage != null)
            {
                Color targetColor = isActive ? activeColor : inactiveColor;
                targetColor.a = originalAlpha;
                buttonImage.color = targetColor;
                baseColor = targetColor;
            }
        }

        public void UpdateTooltip(string text)
        {
            if (!isLevelUpButton && tooltip != null)
            {
                tooltip.SetTooltipText(text);
            }
        }

        public void UpdateProgress(float progress)
        {
            if (isLevelUpButton || buttonImage == null) return;

            Color targetHighlightColor = Color.Lerp(baseColor, Color.white, progress * 0.7f);
            targetHighlightColor.a = originalAlpha;
            buttonImage.color = targetHighlightColor;
        }

        public void OnHoldCancelled()
        {
            if (isLevelUpButton) return;

            if (buttonImage != null && emotionSystemInstance != null)
            {
                UpdateVisual(emotionSystemInstance.IsEmotionActive(emotionType), emotionSystemInstance.activeColor, emotionSystemInstance.inactiveColor);
            }
        }

        public EmotionSystem.EmotionType GetEmotionType() => emotionType;

        private void OnDestroy()
        {
            if (button != null)
            {
                if (onLevelUpButtonClickAction != null)
                {
                    button.onClick.RemoveListener(onLevelUpButtonClickAction);
                }
                if (onEmotionButtonClickAction != null)
                {
                    button.onClick.RemoveListener(onEmotionButtonClickAction);
                }
            }

            if (longHoldHandler != null && longHoldHandler.onLongHold != null && onLongHoldAction != null)
            {
                longHoldHandler.onLongHold.RemoveListener(onLongHoldAction);
            }
        }
    }
} 