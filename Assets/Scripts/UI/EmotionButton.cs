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

            if (isLevelUpButton)
            {
                button.onClick.AddListener(() => {
                    Debug.Log("[EmotionButton] LevelUp button clicked");
                    emotionSystem.UpgradeMaxActiveEmotions();
                });
            }
            else
            {
                longHoldHandler.onLongHold = new UnityEngine.Events.UnityEvent();
                longHoldHandler.onLongHold.AddListener(() => {
                    emotionSystem.UpgradeEmotion(emotionType);
                    UpdateVisual(emotionSystem.IsEmotionActive(emotionType), emotionSystem.activeColor, emotionSystem.inactiveColor);
                });

                button.onClick.AddListener(() => emotionSystem.ToggleEmotion(emotionType));
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
    }
} 