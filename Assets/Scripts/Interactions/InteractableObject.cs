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
            if (!IsInteractable) return;
            if (videAssign == null)
            {
                // Нет диалога — ничего не делаем
                return;
            }

            // Проверяем, не запущен ли уже другой диалог
            if (VIDE_Data.VIDE_Data.isActive)
            {
                VIDE_Data.VIDE_Data.EndDialogue();
            }

            // Запускаем диалог через VIDE
            VIDE_Data.VIDE_Data.BeginDialogue(videAssign);

            // Воспроизводим монолог, если есть MonologueTriggerData
            var trigger = GetComponent<BreakTheCycle.Dialogue.MonologueTriggerData>();
            if (trigger != null && trigger.monologueID >= 0)
            {
                var manager = FindObjectOfType<MonologueManager>();
                if (manager != null)
                {
                    manager.PlayMonologue(trigger.monologueID);
                }
            }
        }
    }
} 