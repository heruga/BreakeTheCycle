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
            // --- Применяем стартовый узел из глобального менеджера очереди ---
            if (!string.IsNullOrEmpty(npcId) && DialogueStartNodeQueue.Instance != null)
            {
                int nodeId;
                if (DialogueStartNodeQueue.Instance.TryGetStartNode(npcId, out nodeId))
                {
                    if (dialogueAssign != null)
                    {
                        dialogueAssign.overrideStartNode = nodeId;
                        Debug.Log($"[NPCDialogueInteract] Применён стартовый узел {nodeId} для NPC {npcId}");
                        DialogueStartNodeQueue.Instance.RemoveStartNode(npcId);
                    }
                }
            }
        }

        public void OnInteract()
        {
            if (dialogueAssign != null)
            {
                if (!VIDE_Data.VIDE_Data.isActive)
                {
                    if (uiManager != null)
                        uiManager.Interact(dialogueAssign);
                    else
                        Debug.LogWarning("UI-менеджер не назначен!");
                }
                else
                {
                    Debug.LogWarning("Диалог уже активен! Сначала завершите предыдущий.");
                }
            }
            else
            {
                Debug.LogWarning($"[NPCDialogueInteract] Нельзя запустить диалог: VIDE_Assign не назначен на {gameObject.name}!");
            }
        }
    }
} 