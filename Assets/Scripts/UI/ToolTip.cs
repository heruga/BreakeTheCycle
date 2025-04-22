using UnityEngine;
using UnityEngine.EventSystems;

public class ToolTip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private string tooltipText;
    [SerializeField] private float showDelay = 0.5f;
    
    private bool isPointerOver = false;
    private float pointerEnterTime;
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerOver = true;
        pointerEnterTime = Time.time;
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerOver = false;
        ToolTipManager.Instance.HideTooltip();
    }
    
    private void Update()
    {
        if (isPointerOver && Time.time >= pointerEnterTime + showDelay)
        {
            ToolTipManager.Instance.ShowTooltip(tooltipText, Input.mousePosition);
            isPointerOver = false;
        }
    }
    
    public void SetTooltipText(string text)
    {
        tooltipText = text;
    }
}
