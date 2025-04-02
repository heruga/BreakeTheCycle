using UnityEngine;

public interface IInteractable
{
    bool IsInteractable { get; }
    float InteractionRadius { get; }
    bool IsInRange { get; }
    
    void OnInteractionStart();
    void OnInteractionEnd();
    void OnInteractionComplete();
    void OnInteractionCancel();
    
    void ShowInteractionPrompt();
    void HideInteractionPrompt();
    
    bool CheckInteractionRange(Vector3 playerPosition);
    void SetInteractionRange(float radius);
} 