using UnityEngine;
using UnityEngine.SceneManagement;
using DungeonGeneration;
using DungeonGeneration.ScriptableObjects;

namespace BreakTheCycle
{
    public class DoorInteraction : InteractableObject
    {
        [Header("Room Settings")]
        [SerializeField] private RoomTypeSO[] roomTypes;
        [SerializeField] private KeyCode interactionKey = KeyCode.F;

        private RoomManager roomManager;
        private DungeonGenerator dungeonGenerator;
        private bool isPlayerInRange = false;

        private void Start()
        {
            Debug.Log("DoorInteraction: Start начат");
            
            // Ищем RoomManager в родительских объектах
            roomManager = GetComponentInParent<RoomManager>();
            if (roomManager == null)
            {
                Debug.LogError("RoomManager не найден в родительских объектах!");
                // Пробуем найти в сцене
                roomManager = FindObjectOfType<RoomManager>();
                if (roomManager == null)
                {
                    Debug.LogError("RoomManager не найден в сцене!");
                    return;
                }
                Debug.Log("RoomManager найден в сцене");
            }
            else
            {
                Debug.Log($"RoomManager найден в родительском объекте. Тип комнаты: {roomManager.RoomType?.roomType}");
            }

            // Ищем DungeonGenerator в сцене
            dungeonGenerator = FindObjectOfType<DungeonGenerator>();
            if (dungeonGenerator == null)
            {
                Debug.LogError("DungeonGenerator не найден в сцене!");
                return;
            }
            Debug.Log("DungeonGenerator найден");
            
            // Проверяем коллайдер
            Collider doorCollider = GetComponent<Collider>();
            if (doorCollider == null)
            {
                Debug.LogError("На двери отсутствует коллайдер!");
                // Создаем BoxCollider если его нет
                doorCollider = gameObject.AddComponent<BoxCollider>();
            }
            
            // Настраиваем коллайдер
            doorCollider.isTrigger = true;
            // Устанавливаем размер коллайдера
            if (doorCollider is BoxCollider boxCollider)
            {
                boxCollider.size = new Vector3(2f, 2f, 2f); // Увеличиваем размер для более надежного определения входа
                boxCollider.center = Vector3.zero;
            }
            
            Debug.Log($"Коллайдер настроен. Is Trigger: {doorCollider.isTrigger}, Size: {doorCollider.bounds.size}");
        }

        private void Update()
        {
            if (isPlayerInRange)
            {
                Debug.Log($"Игрок в зоне взаимодействия. Нажмите {interactionKey} для взаимодействия");
                if (Input.GetKeyDown(interactionKey))
                {
                    Debug.Log($"Нажата клавиша {interactionKey}");
                    Debug.Log($"RoomManager: {(roomManager != null ? "найден" : "не найден")}");
                    Debug.Log($"DungeonGenerator: {(dungeonGenerator != null ? "найден" : "не найден")}");
                    Interact();
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                Debug.Log($"Игрок вошел в зону взаимодействия. Тег объекта: {other.tag}");
                isPlayerInRange = true;
            }
            else
            {
                Debug.Log($"Объект вошел в триггер, но это не игрок. Тег: {other.tag}");
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                Debug.Log($"Игрок вышел из зоны взаимодействия. Тег объекта: {other.tag}");
                isPlayerInRange = false;
            }
            else
            {
                Debug.Log($"Объект вышел из триггера, но это не игрок. Тег: {other.tag}");
            }
        }

        public override void Interact()
        {
            Debug.Log("Начало взаимодействия с дверью");
            
            if (roomManager == null || dungeonGenerator == null)
            {
                Debug.LogError("RoomManager или DungeonGenerator не найдены!");
                return;
            }

            // Проверяем, можно ли открыть дверь
            bool canOpenDoor = roomManager.RoomType != null && 
                (roomManager.RoomType.roomType == DungeonGeneration.ScriptableObjects.RoomType.Start || 
                 roomManager.AreAllEnemiesDefeated());

            if (!canOpenDoor)
            {
                Debug.Log($"Дверь нельзя открыть: не все враги побеждены или это не стартовая комната. Тип комнаты: {roomManager.RoomType?.roomType}");
                return;
            }

            Debug.Log($"Тип комнаты: {roomManager.RoomType?.roomType}");
            
            // Получаем направление двери
            Vector2Int direction = GetDoorDirection();
            
            // Получаем позицию текущей комнаты
            Vector2Int currentPosition = new Vector2Int(
                Mathf.RoundToInt(transform.parent.position.x / dungeonGenerator.RoomSpacing),
                Mathf.RoundToInt(transform.parent.position.z / dungeonGenerator.RoomSpacing)
            );

            Debug.Log($"Текущая позиция комнаты: {currentPosition}, Направление: {direction}");

            // Получаем ID следующей комнаты
            string nextRoomId = dungeonGenerator.GetNextRoomId(currentPosition, direction);
            
            if (!string.IsNullOrEmpty(nextRoomId))
            {
                Debug.Log($"Загрузка следующей комнаты с ID: {nextRoomId}");
                roomManager.LoadNextRoom(nextRoomId);
            }
            else
            {
                Debug.LogWarning("Следующая комната не найдена!");
            }
        }

        private Vector2Int GetDoorDirection()
        {
            if (roomManager == null || dungeonGenerator == null)
            {
                Debug.LogError("RoomManager или DungeonGenerator не найдены в GetDoorDirection!");
                return Vector2Int.zero;
            }

            // Получаем позицию текущей комнаты
            Vector2Int currentPosition = new Vector2Int(
                Mathf.RoundToInt(transform.parent.position.x / dungeonGenerator.RoomSpacing),
                Mathf.RoundToInt(transform.parent.position.z / dungeonGenerator.RoomSpacing)
            );

            Debug.Log($"Текущая позиция комнаты: {currentPosition}");

            // Получаем текущую комнату
            RoomNode currentRoom = dungeonGenerator.GetRoomNodeAtPosition(transform.parent.position);
            if (currentRoom == null)
            {
                Debug.LogError("Текущая комната не найдена!");
                return Vector2Int.zero;
            }

            Debug.Log($"Найдено соединенных комнат: {currentRoom.ConnectedRooms.Count}");

            // Находим следующую комнату из соединенных комнат
            foreach (var connectedRoom in currentRoom.ConnectedRooms)
            {
                // Определяем направление к следующей комнате
                Vector2Int direction = connectedRoom.Position - currentPosition;
                
                // Проверяем, что направление соответствует одной из сторон (вверх, вниз, влево, вправо)
                if ((direction.x == 0 && Mathf.Abs(direction.y) == 1) || 
                    (direction.y == 0 && Mathf.Abs(direction.x) == 1))
                {
                    Debug.Log($"Найдена следующая комната в направлении: {direction}");
                    return direction;
                }
                else
                {
                    Debug.LogWarning($"Комната найдена, но направление не соответствует стороне: {direction}");
                }
            }

            Debug.LogWarning("Соединенные комнаты не найдены или недоступны!");
            return Vector2Int.zero;
        }
    }
} 