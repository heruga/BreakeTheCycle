using UnityEngine;
using VIDE_Data;

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
            if (VD.isActive)
            {
                VD.EndDialogue();
            }

            // Запускаем диалог через VIDE
            VD.BeginDialogue(videAssign);
        }
    }
} 