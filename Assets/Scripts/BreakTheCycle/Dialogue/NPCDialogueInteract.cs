using UnityEngine;

namespace BreakTheCycle.Dialogue
{
    public class NPCDialogueInteract : MonoBehaviour, IInteractable
    {
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