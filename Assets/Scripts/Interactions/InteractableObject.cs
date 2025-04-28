using UnityEngine;
using VIDE_Data;
using BreakTheCycle.Dialogue;

namespace BreakTheCycle
{
    /// <summary>
    /// Интерактивный объект, взаимодействие с которым происходит через диалоговую систему VIDE.
    /// </summary>
    public class InteractableObject : BaseInteractable
    {
        private VIDE_Assign videAssign;
        private Transform playerTransform;

        private void Awake()
        {
            // Пытаемся найти компонент VIDE_Assign на объекте
            videAssign = GetComponent<VIDE_Assign>();
        }

        /// <summary>
        /// Проверяет, находится ли игрок в радиусе взаимодействия
        /// </summary>
        public bool IsPlayerInRange(Transform player)
        {
            if (!IsInteractable) return false;
            float distance = Vector3.Distance(transform.position, player.position);
            return distance <= InteractionRadius;
        }

        /// <summary>
        /// Взаимодействие с объектом: запускает диалог через VIDE, если компонент VIDE_Assign присутствует
        /// </summary>
        public override void OnInteract()
        {
            Debug.Log($"[InteractableObject] OnInteract вызван для {gameObject.name}");
            if (!IsInteractable)
            {
                Debug.Log($"[InteractableObject] Объект {gameObject.name} не интерактивен (IsInteractable = false)");
                return;
            }

            var trigger = GetComponent<BreakTheCycle.Dialogue.MonologueTriggerData>();
            if (trigger == null)
            {
                Debug.Log($"[InteractableObject] MonologueTriggerData не найден на {gameObject.name}");
                return;
            }
            Debug.Log($"[InteractableObject] MonologueTriggerData найден на {gameObject.name}, monologueID = {trigger.monologueID}");

            if (trigger.monologueID < 0)
            {
                Debug.Log($"[InteractableObject] Некорректный monologueID ({trigger.monologueID}) на {gameObject.name}");
                return;
            }

            var manager = FindObjectOfType<MonologueManager>();
            if (manager == null)
            {
                Debug.Log($"[InteractableObject] MonologueManager не найден в сцене!");
                return;
            }
            Debug.Log($"[InteractableObject] MonologueManager найден, вызываю PlayMonologue({trigger.monologueID})");
            manager.PlayMonologue(trigger.monologueID);
            Debug.Log($"[InteractableObject] PlayMonologue({trigger.monologueID}) вызван");
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, InteractionRadius);
        }
#endif
    }
} 