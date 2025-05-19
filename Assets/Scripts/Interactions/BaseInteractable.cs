using UnityEngine;
using UnityEngine.Events;
using System;

namespace BreakTheCycle
{
    /// Абстрактный базовый класс для всех интерактивных объектов.
    /// Содержит общую логику: радиус взаимодействия, доступность, событие взаимодействия.
    public abstract class BaseInteractable : MonoBehaviour, IInteractable
    {
        [Header("Основные настройки взаимодействия")]
        [SerializeField] protected float interactionRadius = 2f;
        [SerializeField] protected bool isInteractable = true;
        [SerializeField] protected UnityEvent onInteractEvent;

        // События для управления обводкой (переименованы, чтобы избежать конфликта)
        public event Action PlayerEnteredZone;
        public event Action PlayerExitedZone;

        /// Радиус, в котором игрок может взаимодействовать с объектом
        public float InteractionRadius => interactionRadius;

        /// Можно ли сейчас взаимодействовать с объектом
        public bool IsInteractable => isInteractable;

        /// Метод взаимодействия (реализация интерфейса)
        public virtual void OnInteract()
        {
            if (isInteractable)
            {
                onInteractEvent?.Invoke();
            }
        }

        /// Вызывается при входе игрока в зону взаимодействия
        public virtual void OnPlayerEnter()
        {
            // Вызываем событие входа
            PlayerEnteredZone?.Invoke();
        }

        /// Вызывается при выходе игрока из зоны взаимодействия
        public virtual void OnPlayerExit()
        {
            // Вызываем событие выхода
            PlayerExitedZone?.Invoke();
        }
    }
} 