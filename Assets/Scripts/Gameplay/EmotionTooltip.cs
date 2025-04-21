using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class EmotionTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public EmotionSystem.EmotionType emotionType;
    public GameObject tooltipPanel;
    public TextMeshProUGUI tooltipText;
    private EmotionSystem emotionSystem;

    private void Start()
    {
        emotionSystem = FindObjectOfType<EmotionSystem>();
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (tooltipPanel != null && tooltipText != null && emotionSystem != null)
        {
            tooltipText.text = emotionSystem.GetEmotionDescription(emotionType);
            tooltipPanel.SetActive(true);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
        }
    }
} 