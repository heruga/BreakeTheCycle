using UnityEngine;
// Remap VIDE_Data to avoid conflict if you have another VIDE_Data class elsewhere, or ensure uniqueness.
// For now, assuming BreakTheCycle.Dialogue.VIDE_Data is not what's intended for VIDE_Data.VIDE_Data.isActive
// This might need adjustment based on your actual VIDE plugin structure.
// using VIDE_Data = VIDE_Data; // Example of aliasing if needed.

namespace BreakTheCycle.Dialogue
{
    // Теперь наследуемся от BaseInteractable
    public class NPCDialogueInteract : BaseInteractable 
    {
        public string npcId = ""; // Уникальный идентификатор NPC
        private VIDE_Assign dialogueAssign;
        public Template_UIManager uiManager;

        // Awake остается таким же или можно вызывать base.Awake() если в BaseInteractable есть логика Awake
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

        // Переопределяем OnInteract из BaseInteractable
        public override void OnInteract()
        {
            // Используем свойство IsInteractable из базового класса
            if (!base.IsInteractable) 
            {
                Debug.Log($"[NPCDialogueInteract] Объект {gameObject.name} не интерактивен (IsInteractable = false).");
                return;
            }

            if (dialogueAssign == null)
            {
                Debug.LogWarning($"[NPCDialogueInteract] Нельзя взаимодействовать: VIDE_Assign не найден на {gameObject.name}!");
                return;
            }

            // Если VIDE_Data это namespace или статический класс из плагина, то доступ должен быть корректным.
            // Если у вас есть локальный класс VIDE_Data, может потребоваться уточнение.
            if (!VIDE_Data.VIDE_Data.isActive) // Возвращаем оригинальный способ доступа
            {
                int nextNodeId = -1; 
                if (!string.IsNullOrEmpty(npcId) && DialogueStartNodeQueue.Instance != null)
                {
                    if (DialogueStartNodeQueue.Instance.TryDequeueNextStartNode(npcId, out int dequeuedId))
                    {
                        nextNodeId = dequeuedId; 
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
            
            // Если в BaseInteractable.OnInteract() есть важная логика (например, вызов onInteractEvent),
            // которую вы хотите сохранить, вы можете вызвать base.OnInteract();
            // base.OnInteract(); 
            // Но в вашем случае, похоже, вся логика взаимодействия с диалогом здесь, и это нормально.
        }
        
        // OnPlayerEnter и OnPlayerExit будут унаследованы от BaseInteractable
        // и будут вызываться RealityPlayerController/ConsciousnessController,
        // что приведет к срабатыванию событий PlayerEnteredZone/PlayerExitedZone
        // для InteractableOutline.
    }
} 