using UnityEngine;
using BreakTheCycle;
using TMPro;

namespace DungeonGeneration.Scripts
{
    public class InteractablePortal : MonoBehaviour, IInteractable
    {
        [Header("Interaction Settings")]
        [SerializeField] private float interactionRadius = 2f;
        [SerializeField] private Sprite interactionPromptSprite;
        [SerializeField] private Vector2 promptSize = new Vector2(200, 50);
        [SerializeField] private Color interactionRadiusColor = new Color(0f, 1f, 0f, 0.2f);
        [SerializeField] private string defaultPromptText = "Нажмите F для использования портала";
        [SerializeField] private string notClearedRoomText = "Нужно победить всех врагов в комнате!";
        
        [Header("Portal Settings")]
        [SerializeField] private string nextRoomId;
        [SerializeField] private float pushForce = 5f;
        [SerializeField] private float pushDuration = 0.5f;

        private RoomManager currentRoom;
        private RoomManager targetRoom;
        private bool isInRange;
        private bool isInteractable = true;
        private DungeonGenerator dungeonGenerator;
        private GameObject player;
        private Rigidbody playerRb;
        private Vector3 pushDirection;
        private bool isPushing = false;
        private float pushTimer = 0f;
        private SpriteRenderer promptRenderer;
        private GameObject interactionPrompt;
        private bool showDebugGizmos = true;
        private float lastLogTime = 0f;
        private const float LOG_INTERVAL = 1f; // Интервал логирования в секундах

        public float InteractionRadius => interactionRadius;
        public bool IsInRange => isInRange;
        public bool IsInteractable => isInteractable;

        private void Start()
        {
            currentRoom = GetComponentInParent<RoomManager>();
            if (currentRoom == null)
            {
                Debug.LogWarning("[InteractablePortal] RoomManager не найден!");
            }

            dungeonGenerator = FindObjectOfType<DungeonGenerator>();
            if (dungeonGenerator == null)
            {
                Debug.LogWarning("[InteractablePortal] DungeonGenerator не найден!");
            }

            // Создаем объект подсказки только если спрайт назначен
            if (interactionPromptSprite != null)
            {
                CreateInteractionPrompt();
            }
            else
            {
                Debug.Log("[InteractablePortal] Спрайт подсказки не назначен, визуальная подсказка не будет отображаться");
            }

            // Подписываемся на событие создания игрока
            DungeonGenerator.OnPlayerCreated += HandlePlayerCreated;
            
            // Изначально скрываем подсказку
            HideInteractionPrompt();
        }

        private void OnDestroy()
        {
            // Отписываемся от события при уничтожении
            DungeonGenerator.OnPlayerCreated -= HandlePlayerCreated;
        }

        private void CreateInteractionPrompt()
        {
            // Создаем объект подсказки только если спрайт назначен
            if (interactionPromptSprite == null) 
            {
                Debug.Log("[InteractablePortal] Спрайт подсказки не назначен!");
                return;
            }

            interactionPrompt = new GameObject("InteractionPrompt");
            interactionPrompt.transform.SetParent(transform);
            interactionPrompt.transform.localPosition = Vector3.up * 2f; // Размещаем подсказку над порталом
            
            // Добавляем компонент SpriteRenderer
            promptRenderer = interactionPrompt.AddComponent<SpriteRenderer>();
            promptRenderer.sprite = interactionPromptSprite;
            promptRenderer.sortingOrder = 10; // Гарантируем, что подсказка будет поверх других объектов
            
            // Настраиваем размер
            interactionPrompt.transform.localScale = new Vector3(
                promptSize.x / 100f, 
                promptSize.y / 100f, 
                1f
            );
            
            Debug.Log("[InteractablePortal] Создана подсказка взаимодействия");
        }

        private void HandlePlayerCreated(GameObject playerObject)
        {
            player = playerObject;
            playerRb = player.GetComponent<Rigidbody>();
            if (playerRb == null)
            {
                Debug.LogWarning("[InteractablePortal] Rigidbody не найден на игроке!");
            }
            Debug.Log("[InteractablePortal] Игрок найден");
        }

        private void Update()
        {
            if (player == null) return;

            // Проверяем расстояние до игрока
            float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
            bool wasInRange = isInRange;
            isInRange = distanceToPlayer <= interactionRadius;

            // Периодическое логирование, если игрок в зоне взаимодействия
            if (isInRange && Time.time - lastLogTime >= LOG_INTERVAL)
            {
                Debug.Log($"[InteractablePortal] Игрок находится в зоне взаимодействия. Расстояние: {distanceToPlayer:F2} м");
                lastLogTime = Time.time;
            }

            // Обновляем отображение подсказки в зависимости от расстояния
            if (isInRange && !wasInRange)
            {
                ShowInteractionPrompt();
            }
            else if (!isInRange && wasInRange)
            {
                HideInteractionPrompt();
            }

            // Обрабатываем отталкивание
            if (isPushing)
            {
                pushTimer += Time.deltaTime;
                if (pushTimer < pushDuration && playerRb != null)
                {
                    playerRb.AddForce(pushDirection * pushForce, ForceMode.Force);
                }
                else
                {
                    isPushing = false;
                    pushTimer = 0f;
                }
            }
        }

        public void OnInteract()
        {
            Debug.Log("[InteractablePortal] Начало взаимодействия с порталом");
            
            // Проверяем, зачищена ли текущая комната
            if (currentRoom != null && !currentRoom.IsRoomCleared)
            {
                Debug.Log("[InteractablePortal] Комната не зачищена! Нужно победить всех врагов.");
                ShowInteractionPrompt();
                return;
            }

            if (dungeonGenerator == null)
            {
                dungeonGenerator = FindObjectOfType<DungeonGenerator>();
                if (dungeonGenerator == null)
                {
                    Debug.LogError("[InteractablePortal] DungeonGenerator не найден!");
                    return;
                }
            }

            // Если nextRoomId не назначен, выбираем случайную комнату
            if (string.IsNullOrEmpty(nextRoomId))
            {
                var availableRooms = dungeonGenerator.GetAvailableRooms();
                Debug.Log($"[InteractablePortal] Доступных комнат: {(availableRooms != null ? availableRooms.Count : 0)}");
                
                if (availableRooms != null && availableRooms.Count > 0)
                {
                    nextRoomId = availableRooms[Random.Range(0, availableRooms.Count)].Id;
                    Debug.Log($"[InteractablePortal] Выбрана случайная комната с ID: {nextRoomId}");
                }
                else
                {
                    Debug.LogError("[InteractablePortal] Нет доступных комнат для телепортации!");
                    return;
                }
            }

            // Проверяем игрока
            if (player == null)
            {
                player = GameObject.FindGameObjectWithTag("Player");
                if (player == null)
                {
                    Debug.LogError("[InteractablePortal] Игрок не найден для телепортации!");
                    return;
                }
            }

            Debug.Log($"[InteractablePortal] Попытка телепортации в комнату: {nextRoomId}, игрок: {player.name}, текущая позиция: {player.transform.position}");
            dungeonGenerator.LoadRoom(nextRoomId);
            Debug.Log("[InteractablePortal] Вызов телепортации завершен");
        }

        public void ShowInteractionPrompt()
        {
            ShowInteractionPrompt(currentRoom != null && !currentRoom.IsRoomCleared ? notClearedRoomText : defaultPromptText);
        }

        public void ShowInteractionPrompt(string text)
        {
            if (interactionPrompt != null)
            {
                interactionPrompt.SetActive(true);
                var textComponent = interactionPrompt.GetComponentInChildren<TextMeshProUGUI>();
                if (textComponent != null)
                {
                    textComponent.text = text;
                }
            }
        }

        public void HideInteractionPrompt()
        {
            if (interactionPrompt != null)
            {
                interactionPrompt.SetActive(false);
            }
        }

        public void OnInteractionStart()
        {
            Debug.Log("[InteractablePortal] Начало взаимодействия");
        }

        public void OnInteractionEnd()
        {
            Debug.Log("[InteractablePortal] Конец взаимодействия");
        }

        public void OnInteractionComplete()
        {
            Debug.Log("[InteractablePortal] Взаимодействие завершено");
        }

        public void OnInteractionCancel()
        {
            Debug.Log("[InteractablePortal] Взаимодействие отменено");
        }

        public bool CheckInteractionRange(Vector3 position)
        {
            if (player == null)
            {
                player = GameObject.FindGameObjectWithTag("Player");
                if (player == null)
                {
                    Debug.LogWarning("[InteractablePortal] Игрок не найден!");
                    return false;
                }
            }

            float distance = Vector3.Distance(transform.position, position);
            bool isInRange = distance <= interactionRadius;

            if (isInRange)
            {
                Debug.Log($"[InteractablePortal] Игрок находится в области действия портала. Расстояние: {distance:F2}, Максимальное расстояние: {interactionRadius}");
            }

            return isInRange;
        }

        public void SetInteractionRange(float range)
        {
            interactionRadius = range;
        }

        private void OnDrawGizmos()
        {
            if (showDebugGizmos)
            {
                Gizmos.color = interactionRadiusColor;
                Gizmos.DrawSphere(transform.position, interactionRadius);
            }
        }
    }
} 