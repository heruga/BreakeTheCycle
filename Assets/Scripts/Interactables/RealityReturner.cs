using UnityEngine;
using BreakTheCycle;

/// <summary>
/// Интерактивный объект, возвращающий игрока в другой мир при взаимодействии.
/// </summary>
public class RealityReturner : BaseInteractable
{
    public override void OnInteract()
    {
        // Проверяем, есть ли GameManager
        if (GameManager.Instance == null)
        {
            Debug.LogError("[RealityReturner] GameManager.Instance не найден!");
            return;
        }

        // Сохраняем состояние игры и сцены перед переходом
        GameManager.Instance.SaveGame();
        GameManager.Instance.SaveSceneState(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);

        // Переходим именно в реальность
        GameManager.Instance.StartCoroutine(GameManager.Instance.SwitchWorldCoroutine(GameManager.Instance.realitySceneName));
        Debug.Log("[RealityReturner] Переключаю мир: переход в реальность через SwitchWorldCoroutine...");
    }
} 