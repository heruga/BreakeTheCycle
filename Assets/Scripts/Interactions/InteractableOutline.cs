using UnityEngine;
using BreakTheCycle;

[RequireComponent(typeof(BaseInteractable))]
public class InteractableOutline : MonoBehaviour
{
    [Header("Настройки обводки")]
    [SerializeField] private Color outlineColor = Color.yellow;
    [SerializeField] private float outlineWidth = 5f;
    [SerializeField] private bool enableOnStart = false;

    private Outline outline;
    private BaseInteractable interactable;

    private void Awake()
    {
        interactable = GetComponent<BaseInteractable>();
        
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

    private void OnEnable()
    {
        if (interactable != null)
        {
            interactable.PlayerEnteredZone += ShowOutline;
            interactable.PlayerExitedZone += HideOutline;
        }
    }

    private void OnDisable()
    {
        if (interactable != null)
        {
            interactable.PlayerEnteredZone -= ShowOutline;
            interactable.PlayerExitedZone -= HideOutline;
        }
    }

    public void ShowOutline()
    {
        if (outline != null)
        {
            outline.enabled = true;
        }
    }

    public void HideOutline()
    {
        if (outline != null)
        {
            outline.enabled = false;
        }
    }
} 