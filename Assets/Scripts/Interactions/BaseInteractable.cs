using UnityEngine;
using UnityEngine.Events;

public abstract class BaseInteractable : MonoBehaviour, IInteractable
{
    [Header("Interaction Settings")]
    [SerializeField] protected float interactionRadius = 2f;
    [SerializeField] protected bool isInteractable = true;
    
    [Header("Events")]
    public UnityEvent onInteractionStart = new UnityEvent();
    public UnityEvent onInteractionEnd = new UnityEvent();
    public UnityEvent onInteractionComplete = new UnityEvent();
    public UnityEvent onInteractionCancel = new UnityEvent();
    
    protected bool isInRange;
    protected bool isInteracting;
    
    public virtual bool IsInteractable => isInteractable;
    public virtual float InteractionRadius => interactionRadius;
    public virtual bool IsInRange => isInRange;
    
    protected virtual void Start()
    {
        if (interactionRadius <= 0)
        {
            Debug.LogWarning($"Interaction radius is set to 0 or negative on {gameObject.name}");
        }
    }
    
    public virtual void OnInteractionStart()
    {
        if (!IsInteractable || isInteracting) return;
        
        isInteracting = true;
        onInteractionStart.Invoke();
    }
    
    public virtual void OnInteractionEnd()
    {
        if (!isInteracting) return;
        
        isInteracting = false;
        onInteractionEnd.Invoke();
    }
    
    public virtual void OnInteractionComplete()
    {
        if (!isInteracting) return;
        
        isInteracting = false;
        onInteractionComplete.Invoke();
    }
    
    public virtual void OnInteractionCancel()
    {
        if (!isInteracting) return;
        
        isInteracting = false;
        onInteractionCancel.Invoke();
    }
    
    public virtual void ShowInteractionPrompt()
    {
        // Реализация в дочерних классах
    }
    
    public virtual void HideInteractionPrompt()
    {
        // Реализация в дочерних классах
    }
    
    public virtual bool CheckInteractionRange(Vector3 playerPosition)
    {
        float distance = Vector3.Distance(transform.position, playerPosition);
        isInRange = distance <= interactionRadius;
        return isInRange;
    }
    
    public virtual void SetInteractionRange(float radius)
    {
        interactionRadius = radius;
    }
    
    protected virtual void OnDrawGizmosSelected()
    {
        // Визуализация радиуса взаимодействия в редакторе
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
} 