using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using DungeonGeneration.ScriptableObjects;
using System.Collections;

namespace DungeonGeneration
{
    public class DungeonGenerator : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private DungeonConfigSO dungeonConfig;
        [SerializeField] private Transform dungeonContainer;

        private List<RoomNode> rooms;
        private RoomNode startRoom;
        private RoomNode bossRoom;
        private int currentDifficulty;
        private bool isGenerating = false;

        private void Start()
        {
            Debug.Log("[DungeonGenerator] Start начат");
            if (dungeonConfig == null)
            {
                Debug.LogError("[DungeonGenerator] DungeonConfig не назначен!");
                return;
            }

            if (dungeonContainer == null)
            {
                Debug.LogError("[DungeonGenerator] DungeonContainer не назначен!");
                return;
            }

            Debug.Log("[DungeonGenerator] Начинаем асинхронную генерацию подземелья");
            StartCoroutine(GenerateDungeonAsync());
        }

        private IEnumerator GenerateDungeonAsync()
        {
            if (isGenerating)
            {
                Debug.LogWarning("[DungeonGenerator] Генерация уже выполняется!");
                yield break;
            }

            isGenerating = true;
            Debug.Log("[DungeonGenerator] Начало асинхронной генерации подземелья");

            // Очищаем предыдущую генерацию
            ClearDungeon();
            yield return null;

            // Инициализируем новую генерацию
            rooms = new List<RoomNode>();
            currentDifficulty = dungeonConfig.startingDifficulty;
            Debug.Log($"[DungeonGenerator] Текущая сложность: {currentDifficulty}");

            // Генерируем комнаты
            Debug.Log("[DungeonGenerator] Начинаем генерацию расположения комнат");
            GenerateRoomLayout();
            yield return null;

            // Создаем физические комнаты
            Debug.Log("[DungeonGenerator] Начинаем создание физических комнат");
            foreach (var room in rooms)
            {
                if (room.RoomTemplate == null || room.RoomTemplate.roomPrefab == null)
                {
                    Debug.LogError($"[DungeonGenerator] Отсутствует префаб для комнаты типа {room.RoomType.name}");
                    continue;
                }

                // Используем roomSpacing вместо roomSize для расстояния между комнатами
                Vector3 position = new Vector3(room.Position.x * dungeonConfig.roomSpacing, 0, room.Position.y * dungeonConfig.roomSpacing);
                GameObject roomInstance = Instantiate(room.RoomTemplate.roomPrefab, position, Quaternion.identity, dungeonContainer);
                room.RoomInstance = roomInstance;
                Debug.Log($"[DungeonGenerator] Создана комната типа {room.RoomType.name} на позиции {position}");
                yield return null;
            }

            // Настраиваем спавн врагов и наград
            Debug.Log("[DungeonGenerator] Начинаем настройку содержимого комнат");
            foreach (var room in rooms)
            {
                if (room.RoomInstance == null) continue;

                // Настраиваем врагов для боевых комнат
                if (room.RoomType.roomType == RoomType.Combat || room.RoomType.roomType == RoomType.Boss)
                {
                    Debug.Log($"[DungeonGenerator] Настройка врагов для комнаты типа {room.RoomType.name}");
                    SetupEnemies(room);
                }

                // Настраиваем награды для комнат с наградами
                if (room.RoomType.roomType == RoomType.Reward)
                {
                    Debug.Log($"[DungeonGenerator] Настройка наград для комнаты типа {room.RoomType.name}");
                    SetupRewards(room);
                }
                yield return null;
            }

            // Создаем игрока в точке спавна
            Debug.Log("[DungeonGenerator] Создание игрока в точке спавна");
            if (startRoom != null && startRoom.RoomInstance != null)
            {
                Transform spawnPoint = startRoom.RoomInstance.transform.Find("Player Spawn Point");
                if (spawnPoint != null)
                {
                    // Ищем существующего игрока в сцене
                    GameObject existingPlayer = GameObject.FindGameObjectWithTag("Player");
                    if (existingPlayer != null)
                    {
                        // Перемещаем существующего игрока в точку спавна
                        existingPlayer.transform.position = spawnPoint.position;
                        existingPlayer.transform.rotation = spawnPoint.rotation;
                        Debug.Log("[DungeonGenerator] Существующий игрок перемещен в точку спавна");
                    }
                    else
                    {
                        // Создаем нового игрока, если его нет
                        GameObject playerPrefab = Resources.Load<GameObject>("Player");
                        if (playerPrefab != null)
                        {
                            GameObject player = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
                            player.tag = "Player";
                            Debug.Log("[DungeonGenerator] Новый игрок создан в точке спавна");
                        }
                        else
                        {
                            Debug.LogError("[DungeonGenerator] Не удалось загрузить префаб игрока из Resources");
                        }
                    }
                }
                else
                {
                    Debug.LogError("[DungeonGenerator] Не найдена точка спавна игрока в стартовой комнате");
                }
            }
            else
            {
                Debug.LogError("[DungeonGenerator] Стартовая комната не создана");
            }
            yield return null;

            Debug.Log("[DungeonGenerator] Генерация подземелья завершена");
            isGenerating = false;
        }

        private void GenerateRoomLayout()
        {
            Debug.Log("[DungeonGenerator] Начало GenerateRoomLayout");
            // Определяем количество комнат для этого забега
            int roomCount = Random.Range(dungeonConfig.minRoomsPerLevel, dungeonConfig.maxRoomsPerLevel + 1);
            Debug.Log($"[DungeonGenerator] Количество комнат: {roomCount}");

            // Создаем стартовую комнату
            Debug.Log("[DungeonGenerator] Поиск позиции для стартовой комнаты");
            Vector2Int startPosition = FindValidPosition();
            Debug.Log($"[DungeonGenerator] Позиция стартовой комнаты: {startPosition}");

            Debug.Log("[DungeonGenerator] Поиск шаблона для стартовой комнаты");
            RoomTemplateSO startTemplate = GetRandomCompatibleTemplate(dungeonConfig.startRoomType);
            if (startTemplate == null)
            {
                Debug.LogError("[DungeonGenerator] Не удалось найти шаблон для стартовой комнаты!");
                return;
            }
            Debug.Log($"[DungeonGenerator] Найден шаблон стартовой комнаты: {startTemplate.name}");

            startRoom = new RoomNode(startPosition, dungeonConfig.startRoomType, startTemplate);
            rooms.Add(startRoom);
            Debug.Log("[DungeonGenerator] Создана стартовая комната");

            // Генерируем остальные комнаты
            for (int i = 1; i < roomCount; i++)
            {
                Debug.Log($"[DungeonGenerator] Генерация комнаты {i + 1}/{roomCount}");
                Vector2Int position = FindValidPosition();
                Debug.Log($"[DungeonGenerator] Позиция комнаты {i + 1}: {position}");

                RoomTypeSO roomType = DetermineRoomType(i, roomCount);
                Debug.Log($"[DungeonGenerator] Тип комнаты {i + 1}: {roomType?.name ?? "null"}");

                if (roomType == null)
                {
                    Debug.LogWarning($"[DungeonGenerator] Не удалось определить тип комнаты {i + 1}");
                    continue;
                }

                RoomTemplateSO template = GetRandomCompatibleTemplate(roomType);
                if (template == null)
                {
                    Debug.LogWarning($"[DungeonGenerator] Не удалось найти шаблон для комнаты типа {roomType.name}");
                    continue;
                }
                Debug.Log($"[DungeonGenerator] Найден шаблон для комнаты {i + 1}: {template.name}");

                RoomNode newRoom = new RoomNode(position, roomType, template);
                rooms.Add(newRoom);
                ConnectToNearestRoom(newRoom);
                Debug.Log($"[DungeonGenerator] Комната {i + 1} создана и подключена");
            }

            // Убеждаемся, что к боссовой комнате есть доступ
            Debug.Log("[DungeonGenerator] Проверка доступа к боссовой комнате");
            EnsureBossRoomAccess();
        }

        private RoomTypeSO DetermineRoomType(int currentIndex, int totalRooms)
        {
            // Если это последняя комната, это боссовая комната
            if (currentIndex == totalRooms - 1)
            {
                return dungeonConfig.bossRoomType;
            }

            // Проверяем, что у нас есть необходимые типы комнат
            if (dungeonConfig.combatRoomType == null)
            {
                Debug.LogWarning("Тип боевой комнаты не назначен!");
                return null;
            }

            // Определяем тип комнаты на основе вероятностей
            float randomValue = Random.value;
            float totalChance = dungeonConfig.combatRoomChance;
            
            if (randomValue < totalChance)
            {
                return dungeonConfig.combatRoomType;
            }
            
            // Проверяем награды и магазин только если они настроены
            if (dungeonConfig.rewardRoomType != null)
            {
                totalChance += dungeonConfig.rewardRoomChance;
                if (randomValue < totalChance)
                {
                    return dungeonConfig.rewardRoomType;
                }
            }

            if (dungeonConfig.shopRoomType != null)
            {
                totalChance += dungeonConfig.shopRoomChance;
                if (randomValue < totalChance)
                {
                    return dungeonConfig.shopRoomType;
                }
            }

            // Если ни один из дополнительных типов не доступен, возвращаем боевую комнату
            return dungeonConfig.combatRoomType;
        }

        private RoomTemplateSO GetRandomCompatibleTemplate(RoomTypeSO roomType)
        {
            if (roomType == null || dungeonConfig.roomTemplates == null || dungeonConfig.roomTemplates.Length == 0)
            {
                Debug.LogWarning("Не найдены шаблоны комнат!");
                return null;
            }

            List<RoomTemplateSO> compatibleTemplates = new List<RoomTemplateSO>();
            foreach (var template in dungeonConfig.roomTemplates)
            {
                if (template != null && template.compatibleRoomTypes.Contains(roomType))
                {
                    compatibleTemplates.Add(template);
                }
            }

            if (compatibleTemplates.Count == 0)
            {
                Debug.LogWarning($"Не найдены совместимые шаблоны для типа комнаты {roomType.name}");
                return null;
            }

            return compatibleTemplates[Random.Range(0, compatibleTemplates.Count)];
        }

        private Vector2Int FindValidPosition()
        {
            Debug.Log("[DungeonGenerator] Поиск валидной позиции");
            Vector2Int position;
            bool validPosition;
            int attempts = 0;
            const int maxAttempts = 100; // Максимальное количество попыток

            do
            {
                // Увеличиваем диапазон генерации позиций
                position = new Vector2Int(
                    Random.Range(-100, 160),
                    Random.Range(-150, 160)
                );
                validPosition = true;

                foreach (var room in rooms)
                {
                    // Используем roomSpacing для проверки расстояния между комнатами
                    float distance = Vector2Int.Distance(position, room.Position);
                    if (distance < dungeonConfig.roomSpacing / 2f) // Делим на 2, так как roomSpacing это расстояние между центрами комнат
                    {
                        validPosition = false;
                        break;
                    }
                }

                attempts++;
                if (attempts >= maxAttempts)
                {
                    Debug.LogWarning($"[DungeonGenerator] Достигнуто максимальное количество попыток ({maxAttempts}) поиска валидной позиции");
                    // Возвращаем позицию (0,0) если не удалось найти валидную
                    return Vector2Int.zero;
                }
            } while (!validPosition);

            Debug.Log($"[DungeonGenerator] Найдена валидная позиция: {position} после {attempts} попыток");
            return position;
        }

        private void ConnectToNearestRoom(RoomNode newRoom)
        {
            RoomNode nearestRoom = null;
            float minDistance = float.MaxValue;

            foreach (var room in rooms)
            {
                if (room == newRoom) continue;

                float distance = Vector2Int.Distance(newRoom.Position, room.Position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestRoom = room;
                }
            }

            if (nearestRoom != null)
            {
                newRoom.AddConnection(nearestRoom);
                nearestRoom.AddConnection(newRoom);
            }
        }

        private void EnsureBossRoomAccess()
        {
            // Находим боссовую комнату
            bossRoom = rooms.Find(r => r.RoomType.roomType == RoomType.Boss);
            if (bossRoom == null) return;

            // Убеждаемся, что к ней есть путь
            if (!HasPathToBoss())
            {
                // Соединяем с ближайшей комнатой, если нет пути
                ConnectToNearestRoom(bossRoom);
            }
        }

        private bool HasPathToBoss()
        {
            HashSet<RoomNode> visited = new HashSet<RoomNode>();
            Queue<RoomNode> queue = new Queue<RoomNode>();
            queue.Enqueue(startRoom);
            visited.Add(startRoom);

            while (queue.Count > 0)
            {
                RoomNode current = queue.Dequeue();
                if (current == bossRoom)
                {
                    return true;
                }

                foreach (var connected in current.ConnectedRooms)
                {
                    if (!visited.Contains(connected))
                    {
                        visited.Add(connected);
                        queue.Enqueue(connected);
                    }
                }
            }

            return false;
        }

        private void SetupEnemies(RoomNode room)
        {
            Debug.Log($"[DungeonGenerator] Начало SetupEnemies для комнаты типа {room.RoomType.name}");
            if (dungeonConfig.enemyConfigs == null || dungeonConfig.enemyConfigs.Length == 0)
            {
                Debug.LogError("[DungeonGenerator] Не настроены конфигурации врагов!");
                return;
            }

            // Находим точки спавна врагов
            var spawnPoints = room.RoomInstance.GetComponentsInChildren<Transform>()
                .Where(t => t.CompareTag("EnemySpawnPoint"))
                .ToList();
            Debug.Log($"[DungeonGenerator] Найдено точек спавна: {spawnPoints.Count}");

            foreach (var spawnPoint in spawnPoints)
            {
                if (Random.value < 0.5f) // 50% шанс спавна
                {
                    // Выбираем случайного врага из конфигураций
                    EnemyConfigSO enemyConfig = dungeonConfig.enemyConfigs[Random.Range(0, dungeonConfig.enemyConfigs.Length)];
                    if (enemyConfig != null && enemyConfig.enemyPrefab != null)
                    {
                        GameObject enemy = Instantiate(enemyConfig.enemyPrefab, spawnPoint.position, Quaternion.identity, room.RoomInstance.transform);
                        Debug.Log($"[DungeonGenerator] Создан враг типа {enemyConfig.name} на позиции {spawnPoint.position}");
                    }
                }
            }
        }

        private void SetupRewards(RoomNode room)
        {
            Debug.Log($"[DungeonGenerator] Начало SetupRewards для комнаты типа {room.RoomType.name}");
            // Здесь будет логика настройки наград
        }

        private void ClearDungeon()
        {
            Debug.Log("[DungeonGenerator] Очистка предыдущего подземелья");
            if (dungeonContainer != null)
            {
                foreach (Transform child in dungeonContainer)
                {
                    Destroy(child.gameObject);
                }
            }
            rooms?.Clear();
            startRoom = null;
            bossRoom = null;
            Debug.Log("[DungeonGenerator] Очистка завершена");
        }

        public void OnRoomCleared(RoomNode room)
        {
            room.IsCleared = true;
            // Здесь можно добавить логику для активации дверей или других эффектов
        }

        public RoomNode GetRoomNodeAtPosition(Vector3 worldPosition)
        {
            // Преобразуем мировые координаты в координаты сетки
            Vector2Int gridPosition = new Vector2Int(
                Mathf.RoundToInt(worldPosition.x / dungeonConfig.roomSpacing),
                Mathf.RoundToInt(worldPosition.z / dungeonConfig.roomSpacing)
            );

            // Ищем комнату с соответствующими координатами
            return rooms.FirstOrDefault(r => r.Position == gridPosition);
        }
    }
} 