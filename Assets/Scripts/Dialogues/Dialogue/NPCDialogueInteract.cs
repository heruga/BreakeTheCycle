using UnityEngine;
using BreakTheCycle.Dialogue;

namespace BreakTheCycle.Dialogue
{
    public class NPCDialogueInteract : MonoBehaviour, IInteractable
    {
        public string npcId = ""; // Уникальный идентификатор NPC
        private VIDE_Assign dialogueAssign;
        public Template_UIManager uiManager;

        private void Awake()
        {
            Debug.Log($"[NPCDialogueInteract] Awake: npcId={npcId}, gameObject={gameObject.name}");
            dialogueAssign = GetComponent<VIDE_Assign>();
            if (dialogueAssign == null)
            {
                Debug.LogWarning($"[NPCDialogueInteract] На объекте {gameObject.name} не найден компонент VIDE_Assign!");
            }
            if (uiManager == null)
            {
                uiManager = FindObjectOfType<Template_UIManager>();
                if (uiManager == null)
                    Debug.LogWarning("[NPCDialogueInteract] В сцене не найден Template_UIManager!");
            }
        }

        public void OnInteract()
        {
            if (dialogueAssign == null)
            {
                Debug.LogWarning($"[NPCDialogueInteract] Нельзя взаимодействовать: VIDE_Assign не найден на {gameObject.name}!");
                return;
            }

                if (!VIDE_Data.VIDE_Data.isActive)
                {
                int nextNodeId = -1; // По умолчанию -1 (используется базовый узел)
                if (!string.IsNullOrEmpty(npcId) && DialogueStartNodeQueue.Instance != null)
                {
                    if (DialogueStartNodeQueue.Instance.TryDequeueNextStartNode(npcId, out int dequeuedId))
                    {
                        nextNodeId = dequeuedId; // Используем ID из очереди
                        Debug.Log($"[NPCDialogueInteract] Взят узел {nextNodeId} из очереди для NPC {npcId}");
                    }
                    else
                    {
                        Debug.Log($"[NPCDialogueInteract] Очередь для NPC {npcId} пуста, используем базовый узел.");
                    }
                }
                else
                {
                    Debug.LogWarning($"[NPCDialogueInteract] Не можем проверить очередь: npcId пуст или DialogueStartNodeQueue.Instance == null. Используем базовый узел.");
                }
                dialogueAssign.overrideStartNode = nextNodeId;

                if (uiManager != null)
                {
                    Debug.Log($"[NPCDialogueInteract] Запуск диалога с overrideStartNode = {nextNodeId}");
                    uiManager.Interact(dialogueAssign);
                }
                else
                    Debug.LogWarning("UI-менеджер не назначен!");
            }
            else
            {
                if (uiManager != null)
                {
                     Debug.Log("[NPCDialogueInteract] Диалог уже активен, передаем управление UI-менеджеру.");
                     uiManager.Interact(dialogueAssign);
                }
            }
        }
    }
} 