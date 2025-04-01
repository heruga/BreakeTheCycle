using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using DungeonGeneration.ScriptableObjects;
using System.Collections;
using BreakTheCycle;

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

        public List<RoomNode> Rooms => rooms;
        public RoomNode StartRoom => startRoom;
        public RoomNode BossRoom => bossRoom;
        public float RoomSpacing => dungeonConfig.roomSpacing;

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
                
                // Проверяем содержимое комнаты
                Debug.Log($"[DungeonGenerator] Содержимое комнаты {room.RoomType.name}:");
                foreach (Transform child in roomInstance.transform)
                {
                    Debug.Log($"- {child.name} (тег: {child.tag})");
                    // Проверяем дочерние объекты
                    foreach (Transform grandChild in child)
                    {
                        Debug.Log($"  - {grandChild.name} (тег: {grandChild.tag})");
                        // Проверяем компоненты на двери
                        if (grandChild.name.Contains("Door"))
                        {
                            Debug.Log($"    Компоненты двери {grandChild.name}:");
                            var doorInteraction = grandChild.GetComponent<DoorInteraction>();
                            if (doorInteraction != null)
                            {
                                Debug.Log("    - Найден компонент DoorInteraction");
                            }
                            else
                            {
                                Debug.Log("    - Компонент DoorInteraction отсутствует!");
                            }
                            var collider = grandChild.GetComponent<Collider>();
                            if (collider != null)
                            {
                                Debug.Log($"    - Найден коллайдер типа {collider.GetType().Name}");
                                if (collider is BoxCollider boxCollider)
                                {
                                    Debug.Log($"    - Размер коллайдера: {boxCollider.size}");
                                    Debug.Log($"    - Is Trigger: {boxCollider.isTrigger}");
                                }
                            }
                            else
                            {
                                Debug.Log("    - Коллайдер отсутствует!");
                            }
                        }
                    }
                }
                
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
            // Находим стартовую комнату
            startRoom = rooms.FirstOrDefault(r => r.RoomType.roomType == RoomType.Start);
            if (startRoom != null)
            {
                Debug.Log($"[DungeonGenerator] Найдена стартовая комната: {startRoom.Id}");
                Debug.Log($"[DungeonGenerator] Имя стартовой комнаты: {startRoom.RoomInstance.name}");
                
                // Находим точку спавна в стартовой комнате
                Transform spawnPoint = startRoom.RoomInstance.transform.Find("Player Spawn Point");
                if (spawnPoint != null)
                {
                    Debug.Log($"[DungeonGenerator] Найдена точка спавна в стартовой комнате: {spawnPoint.position}");
                    Debug.Log($"[DungeonGenerator] Имя точки спавна: {spawnPoint.name}");
                    Debug.Log($"[DungeonGenerator] Тег точки спавна: {spawnPoint.tag}");
                    
                    // Находим существующего игрока или создаем нового
                    GameObject existingPlayer = GameObject.FindGameObjectWithTag("Player");
                    if (existingPlayer != null)
                    {
                        Debug.Log($"[DungeonGenerator] Найден существующий игрок: {existingPlayer.name}, позиция: {existingPlayer.transform.position}");
                        
                        // Отключаем физику на время перемещения
                        Rigidbody rb = existingPlayer.GetComponent<Rigidbody>();
                        if (rb != null)
                        {
                            rb.isKinematic = true;
                        }
                        
                        // Перемещаем игрока в точку спавна
                        existingPlayer.transform.position = spawnPoint.position;
                        existingPlayer.transform.rotation = spawnPoint.rotation;
                        
                        // Принудительно обновляем позицию
                        if (rb != null)
                        {
                            rb.position = spawnPoint.position;
                            rb.rotation = spawnPoint.rotation;
                            rb.isKinematic = false;
                        }
                        
                        Debug.Log($"[DungeonGenerator] Игрок перемещен в позицию: {existingPlayer.transform.position}");
                        
                        // Проверяем, что перемещение действительно произошло
                        if (Vector3.Distance(existingPlayer.transform.position, spawnPoint.position) > 0.1f)
                        {
                            Debug.LogError("[DungeonGenerator] Не удалось переместить игрока в нужную позицию!");
                            // Пробуем еще раз через один кадр
                            StartCoroutine(RetryPlayerPosition(existingPlayer, spawnPoint));
                        }
                    }
                    else
                    {
                        Debug.Log("[DungeonGenerator] Существующий игрок не найден, создаем нового");
                        // Создаем нового игрока
                        GameObject playerPrefab = Resources.Load<GameObject>("Prefabs/Player");
                        if (playerPrefab != null)
                        {
                            GameObject newPlayer = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
                            newPlayer.tag = "Player";
                            Debug.Log($"[DungeonGenerator] Создан новый игрок в позиции: {newPlayer.transform.position}");
                        }
                        else
                        {
                            Debug.LogError("[DungeonGenerator] Не удалось загрузить префаб игрока!");
                        }
                    }
                }
                else
                {
                    Debug.LogError("[DungeonGenerator] Точка спавна не найдена в стартовой комнате!");
                    // Выводим все дочерние объекты для отладки
                    Debug.Log("[DungeonGenerator] Содержимое стартовой комнаты:");
                    foreach (Transform child in startRoom.RoomInstance.transform)
                    {
                        Debug.Log($"- {child.name} (тег: {child.tag})");
                    }
                }
            }
            else
            {
                Debug.LogError("[DungeonGenerator] Стартовая комната не найдена!");
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
            Debug.Log($"[DungeonGenerator] Поиск ближайшей комнаты для {newRoom.Id}");
            
            RoomNode nearestRoom = null;
            float minDistance = float.MaxValue;

            foreach (var room in rooms)
            {
                if (room == newRoom) continue;

                float distance = Vector2Int.Distance(newRoom.Position, room.Position);
                Debug.Log($"[DungeonGenerator] Расстояние до комнаты {room.Id}: {distance}");
                
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestRoom = room;
                }
            }

            if (nearestRoom != null)
            {
                // Проверяем, что комнаты находятся на соседних позициях
                Vector2Int diff = nearestRoom.Position - newRoom.Position;
                if ((diff.x == 0 && Mathf.Abs(diff.y) == 1) || 
                    (diff.y == 0 && Mathf.Abs(diff.x) == 1))
                {
                    newRoom.AddConnection(nearestRoom);
                    nearestRoom.AddConnection(newRoom);
                    Debug.Log($"[DungeonGenerator] Комната {newRoom.Id} соединена с {nearestRoom.Id}");
                }
                else
                {
                    Debug.LogWarning($"[DungeonGenerator] Комнаты {newRoom.Id} и {nearestRoom.Id} не находятся на соседних позициях!");
                }
            }
            else
            {
                Debug.LogWarning($"[DungeonGenerator] Не найдена ближайшая комната для {newRoom.Id}");
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

        public void LoadRoom(string nextRoomId)
        {
            if (isGenerating)
            {
                Debug.LogWarning("[DungeonGenerator] Нельзя загрузить комнату во время генерации!");
                return;
            }

            // Находим комнату по ID
            RoomNode nextRoom = rooms.Find(r => r.Id == nextRoomId);
            if (nextRoom == null)
            {
                Debug.LogError($"[DungeonGenerator] Комната с ID {nextRoomId} не найдена!");
                return;
            }

            // Если комната уже создана, просто активируем её
            if (nextRoom.RoomInstance != null)
            {
                nextRoom.RoomInstance.SetActive(true);
                Debug.Log($"[DungeonGenerator] Комната {nextRoomId} активирована");
                return;
            }

            // Если комната не создана, создаём её
            Vector3 position = new Vector3(nextRoom.Position.x * dungeonConfig.roomSpacing, 0, nextRoom.Position.y * dungeonConfig.roomSpacing);
            GameObject roomInstance = Instantiate(nextRoom.RoomTemplate.roomPrefab, position, Quaternion.identity, dungeonContainer);
            nextRoom.RoomInstance = roomInstance;

            // Настраиваем содержимое комнаты
            if (nextRoom.RoomType.roomType == RoomType.Combat || nextRoom.RoomType.roomType == RoomType.Boss)
            {
                SetupEnemies(nextRoom);
            }
            else if (nextRoom.RoomType.roomType == RoomType.Reward)
            {
                SetupRewards(nextRoom);
            }

            Debug.Log($"[DungeonGenerator] Комната {nextRoomId} создана и настроена");
        }

        public string GetNextRoomId(Vector2Int currentPosition, Vector2Int direction)
        {
            Debug.Log($"[DungeonGenerator] Поиск следующей комнаты. Текущая позиция: {currentPosition}, Направление: {direction}");
            
            RoomNode currentRoom = GetRoomNodeAtPosition(new Vector3(currentPosition.x * dungeonConfig.roomSpacing, 0, currentPosition.y * dungeonConfig.roomSpacing));
            if (currentRoom == null)
            {
                Debug.LogError("[DungeonGenerator] Текущая комната не найдена!");
                return null;
            }

            Vector2Int nextPosition = currentPosition + direction;
            Debug.Log($"[DungeonGenerator] Позиция следующей комнаты: {nextPosition}");

            // Ищем комнату в списке соединенных комнат
            foreach (var connectedRoom in currentRoom.ConnectedRooms)
            {
                if (connectedRoom.Position == nextPosition)
                {
                    Debug.Log($"[DungeonGenerator] Найдена следующая комната с ID: {connectedRoom.Id}");
                    return connectedRoom.Id;
                }
            }

            Debug.LogWarning("[DungeonGenerator] Следующая комната не найдена в списке соединенных комнат!");
            return null;
        }

        private IEnumerator RetryPlayerPosition(GameObject player, Transform spawnPoint)
        {
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();

            // Находим существующего игрока или создаем нового
            GameObject existingPlayer = GameObject.FindGameObjectWithTag("Player");
            if (existingPlayer != null)
            {
                Debug.Log($"[DungeonGenerator] Найден существующий игрок: {existingPlayer.name}, позиция: {existingPlayer.transform.position}");
                
                // Отключаем физику на время перемещения
                Rigidbody rb = existingPlayer.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = true;
                }
                
                // Перемещаем игрока в точку спавна
                existingPlayer.transform.position = spawnPoint.position;
                existingPlayer.transform.rotation = spawnPoint.rotation;
                
                // Принудительно обновляем позицию
                if (rb != null)
                {
                    rb.position = spawnPoint.position;
                    rb.rotation = spawnPoint.rotation;
                    rb.isKinematic = false;
                }
                
                Debug.Log($"[DungeonGenerator] Игрок перемещен в позицию: {existingPlayer.transform.position}");
                
                // Проверяем, что перемещение действительно произошло
                if (Vector3.Distance(existingPlayer.transform.position, spawnPoint.position) > 0.1f)
                {
                    Debug.LogError("[DungeonGenerator] Не удалось переместить игрока в нужную позицию!");
                    // Пробуем еще раз через один кадр
                    StartCoroutine(RetryPlayerPosition(existingPlayer, spawnPoint));
                }
            }
            else
            {
                Debug.Log("[DungeonGenerator] Существующий игрок не найден, создаем нового");
                // Создаем нового игрока
                GameObject playerPrefab = Resources.Load<GameObject>("Prefabs/Player");
                if (playerPrefab != null)
                {
                    GameObject newPlayer = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
                    newPlayer.tag = "Player";
                    Debug.Log($"[DungeonGenerator] Создан новый игрок в позиции: {newPlayer.transform.position}");
                }
                else
                {
                    Debug.LogError("[DungeonGenerator] Не удалось загрузить префаб игрока!");
                }
            }
        }
    }
} 