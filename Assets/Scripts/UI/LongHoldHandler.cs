using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UI; // Добавляем для EmotionButton

public class LongHoldHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public float holdTime = 2f;
    private bool isHolding = false;
    private float holdStartTime;
    private bool longHoldTriggered = false; // Флаг, что долгое удержание уже сработало

    public UnityEvent onLongHold;
    public UnityEvent onPointerDownEvent;
    public UnityEvent onPointerUpEvent;

    private EmotionButton emotionButton; // Ссылка на EmotionButton

    public bool IsHolding => isHolding;
    public float HoldStartTime => holdStartTime;

    private void Awake()
    {
        emotionButton = GetComponent<EmotionButton>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isHolding = true;
        holdStartTime = Time.time;
        longHoldTriggered = false; // Сбрасываем флаг при новом нажатии
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

        // Если удержание было прервано до срабатывания onLongHold
        if (!longHoldTriggered && emotionButton != null)
        {
            emotionButton.OnHoldCancelled();
        }
    }
    
    private void Update()
    {
        if (isHolding && !longHoldTriggered && Time.time >= holdStartTime + holdTime)
        {
            isHolding = false; // Можно оставить true, если нужно, чтобы UpdateProgress продолжал работать до OnPointerUp
            longHoldTriggered = true;
            onLongHold?.Invoke();
        }
    }
}
