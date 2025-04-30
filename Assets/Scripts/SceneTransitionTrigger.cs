using UnityEngine;

/// <summary>
/// Скрипт для создания триггерной зоны, которая информирует GameManager о входе/выходе игрока.
/// </summary>
public class SceneTransitionTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("[SceneTransitionTrigger] OnTriggerEnter СРАБОТАЛ для игрока!");
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetPlayerInTransitionZone(true);
            }
            else
            {
                Debug.LogError("[SceneTransitionTrigger] GameManager.Instance не найден при входе в триггер!");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
             Debug.Log("[SceneTransitionTrigger] OnTriggerExit СРАБОТАЛ для игрока!");
             if (GameManager.Instance != null)
            {
                GameManager.Instance.SetPlayerInTransitionZone(false);
            }
            // Ошибка здесь менее критична, но можно добавить лог при желании
            // else { Debug.LogWarning("[SceneTransitionTrigger] GameManager.Instance не найден при выходе из триггера!"); }
        }
    }
} 