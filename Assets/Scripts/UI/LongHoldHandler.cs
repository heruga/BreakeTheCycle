using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class LongHoldHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public float holdTime = 2f;
    private bool isHolding = false;
    private float holdStartTime;
    
    public UnityEvent onLongHold;
    public UnityEvent onPointerDownEvent;
    public UnityEvent onPointerUpEvent;
    
    public bool IsHolding => isHolding;
    public float HoldStartTime => holdStartTime;
    
    public void OnPointerDown(PointerEventData eventData)
    {
        isHolding = true;
        holdStartTime = Time.time;
        onPointerDownEvent?.Invoke();
        
        // Скрываем тултип при нажатии
        if (ToolTipManager.Instance != null)
        {
            ToolTipManager.Instance.HideTooltip();
        }
    }
    
    public void OnPointerUp(PointerEventData eventData)
    {
        isHolding = false;
        onPointerUpEvent?.Invoke();
    }
    
    private void Update()
    {
        if (isHolding && Time.time >= holdStartTime + holdTime)
        {
            isHolding = false;
            onLongHold?.Invoke();
        }
    }
}
