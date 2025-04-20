using UnityEngine;
using UnityEngine.Events;

namespace BreakTheCycle
{
    /// <summary>
    /// Абстрактный базовый класс для всех интерактивных объектов.
    /// Содержит общую логику: радиус взаимодействия, доступность, событие взаимодействия.
    /// </summary>
    public abstract class BaseInteractable : MonoBehaviour, IInteractable
    {
        [Header("Основные настройки взаимодействия")]
        [SerializeField] protected float interactionRadius = 2f;
        [SerializeField] protected bool isInteractable = true;
        [SerializeField] protected UnityEvent onInteractEvent;

        /// <summary>
        /// Радиус, в котором игрок может взаимодействовать с объектом
        /// </summary>
        public float InteractionRadius => interactionRadius;

        /// <summary>
        /// Можно ли сейчас взаимодействовать с объектом
        /// </summary>
        public bool IsInteractable => isInteractable;

        /// <summary>
        /// Метод взаимодействия (реализация интерфейса)
        /// </summary>
        public virtual void OnInteract()
        {
            if (isInteractable)
            {
                onInteractEvent?.Invoke();
            }
        }

        /// <summary>
        /// Вызывается при входе игрока в зону взаимодействия
        /// </summary>
        public virtual void OnPlayerEnter()
        {
            // Для переопределения в наследниках
        }

        /// <summary>
        /// Вызывается при выходе игрока из зоны взаимодействия
        /// </summary>
        public virtual void OnPlayerExit()
        {
            // Для переопределения в наследниках
        }
    }
} 