using UnityEngine;
using BreakTheCycle.Dialogue;
using BreakTheCycle;

// Наследуемся от InteractableObject
public class FinalInteractable : InteractableObject 
{
    [Header("Final Monologue Settings")]
    [SerializeField]
    [Tooltip("ID монолога, который проигрывается ПОСЛЕ победы над боссом.")]
    private int finalMonologueID = -1;

    // Переопределяем метод OnInteract из родительского класса
    public override void OnInteract()
    {
        Debug.Log($"[FinalInteractable] Взаимодействие с {gameObject.name}");

        bool isBossDefeated = PlayerPrefs.GetInt("BossDefeated", 0) == 1;
        bool finalSequenceTriggered = PlayerPrefs.GetInt("FinalSequenceTriggered", 0) == 1;

        Debug.Log($"[FinalInteractable] Проверка: isBossDefeated={isBossDefeated}, finalSequenceTriggered={finalSequenceTriggered}");

        if (isBossDefeated && !finalSequenceTriggered)
        {
            Debug.Log($"[FinalInteractable] Босс побежден, запускаем финальный монолог (ID: {finalMonologueID}) и последовательность конца игры.");
            if (finalMonologueID >= 0)
            {
                var monologueManager = FindObjectOfType<MonologueManager>();
                if (monologueManager != null)
                {
                    monologueManager.PlayMonologue(finalMonologueID);

                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.ListenForFinalMonologue(finalMonologueID);
                        PlayerPrefs.SetInt("FinalSequenceTriggered", 1);
                        PlayerPrefs.Save();
                        Debug.Log($"[FinalInteractable] Флаг FinalSequenceTriggered установлен в 1.");
                    }
                    else
                    {
                        Debug.LogError("[FinalInteractable] GameManager.Instance не найден!");
                    }
                }
                else
                {
                    Debug.LogError("[FinalInteractable] MonologueManager не найден в сцене!");
                }
            }
            else
            {
                Debug.LogWarning($"[FinalInteractable] Финальный монолог не назначен (ID < 0).");
            }
        }
        else if (isBossDefeated && finalSequenceTriggered)
        {
            Debug.Log($"[FinalInteractable] Финальная последовательность уже была запущена ранее.");
        }
        else
        {
            Debug.Log($"[FinalInteractable] Босс не побежден. Вызываем базовую реализацию OnInteract() для обычного монолога.");
            base.OnInteract();
        }
    }
} 