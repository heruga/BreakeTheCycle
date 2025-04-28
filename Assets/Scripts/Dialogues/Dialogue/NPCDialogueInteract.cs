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
            // --- Применяем стартовый узел из глобального менеджера очереди ---
            if (!string.IsNullOrEmpty(npcId) && DialogueStartNodeQueue.Instance != null)
            {
                int nodeId;
                bool found = DialogueStartNodeQueue.Instance.TryGetStartNode(npcId, out nodeId);
                Debug.Log($"[NPCDialogueInteract] Проверка очереди: найдено={found}, nodeId={nodeId}");
                if (found)
                {
                    if (dialogueAssign != null)
                    {
                        dialogueAssign.overrideStartNode = nodeId;
                        Debug.Log($"[NPCDialogueInteract] Применён стартовый узел {nodeId} для NPC {npcId}");
                        DialogueStartNodeQueue.Instance.RemoveStartNode(npcId);
                    }
                }
            }
            else
            {
                Debug.Log($"[NPCDialogueInteract] Очередь пуста или npcId не задан: npcId={npcId}, DialogueStartNodeQueue.Instance={(DialogueStartNodeQueue.Instance != null)}");
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