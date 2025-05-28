using UnityEngine;
using UnityEngine.EventSystems;

public class ToolTip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private string tooltipText;
    [SerializeField] private float showDelay = 0.5f;
    [SerializeField] private bool showOnStart = false; // Показывать ли тултип сразу при старте
    
    private bool isPointerOver = false;
    private float pointerEnterTime;
    private Vector2 lastPointerPosition;
    private bool isTooltipShown = false;
    
    private void Start()
    {
        if (showOnStart && ToolTipManager.Instance != null)
        {
            // Если нужно показать тултип сразу при старте
            ShowTooltipImmediate();
        }
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerOver = true;
        pointerEnterTime = Time.time;
        lastPointerPosition = eventData.position;
        isTooltipShown = false;
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerOver = false;
        if (ToolTipManager.Instance != null)
        {
            ToolTipManager.Instance.HideTooltip();
            isTooltipShown = false;
        }
    }
    
    private void Update()
    {
        if (isPointerOver && !isTooltipShown)
        {
            // Обновляем позицию указателя
            lastPointerPosition = Input.mousePosition;
            
            if (Time.time >= pointerEnterTime + showDelay)
            {
                if (ToolTipManager.Instance != null)
                {
                    ToolTipManager.Instance.ShowTooltip(tooltipText, lastPointerPosition);
                    isTooltipShown = true;
                }
            }
        }
    }
    
    public void ShowTooltipImmediate()
    {
        if (ToolTipManager.Instance != null)
        {
            // Показываем тултип немедленно в позиции объекта
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(Camera.main, transform.position);
            ToolTipManager.Instance.ShowTooltip(tooltipText, screenPos);
            isTooltipShown = true;
        }
    }
    
    public void SetTooltipText(string text)
    {
        tooltipText = text;
        
        // Если тултип уже показан, обновляем его содержимое
        if (isTooltipShown && ToolTipManager.Instance != null)
        {
            ToolTipManager.Instance.ShowTooltip(tooltipText, lastPointerPosition);
        }
    }
    
    private void OnDisable()
    {
        // Скрываем тултип при отключении объекта
        if (isTooltipShown && ToolTipManager.Instance != null)
        {
            ToolTipManager.Instance.HideTooltip();
            isTooltipShown = false;
        }
    }
}
