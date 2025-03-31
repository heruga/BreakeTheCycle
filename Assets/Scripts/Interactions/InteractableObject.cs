using UnityEngine;
using UnityEngine.Events;
using System;

namespace BreakTheCycle
{
    /// <summary>
    /// Базовый класс для всех интерактивных объектов в игре
    /// </summary>
    public class InteractableObject : MonoBehaviour
    {
        [Header("Основные настройки")]
        [SerializeField] private string objectId; // Уникальный ID объекта
        [SerializeField] private string displayName = "Объект"; // Отображаемое имя
        [SerializeField] private float interactionRadius = 2f; // Радиус взаимодействия
        [SerializeField] private bool canInteractInReality = true; // Можно ли взаимодействовать в "Реальности"
        [SerializeField] private bool canInteractInConsciousness = true; // Можно ли взаимодействовать в "Сознании"
        
        // Публичное свойство для доступа к радиусу взаимодействия
        public float InteractionRadius
        {
            get => interactionRadius;
            set => interactionRadius = value;
        }
        
        [Header("Взаимодействие")]
        [SerializeField] private UnityEvent onInteractEvent; // События при взаимодействии
        [SerializeField] private string interactionText = "Нажмите F для взаимодействия"; // Текст подсказки
        
        [Header("Описание")]
        [SerializeField] private string objectDescription = "Описание объекта...";
        [SerializeField] private AudioClip interactionSound; // Звук при взаимодействии
        
        private bool isInteractable = true; // Можно ли сейчас взаимодействовать с объектом
        private Collider objectCollider; // Коллайдер объекта
        private Renderer objectRenderer; // Рендерер объекта
        
        private void Awake()
        {
            // Генерируем уникальный ID, если он не задан
            if (string.IsNullOrEmpty(objectId))
            {
                objectId = Guid.NewGuid().ToString();
            }
            
            objectCollider = GetComponent<Collider>();
            objectRenderer = GetComponent<Renderer>();
        }
        
        /// <summary>
        /// Проверяет, находится ли игрок в радиусе взаимодействия
        /// </summary>
        public virtual bool IsPlayerInRange(Transform playerTransform)
        {
            if (!isInteractable) return false;
            
            // Проверяем, в правильном ли мы состоянии для взаимодействия
            if (GameManager.Instance != null)
            {
                var currentState = GameManager.Instance.GetCurrentState();
                switch (currentState)
                {
                    case GameManager.GameState.Reality when !canInteractInReality:
                    case GameManager.GameState.Consciousness when !canInteractInConsciousness:
                        return false;
                }
            }
            
            // Проверяем расстояние до игрока
            float distance = Vector3.Distance(transform.position, playerTransform.position);
            return distance <= interactionRadius;
        }
        
        /// <summary>
        /// Взаимодействие с объектом
        /// </summary>
        public virtual void Interact()
        {
            if (!isInteractable) return;
            
            // Проверяем, не открыт ли уже просмотрщик
            if (ObjectViewer.Instance != null && ObjectViewer.Instance.IsViewerActive())
            {
                Debug.Log("InteractableObject: Просмотрщик уже открыт, игнорируем взаимодействие");
                return;
            }
            
            // Выводим сообщение в консоль
            Debug.Log($"Игрок взаимодействует с объектом: {displayName}");
            
            // Воспроизводим звук взаимодействия, если он задан
            if (interactionSound != null)
            {
                AudioSource.PlayClipAtPoint(interactionSound, transform.position);
            }
            
            // Вызываем события взаимодействия
            onInteractEvent?.Invoke();
            
            // Показываем объект в просмотрщике
            if (ObjectViewer.Instance != null)
            {
                ObjectViewer.Instance.ShowObject(gameObject, objectDescription);
            }
            else
            {
                Debug.LogWarning("ObjectViewer не найден! Окно просмотра не будет открыто.");
            }
        }
        
        /// <summary>
        /// Делает объект интерактивным или неинтерактивным
        /// </summary>
        public void SetInteractable(bool value)
        {
            isInteractable = value;
        }
        
        /// <summary>
        /// Возвращает ID объекта
        /// </summary>
        public string GetId()
        {
            return objectId;
        }
        
        /// <summary>
        /// Возвращает отображаемое имя объекта
        /// </summary>
        public string GetDisplayName()
        {
            return displayName;
        }
        
        /// <summary>
        /// Возвращает текст подсказки для взаимодействия
        /// </summary>
        public virtual string GetInteractionText()
        {
            return interactionText;
        }
        
        /// <summary>
        /// Получение текущего состояния объекта для сохранения
        /// </summary>
        public virtual object GetState()
        {
            // Базовое состояние, которое нужно сохранить
            InteractableState state = new InteractableState();
            state.position = transform.position;
            state.rotation = transform.rotation;
            state.isActive = gameObject.activeSelf;
            state.isInteractable = isInteractable;
            
            return state;
        }
        
        /// <summary>
        /// Восстановление состояния объекта
        /// </summary>
        public virtual void RestoreState(object state)
        {
            if (state is InteractableState savedState)
            {
                transform.position = savedState.position;
                transform.rotation = savedState.rotation;
                gameObject.SetActive(savedState.isActive);
                isInteractable = savedState.isInteractable;
            }
        }
        
        // Визуализация радиуса взаимодействия в редакторе
        protected virtual void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, interactionRadius);
        }
        
        /// <summary>
        /// Класс для хранения базового состояния интерактивного объекта
        /// </summary>
        [Serializable]
        public class InteractableState
        {
            public Vector3 position;
            public Quaternion rotation;
            public bool isActive;
            public bool isInteractable;
        }
    }
} 