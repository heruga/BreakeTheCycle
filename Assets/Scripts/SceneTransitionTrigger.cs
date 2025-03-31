using UnityEngine;

/// <summary>
/// Скрипт для создания триггерной зоны, которая активирует переход между сценами
/// </summary>
public class SceneTransitionTrigger : MonoBehaviour
{
    private bool isPlayerInTrigger = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInTrigger = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInTrigger = false;
        }
    }

    private void Update()
    {
        if (isPlayerInTrigger && Input.GetKeyDown(KeyCode.R))
        {
            GameManager.Instance.SwitchWorld();
        }
    }
} 