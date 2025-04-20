using UnityEngine;
using BreakTheCycle;

/// <summary>
/// Интерактивный объект для работы с системой эмоций
/// </summary>
public class EmotionInteractable : BaseInteractable
{
    [Header("Ссылки")]
    [SerializeField] private EmotionUI emotionUI;

    private void Start()
    {
        // Проверяем, задан ли EmotionUI
        if (emotionUI == null)
        {
            emotionUI = FindObjectOfType<EmotionUI>();
            if (emotionUI == null)
            {
                Debug.LogError("[EmotionInteractable] EmotionUI не найден. Добавьте в сцену объект с компонентом EmotionUI.");
            }
        }
        
        // Проверяем наличие EmotionSystem
        if (EmotionSystem.Instance == null)
        {
            Debug.LogError("[EmotionInteractable] EmotionSystem не найден. Добавьте в сцену GameManager или объект с компонентом EmotionSystem.");
        }
    }
    
    public override void OnInteract()
    {
        Debug.Log($"[EmotionInteractable] Игрок взаимодействует с объектом: {gameObject.name}");
        if (emotionUI == null)
        {
            Debug.LogError("[EmotionInteractable] EmotionUI не найден");
            return;
        }
        
        if (!emotionUI.IsOpen())
        {
            emotionUI.Open();
            PlayerControlManager.Instance.SetControlsEnabled(false);
        }
        else
        {
            emotionUI.Close();
            PlayerControlManager.Instance.SetControlsEnabled(true);
        }
        
        // Вызываем базовый метод для выполнения UnityEvent
        base.OnInteract();
    }
    
    public override void OnPlayerEnter()
    {
        base.OnPlayerEnter();
        // Визуальная обратная связь отключена
    }
    
    public override void OnPlayerExit()
    {
        base.OnPlayerExit();
        // Визуальная обратная связь отключена
        // Закрываем окно, если открыто
        if (emotionUI != null && emotionUI.IsOpen())
        {
            emotionUI.Close();
            PlayerControlManager.Instance.SetControlsEnabled(true);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 0.6f, 1f, 1f); // Ярко-синий для контура
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
#endif
} 