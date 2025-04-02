using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using DungeonGeneration.ScriptableObjects;
using System.Collections;
using BreakTheCycle;
using DungeonGeneration.Scripts;

namespace DungeonGeneration
{
    public class DungeonGenerator : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private DungeonConfigSO dungeonConfig;
        [SerializeField] private Transform dungeonContainer;
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private float generationDelay = 0.5f;
        [SerializeField] private bool showDebugGizmos = true;
        [SerializeField] private Color debugLineColor = Color.yellow;

        private List<RoomNode> roomNodes = new List<RoomNode>();
        private Dictionary<Vector2Int, RoomManager> roomInstances = new Dictionary<Vector2Int, RoomManager>();
        private RoomNode startRoomNode;
        private GameObject playerInstance;
        private bool isGenerating = false;
        private HashSet<Vector2Int> existingPositions = new HashSet<Vector2Int>();

        public static event System.Action<GameObject> OnPlayerCreated;

        public List<RoomNode> Rooms => roomNodes;
        public RoomNode StartRoom => startRoomNode;
        public float RoomSpacing => dungeonConfig.roomSpacing;
        public DungeonConfigSO DungeonConfig => dungeonConfig;

        private void Start()
        {
            // Проверяем необходимые компоненты
            if (dungeonConfig == null || dungeonContainer == null || playerPrefab == null)
            {
                Debug.LogError("[DungeonGenerator] Отсутствуют необходимые компоненты!");
                return;
            }

            // Проверяем корректность конфигурации
            ValidateConfiguration();

            startRoomNode = null; // Явно обнуляем перед генерацией
            Debug.Log("[DungeonGenerator] Start метод: startRoomNode установлен в null");

            // Запускаем асинхронную генерацию
            StartCoroutine(GenerateDungeonAsync());
        }

        // Проверка правильности настроек конфигурации
        private void ValidateConfiguration()
        {
            Debug.Log("[DungeonGenerator] Проверка конфигурации...");
            
            // Проверяем наличие стартовой комнаты
            if (dungeonConfig.startRoomType == null)
            {
                Debug.LogError("[DungeonGenerator] startRoomType не назначен в DungeonConfig!");
            }
            else
            {
                Debug.Log($"[DungeonGenerator] startRoomType: {dungeonConfig.startRoomType.typeName}");
            }
            
            // Проверяем наличие шаблона для стартовой комнаты
            bool hasStartTemplate = false;
            
            if (dungeonConfig.roomTemplates != null)
            {
                Debug.Log($"[DungeonGenerator] Всего шаблонов комнат: {dungeonConfig.roomTemplates.Length}");
                
                foreach (var template in dungeonConfig.roomTemplates)
                {
                    if (template == null)
                    {
                        Debug.LogWarning("[DungeonGenerator] Найден пустой шаблон комнаты!");
                        continue;
                    }
                    
                    Debug.Log($"[DungeonGenerator] Шаблон: {template.templateName}, Префаб: {(template.roomPrefab ? template.roomPrefab.name : "null")}");
                    
                    if (template.templateName.Contains("Start"))
                    {
                        hasStartTemplate = true;
                        Debug.Log($"[DungeonGenerator] Найден шаблон стартовой комнаты: {template.templateName}");
                    }
                }
            }
            else
            {
                Debug.LogError("[DungeonGenerator] roomTemplates не назначены в DungeonConfig!");
            }
            
            if (!hasStartTemplate)
            {
                Debug.LogError("[DungeonGenerator] Не найден шаблон стартовой комнаты! Добавьте шаблон со словом 'Start' в имени в DungeonConfig.");
            }
        }

        private IEnumerator GenerateDungeonAsync()
        {
            // Очищаем предыдущий данж
            ClearDungeon();

            // Сразу после очистки
            Debug.Log($"[DungeonGenerator] После ClearDungeon: startRoomNode = {(startRoomNode == null ? "null" : startRoomNode.Id)}");

            // Последовательные этапы генерации
            yield return StartCoroutine(GenerateDungeonStructure());
            Debug.Log($"[DungeonGenerator] После GenerateDungeonStructure: startRoomNode = {(startRoomNode == null ? "null" : startRoomNode.Id)}");

            yield return StartCoroutine(GenerateRooms());
            Debug.Log($"[DungeonGenerator] После GenerateRooms: startRoomNode = {(startRoomNode == null ? "null" : startRoomNode.Id)}");

            yield return StartCoroutine(ConnectRooms());
            Debug.Log($"[DungeonGenerator] После ConnectRooms: startRoomNode = {(startRoomNode == null ? "null" : startRoomNode.Id)}");

            // Перед размещением игрока
            Debug.Log($"[DungeonGenerator] Перед PlacePlayer: startRoomNode = {(startRoomNode == null ? "null" : startRoomNode.Id)}");
            Debug.Log($"[DungeonGenerator] Перед PlacePlayer: Количество комнат в roomNodes = {roomNodes.Count}");
            if (startRoomNode != null)
            {
                Debug.Log($"[DungeonGenerator] Перед PlacePlayer: RoomInstance стартовой комнаты = {(startRoomNode.RoomInstance == null ? "null" : startRoomNode.RoomInstance.name)}");
                Debug.Log($"[DungeonGenerator] Перед PlacePlayer: Тип комнаты = {startRoomNode.RoomType.typeName}");
            }

            yield return StartCoroutine(PlacePlayer());
        }

        private IEnumerator GenerateDungeonStructure()
        {
            roomNodes = new List<RoomNode>();
            Debug.Log("[DungeonGenerator] Начинаем генерацию расположения комнат");

            // Проверяем, что у нас есть тип стартовой комнаты
            if (dungeonConfig.startRoomType == null)
            {
                Debug.LogError("[DungeonGenerator] Тип стартовой комнаты не назначен в DungeonConfig!");
                yield break;
            }

            Debug.Log($"[DungeonGenerator] Тип стартовой комнаты: {dungeonConfig.startRoomType.typeName}");

            // Сначала создаем стартовую комнату
            Vector2Int startPosition = Vector2Int.zero;
            var startRoom = new RoomNode(startPosition, dungeonConfig.startRoomType, "start_room");
            roomNodes.Add(startRoom);
            startRoomNode = startRoom;
            Debug.Log($"[DungeonGenerator] Создана стартовая комната типа: {dungeonConfig.startRoomType.typeName} на позиции {startPosition}");
            Debug.Log($"[DungeonGenerator] startRoomNode установлен: ID = {startRoomNode.Id}, Type = {startRoomNode.RoomType?.typeName ?? "null"}");

            // Проверяем, что стартовая комната добавлена в список
            if (!roomNodes.Contains(startRoom))
            {
                Debug.LogError("[DungeonGenerator] Стартовая комната не была добавлена в список комнат!");
                yield break;
            }

            // Проверяем, что startRoomNode правильно установлен
            if (startRoomNode == null)
            {
                Debug.LogError("[DungeonGenerator] startRoomNode не был установлен!");
                yield break;
            }

            Debug.Log($"[DungeonGenerator] startRoomNode установлен: {startRoomNode.Id} типа {startRoomNode.RoomType.typeName}");

            // Генерируем остальные комнаты
            for (int i = 1; i < dungeonConfig.maxRooms; i++)
            {
                Vector2Int position = FindValidPosition();

                // Определяем тип комнаты
                RoomTypeSO roomType = DetermineRoomType(position);
                if (roomType == null)
                {
                    Debug.LogError($"[DungeonGenerator] Не удалось определить тип комнаты для позиции {position}");
                    continue;
                }

                // Создаем новую комнату
                var room = new RoomNode(position, roomType, $"room_{i}_{position.x}_{position.y}");
                roomNodes.Add(room);
                Debug.Log($"[DungeonGenerator] Создана комната типа {roomType.typeName} на позиции {position}");
            }

            Debug.Log($"[DungeonGenerator] Всего создано комнат: {roomNodes.Count}");
            Debug.Log($"[DungeonGenerator] В конце GenerateDungeonStructure: startRoomNode = {(startRoomNode == null ? "null" : startRoomNode.Id)}");
            yield return null;
        }

        private IEnumerator GenerateRooms()
        {
            Debug.Log($"[DungeonGenerator] В начале GenerateRooms: startRoomNode = {(startRoomNode == null ? "null" : startRoomNode.Id)}");
            Debug.Log("[DungeonGenerator] Начинаем создание комнат");
            
            foreach (var room in roomNodes)
            {
                bool isStartRoom = (room == startRoomNode);
                Debug.Log($"[DungeonGenerator] Создаю комнату {room.Id}, isStartRoom = {isStartRoom}");
                
                CreateRoomInstance(room);
                
                if (isStartRoom)
                {
                    Debug.Log($"[DungeonGenerator] После создания стартовой комнаты: RoomInstance = {(room.RoomInstance == null ? "null" : room.RoomInstance.name)}");
                }
                
                yield return new WaitForSeconds(generationDelay);
            }
            
            Debug.Log($"[DungeonGenerator] В конце GenerateRooms: startRoomNode = {(startRoomNode == null ? "null" : startRoomNode.Id)}");
            Debug.Log($"[DungeonGenerator] В конце GenerateRooms: startRoomNode.RoomInstance = {(startRoomNode?.RoomInstance == null ? "null" : startRoomNode.RoomInstance.name)}");
        }

        private IEnumerator ConnectRooms()
        {
            Debug.Log("[DungeonGenerator] Начинаем соединение комнат");
                yield return null;
            }

        private IEnumerator PlacePlayer()
        {
            Debug.Log($"[DungeonGenerator] PlacePlayer: Начало метода. Существование startRoomNode: {startRoomNode != null}");
            
            // Анализируем состояние roomNodes
            Debug.Log($"[DungeonGenerator] PlacePlayer: roomNodes = {(roomNodes == null ? "null" : "не null")}, количество комнат: {roomNodes?.Count ?? 0}");
            
            if (roomNodes != null && roomNodes.Count > 0)
            {
                Debug.Log("[DungeonGenerator] PlacePlayer: Список всех комнат:");
                foreach (var node in roomNodes)
                {
                    Debug.Log($"  - Комната: ID = {node.Id}, Type = {node.RoomType?.typeName ?? "null"}, Instance = {(node.RoomInstance == null ? "null" : node.RoomInstance.name)}");
                }
            }
            
            // Ждем, пока все комнаты будут созданы
            while (roomNodes == null || roomNodes.Count == 0)
            {
                Debug.Log("[DungeonGenerator] Ожидание создания комнат...");
                yield return new WaitForSeconds(0.1f);
            }

            Debug.Log($"[DungeonGenerator] PlacePlayer: После ожидания. startRoomNode = {(startRoomNode == null ? "null" : startRoomNode.Id)}");
            
            // Перестраховка: если startRoomNode не установлен, попробуем найти его снова
            if (startRoomNode == null)
            {
                Debug.Log("[DungeonGenerator] startRoomNode не установлен, пытаемся найти стартовую комнату в списке roomNodes");
                startRoomNode = roomNodes.FirstOrDefault(r => r.RoomType != null && r.RoomType.typeName.Contains("Start"));
            }

            // Используем уже найденную стартовую комнату
            if (startRoomNode == null)
            {
                Debug.LogError("[DungeonGenerator] Стартовая комната не найдена! Проверьте настройки DungeonConfig.");
                yield break;
            }

            Debug.Log($"[DungeonGenerator] Найдена стартовая комната типа: {startRoomNode.RoomType.typeName}");

            // Получаем RoomManager стартовой комнаты
            if (startRoomNode.RoomInstance == null)
            {
                Debug.LogError("[DungeonGenerator] RoomInstance стартовой комнаты не создан!");
                yield break;
            }
            
            var startRoomManager = startRoomNode.RoomInstance.GetComponent<RoomManager>();
            if (startRoomManager == null)
            {
                Debug.LogError("[DungeonGenerator] RoomManager не найден в стартовой комнате!");
                yield break;
            }

            // Получаем точку спавна
            Transform spawnPoint = startRoomManager.PlayerSpawnPoint;
            if (spawnPoint == null)
            {
                Debug.LogError("[DungeonGenerator] Точка спавна не назначена в стартовой комнате!");
                yield break;
            }

            Debug.Log($"[DungeonGenerator] Точка спавна найдена на позиции: {spawnPoint.position}");

            // Убеждаемся, что стартовая комната активна
            if (!startRoomNode.RoomInstance.activeSelf)
            {
                Debug.Log("[DungeonGenerator] Активация стартовой комнаты");
                startRoomNode.RoomInstance.SetActive(true);
            }

            // Создаем игрока
            playerInstance = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
            playerInstance.tag = "Player";
            Debug.Log($"[DungeonGenerator] Создан новый игрок в стартовой комнате типа: {startRoomNode.RoomType.typeName} на позиции {spawnPoint.position}");

            // Уведомляем о создании игрока
            OnPlayerCreated?.Invoke(playerInstance);

            yield return null;
        }

        private GameObject GetRoomInstance(RoomNode room)
        {
            if (room == null) return null;

            // Ищем комнату по позиции
            var roomInstance = dungeonContainer.GetComponentsInChildren<RoomManager>()
                .FirstOrDefault(r => r.transform.position == new Vector3(room.Position.x * dungeonConfig.roomSpacing, 0, room.Position.y * dungeonConfig.roomSpacing));

            return roomInstance?.gameObject;
        }

        private void ConnectRooms(RoomNode room1, RoomNode room2)
        {
            if (room1 == null || room2 == null) return;

            room1.AddConnection(room2);
            room2.AddConnection(room1);
            Debug.Log($"[DungeonGenerator] Соединены комнаты: {room1.Id} и {room2.Id}");
        }

        private void CreateRoomInstance(RoomNode room)
        {
            bool isStartRoom = (room == startRoomNode);
            Debug.Log($"[DungeonGenerator] CreateRoomInstance: создаю комнату {room.Id}, isStartRoom = {isStartRoom}, тип комнаты = {room.RoomType.typeName}");
            
            if (room?.RoomType == null)
            {
                Debug.LogError("[DungeonGenerator] Room or RoomType is null!");
                return;
            }

            // Получаем шаблон комнаты
            RoomTemplateSO template = GetRandomCompatibleTemplate(room.RoomType);
            if (template?.roomPrefab == null)
            {
                Debug.LogError($"[DungeonGenerator] No compatible template found for room type {room.RoomType.typeName}!");
                return;
            }

            // Проверяем, что для стартовой комнаты выбран правильный шаблон
            if (isStartRoom && !template.templateName.Contains("Start"))
            {
                Debug.LogError($"[DungeonGenerator] Для стартовой комнаты выбран неправильный шаблон: {template.templateName}! Ищем правильный шаблон.");
                
                // Пытаемся найти шаблон стартовой комнаты принудительно
                var startTemplate = dungeonConfig.roomTemplates.FirstOrDefault(t => t != null && t.templateName.Contains("Start"));
                if (startTemplate != null)
                {
                    Debug.Log($"[DungeonGenerator] Найден шаблон стартовой комнаты: {startTemplate.templateName}");
                    template = startTemplate;
                }
                else
                {
                    Debug.LogError("[DungeonGenerator] Не найден ни один шаблон стартовой комнаты! Проверьте конфигурацию DungeonConfig.");
                }
            }

            Debug.Log($"[DungeonGenerator] Выбран шаблон: {template.templateName} для комнаты типа {room.RoomType.typeName}");

            // Создаем комнату
            Vector3 position = new Vector3(room.Position.x * dungeonConfig.roomSpacing, 0, room.Position.y * dungeonConfig.roomSpacing);
            GameObject roomInstance = Instantiate(template.roomPrefab, position, Quaternion.identity, dungeonContainer);
            room.SetRoomInstance(roomInstance);
            
            // Проверяем, если это стартовая комната
            if (isStartRoom)
            {
                Debug.Log($"[DungeonGenerator] CreateRoomInstance: Стартовая комната создана: {roomInstance.name} на позиции {position}");
            }

            // Настраиваем комнату
            var roomManager = roomInstance.GetComponent<RoomManager>();
            if (roomManager != null)
            {
                roomManager.InitializeRoom();
                roomManager.SetRoomType(room.RoomType);
                
                // Если это стартовая комната, делаем её активной, остальные деактивируем
                if (isStartRoom)
                {
                    roomInstance.SetActive(true);
                    Debug.Log($"[DungeonGenerator] Активирована стартовая комната: {room.RoomType.typeName}");
                    
                    // Дополнительная проверка
                    if (startRoomNode == null || startRoomNode.RoomInstance == null)
                    {
                        Debug.LogError("[DungeonGenerator] startRoomNode или startRoomNode.RoomInstance == null после создания стартовой комнаты!");
                    }
                }
                else
                {
                    roomInstance.SetActive(false);
                    Debug.Log($"[DungeonGenerator] Создана и деактивирована комната: {room.RoomType.typeName}");
                }
            }
            else
            {
                Debug.LogError($"[DungeonGenerator] RoomManager component not found on room instance {roomInstance.name}!");
            }
        }

        private RoomTypeSO DetermineRoomType(Vector2Int position)
        {
            // Если это первая комната (стартовая)
            if (position == Vector2Int.zero)
            {
                Debug.Log("[DungeonGenerator] Определен тип комнаты: Start");
                return dungeonConfig.startRoomType;
            }

            // Если это последняя комната (босс)
            if (position == new Vector2Int(dungeonConfig.maxRooms - 1, dungeonConfig.maxRooms - 1))
            {
                Debug.Log("[DungeonGenerator] Определен тип комнаты: Boss");
                return dungeonConfig.bossRoomType;
            }

            // Для остальных комнат используем случайный выбор между базовой и элитной боевой комнатой
            float randomValue = Random.value;
            if (randomValue < dungeonConfig.basicCombatRoomWeight)
            {
                Debug.Log("[DungeonGenerator] Определен тип комнаты: Basic Combat");
                return dungeonConfig.basicCombatRoomType;
            }
            else
            {
                Debug.Log("[DungeonGenerator] Определен тип комнаты: Elite Combat");
                return dungeonConfig.eliteCombatRoomType;
            }
        }

        private Vector2Int FindValidPosition()
        {
            int maxAttempts = 100;
            int attempts = 0;

            while (attempts < maxAttempts)
            {
                int x = UnityEngine.Random.Range(-dungeonConfig.maxRooms, dungeonConfig.maxRooms + 1);
                int y = UnityEngine.Random.Range(-dungeonConfig.maxRooms, dungeonConfig.maxRooms + 1);
                Vector2Int position = new Vector2Int(x, y);

                // Проверяем, не занята ли позиция
                bool positionOccupied = roomNodes.Any(r => r.Position == new Vector2(position.x, position.y));
                if (!positionOccupied)
                {
                    return position;
                }

                attempts++;
            }

            Debug.LogWarning("[DungeonGenerator] Не удалось найти свободную позицию после " + maxAttempts + " попыток");
            return Vector2Int.zero;
        }

        private void EnsureBossRoomAccess()
        {
            // Находим боссовую комнату
            RoomNode bossRoom = null;
            foreach (var room in roomNodes)
            {
                if (room.RoomType != null && room.RoomType.canBeLast)
                {
                    bossRoom = room;
                    break;
                }
            }

            if (bossRoom == null)
            {
                Debug.LogWarning("Боссовая комната не найдена!");
                return;
            }

            // Находим ближайшую комнату к боссовой
            RoomNode nearestRoom = null;
            float minDistance = float.MaxValue;

            foreach (var room in roomNodes)
            {
                if (room == bossRoom) continue;
                
                float distance = Vector2.Distance(bossRoom.Position, room.Position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestRoom = room;
                }
            }

            if (nearestRoom != null)
            {
                // Проверяем, что комнаты находятся на соседних позициях
                Vector2 diff = nearestRoom.Position - bossRoom.Position;
                if ((diff.x == 0 && Mathf.Abs(diff.y) == 1) || 
                    (diff.y == 0 && Mathf.Abs(diff.x) == 1))
                {
                    ConnectRooms(bossRoom, nearestRoom);
                }
                else
                {
                    Debug.LogWarning($"Боссовая комната и ближайшая комната не находятся на соседних позициях!");
                }
            }
            else
            {
                Debug.LogWarning("Не найдена ближайшая комната к боссовой!");
            }
        }

        private void ClearDungeon()
        {
            // Сохраняем текущее значение перед очисткой
            RoomNode oldStartRoomNode = startRoomNode;
            string oldStartId = oldStartRoomNode?.Id ?? "null";
            
            Debug.Log($"[DungeonGenerator] ClearDungeon: Очистка подангона. Старый startRoomNode = {oldStartId}");

            // Уничтожаем все существующие комнаты
            if (dungeonContainer != null)
            {
                while (dungeonContainer.childCount > 0)
                {
                    DestroyImmediate(dungeonContainer.GetChild(0).gameObject);
                }
            }

            // Очищаем список комнат
            roomNodes?.Clear();
            existingPositions?.Clear();
            
            // Обнуляем ссылки
            startRoomNode = null;
            playerInstance = null;

            Debug.Log("[DungeonGenerator] ClearDungeon: Даньж очищен, startRoomNode = null");
        }

        public void OnRoomCleared(RoomNode room)
        {
            room.ClearRoom();
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
            return roomNodes.FirstOrDefault(r => r.Position == gridPosition);
        }

        public void LoadRoom(string nextRoomId)
        {
            if (isGenerating)
            {
                Debug.LogWarning("[DungeonGenerator] Нельзя загрузить комнату во время генерации!");
                return;
            }

            Debug.Log($"[DungeonGenerator] LoadRoom: Начало загрузки комнаты {nextRoomId}");

            // Находим комнату по ID
            RoomNode nextRoom = roomNodes.Find(r => r.Id == nextRoomId);
            if (nextRoom == null)
            {
                Debug.LogError($"[DungeonGenerator] Комната с ID {nextRoomId} не найдена!");
                return;
            }

            Debug.Log($"[DungeonGenerator] LoadRoom: Найдена комната с ID {nextRoomId}, типа {nextRoom.RoomType.typeName}");

            // Находим текущую комнату игрока
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                Debug.LogError("[DungeonGenerator] Игрок не найден!");
                return;
            }

            Debug.Log($"[DungeonGenerator] LoadRoom: Найден игрок {player.name} на позиции {player.transform.position}");

            // Находим текущую комнату по позиции игрока
            var currentRoom = GetRoomNodeAtPosition(player.transform.position);
            if (currentRoom == null)
            {
                Debug.LogError("[DungeonGenerator] Текущая комната не найдена!");
                Debug.Log($"[DungeonGenerator] Позиция игрока: {player.transform.position}, преобразованная позиция: {Mathf.RoundToInt(player.transform.position.x / dungeonConfig.roomSpacing)}, {Mathf.RoundToInt(player.transform.position.z / dungeonConfig.roomSpacing)}");
                // Пробуем использовать ближайшую комнату вместо текущей
                float minDistance = float.MaxValue;
                RoomNode closestRoom = null;
                
                foreach (var room in roomNodes)
                {
                    if (room.RoomInstance != null)
                    {
                        float distance = Vector3.Distance(player.transform.position, room.RoomInstance.transform.position);
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            closestRoom = room;
                        }
                    }
                }
                
                if (closestRoom != null)
                {
                    Debug.Log($"[DungeonGenerator] Используем ближайшую комнату: {closestRoom.Id} на расстоянии {minDistance}");
                    currentRoom = closestRoom;
                }
                else
                {
                    Debug.LogError("[DungeonGenerator] Не удалось найти ни одной ближайшей комнаты!");
                    return;
                }
            }

            Debug.Log($"[DungeonGenerator] LoadRoom: Текущая комната игрока: {currentRoom.Id}");

            // Если комната уже создана, просто активируем её
            if (nextRoom.RoomInstance != null)
            {
                Debug.Log($"[DungeonGenerator] LoadRoom: Комната {nextRoomId} уже создана, активируем её");
                nextRoom.RoomInstance.SetActive(true);
                var existingRoomManager = nextRoom.RoomInstance.GetComponent<RoomManager>();
                if (existingRoomManager != null)
                {
                    Transform spawnPoint = nextRoom.RoomInstance.transform.Find("PlayerSpawnPoint");
                    if (spawnPoint != null)
                    {
                        Debug.Log($"[DungeonGenerator] LoadRoom: Найдена точка спавна в комнате {nextRoomId} на позиции {spawnPoint.position}");
                        // Перемещаем игрока
                        MovePlayerToSpawnPoint(player, spawnPoint);
                        // После успешного перемещения удаляем предыдущую комнату
                        DestroyRoom(currentRoom.Id);
                    }
                    else
                    {
                        Debug.LogError($"[DungeonGenerator] LoadRoom: Точка спавна не найдена в комнате {nextRoomId}!");
                        // Ищем PlayerSpawnPoint через RoomManager
                        if (existingRoomManager.PlayerSpawnPoint != null)
                        {
                            Debug.Log($"[DungeonGenerator] LoadRoom: Найдена точка спавна через RoomManager на позиции {existingRoomManager.PlayerSpawnPoint.position}");
                            MovePlayerToSpawnPoint(player, existingRoomManager.PlayerSpawnPoint);
                            DestroyRoom(currentRoom.Id);
                        }
                        else
                        {
                            Debug.LogError("[DungeonGenerator] LoadRoom: Точка спавна не найдена даже через RoomManager!");
                        }
                    }
                }
                else
                {
                    Debug.LogError($"[DungeonGenerator] LoadRoom: RoomManager не найден в комнате {nextRoomId}!");
                }
                Debug.Log($"[DungeonGenerator] Комната {nextRoomId} активирована");
                return;
            }

            Debug.Log($"[DungeonGenerator] LoadRoom: Комната {nextRoomId} не создана, создаём её");

            // Если комната не создана, создаём её
            Vector3 position = new Vector3(nextRoom.Position.x * dungeonConfig.roomSpacing, 0, nextRoom.Position.y * dungeonConfig.roomSpacing);
            
            // Получаем шаблон комнаты
            RoomTemplateSO template = GetRandomCompatibleTemplate(nextRoom.RoomType);
            if (template == null || template.roomPrefab == null)
            {
                Debug.LogError($"[DungeonGenerator] Не найден совместимый шаблон для комнаты типа {nextRoom.RoomType.typeName}");
                return;
            }

            Debug.Log($"[DungeonGenerator] LoadRoom: Выбран шаблон {template.templateName} для комнаты {nextRoomId}");
            
            GameObject roomInstance = Instantiate(template.roomPrefab, position, Quaternion.identity, dungeonContainer);
            nextRoom.SetRoomInstance(roomInstance);
            Debug.Log($"[DungeonGenerator] LoadRoom: Создан экземпляр комнаты {roomInstance.name} на позиции {position}");

            // Настраиваем содержимое комнаты через RoomManager
            var newRoomManager = roomInstance.GetComponent<RoomManager>();
            if (newRoomManager != null)
            {
                Debug.Log($"[DungeonGenerator] LoadRoom: Инициализация комнаты через RoomManager");
                newRoomManager.InitializeRoom();
                Transform spawnPoint = roomInstance.transform.Find("PlayerSpawnPoint");
                if (spawnPoint != null)
                {
                    Debug.Log($"[DungeonGenerator] LoadRoom: Найдена точка спавна на позиции {spawnPoint.position}");
                    // Перемещаем игрока в новую комнату
                    MovePlayerToSpawnPoint(player, spawnPoint);
                    // После успешного перемещения удаляем предыдущую комнату
                    DestroyRoom(currentRoom.Id);
                }
                else if (newRoomManager.PlayerSpawnPoint != null)
                {
                    Debug.Log($"[DungeonGenerator] LoadRoom: Найдена точка спавна через RoomManager на позиции {newRoomManager.PlayerSpawnPoint.position}");
                    MovePlayerToSpawnPoint(player, newRoomManager.PlayerSpawnPoint);
                    DestroyRoom(currentRoom.Id);
                }
                else
                {
                    Debug.LogError("[DungeonGenerator] LoadRoom: Точка спавна не найдена!");
                }
            }
            else
            {
                Debug.LogError($"[DungeonGenerator] RoomManager не найден в комнате {nextRoomId}");
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

        private void MovePlayerToSpawnPoint(GameObject player, Transform spawnPoint)
        {
            if (player == null)
            {
                Debug.LogError("[DungeonGenerator] MovePlayerToSpawnPoint: Игрок не найден!");
                return;
            }

            if (spawnPoint == null)
            {
                Debug.LogError("[DungeonGenerator] MovePlayerToSpawnPoint: Точка спавна не найдена!");
                return;
            }

            Debug.Log($"[DungeonGenerator] MovePlayerToSpawnPoint: Перемещение игрока {player.name} из позиции: {player.transform.position} в точку спавна: {spawnPoint.position}");
            
            // Отключаем физику на время перемещения
            Rigidbody rb = player.GetComponent<Rigidbody>();
            bool hadRigidbody = rb != null;
            bool wasKinematic = false;
            
            if (rb != null)
            {
                wasKinematic = rb.isKinematic;
                rb.isKinematic = true;
                Debug.Log("[DungeonGenerator] MovePlayerToSpawnPoint: Физика игрока временно отключена");
            }
            
            // Запоминаем текущий коллайдер
            Collider playerCollider = player.GetComponent<Collider>();
            bool colliderWasEnabled = false;
            
            if (playerCollider != null)
            {
                colliderWasEnabled = playerCollider.enabled;
                playerCollider.enabled = false;
                Debug.Log("[DungeonGenerator] MovePlayerToSpawnPoint: Коллайдер игрока временно отключен");
            }
            
            // Перемещаем игрока в точку спавна
            player.transform.position = spawnPoint.position;
            player.transform.rotation = spawnPoint.rotation;
            
            // Принудительно обновляем позицию Rigidbody
            if (rb != null)
            {
                rb.position = spawnPoint.position;
                rb.rotation = spawnPoint.rotation;
                rb.linearVelocity = Vector3.zero; // Сбрасываем скорость
                rb.angularVelocity = Vector3.zero; // Сбрасываем вращение
                
                // Возвращаем исходное состояние Rigidbody
                rb.isKinematic = wasKinematic;
                Debug.Log("[DungeonGenerator] MovePlayerToSpawnPoint: Физика игрока восстановлена");
            }
            
            // Восстанавливаем коллайдер
            if (playerCollider != null)
            {
                playerCollider.enabled = colliderWasEnabled;
                Debug.Log("[DungeonGenerator] MovePlayerToSpawnPoint: Коллайдер игрока восстановлен");
            }
            
            Debug.Log($"[DungeonGenerator] MovePlayerToSpawnPoint: Игрок успешно перемещен в позицию: {player.transform.position}");
            
            // Проверяем, что перемещение действительно произошло
            if (Vector3.Distance(player.transform.position, spawnPoint.position) > 0.1f)
            {
                Debug.LogError($"[DungeonGenerator] MovePlayerToSpawnPoint: Не удалось переместить игрока в нужную позицию! Текущая позиция: {player.transform.position}, целевая позиция: {spawnPoint.position}, разница: {Vector3.Distance(player.transform.position, spawnPoint.position)}");
                // Пробуем еще раз через один кадр
                StartCoroutine(RetryPlayerPosition(player, spawnPoint));
            }
        }

        private IEnumerator RetryPlayerPosition(GameObject player, Transform spawnPoint)
        {
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();

            // Находим существующего игрока или создаем нового
            GameObject existingPlayer = GameObject.FindGameObjectWithTag("Player");
            if (existingPlayer != null)
            {
                MovePlayerToSpawnPoint(existingPlayer, spawnPoint);
            }
            else
            {
                Debug.Log("[DungeonGenerator] Существующий игрок не найден, создаем нового");
                // Создаем нового игрока
                    GameObject newPlayer = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
                    newPlayer.tag = "Player";
                    Debug.Log($"[DungeonGenerator] Создан новый игрок в позиции: {newPlayer.transform.position}");
                }
        }

        private Vector3 FindPlayerSpawnPoint(GameObject room)
        {
            var roomManager = room.GetComponent<RoomManager>();
            if (roomManager == null)
            {
                Debug.LogError($"RoomManager not found in room {room.name}!");
                return room.transform.position;
            }

            return roomManager.GetPlayerSpawnPosition();
        }

        private void SpawnPlayer(Vector3 position)
        {
            if (playerPrefab == null)
            {
                Debug.LogError("Player prefab not set in DungeonGenerator!");
                return;
            }

            GameObject player = Instantiate(playerPrefab, position, Quaternion.identity);
            player.tag = "Player"; // Убеждаемся, что у игрока правильный тег
        }

        private RoomTemplateSO GetRandomCompatibleTemplate(RoomTypeSO roomType)
        {
            if (roomType == null)
            {
                Debug.LogError("[DungeonGenerator] RoomType is null!");
                return null;
            }

            // Находим шаблоны, соответствующие типу комнаты
            var compatibleTemplates = dungeonConfig.roomTemplates
                .Where(template => template != null && TemplateMatchesRoomType(template, roomType))
                .ToList();

            if (compatibleTemplates.Count == 0)
            {
                Debug.LogError($"[DungeonGenerator] Не найдены совместимые шаблоны для типа комнаты {roomType.typeName}!");
                
                // Если для конкретного типа комнаты нет шаблона, используем любой доступный шаблон
                compatibleTemplates = dungeonConfig.roomTemplates.Where(template => template != null).ToList();
                if (compatibleTemplates.Count == 0)
                {
                    return null;
                }
                Debug.LogWarning($"[DungeonGenerator] Используем случайный шаблон в качестве запасного варианта для типа {roomType.typeName}");
            }

            // Выбираем случайный шаблон из совместимых
            return compatibleTemplates[UnityEngine.Random.Range(0, compatibleTemplates.Count)];
        }

        // Проверяет, соответствует ли шаблон типу комнаты
        private bool TemplateMatchesRoomType(RoomTemplateSO template, RoomTypeSO roomType)
        {
            // Здесь логика сопоставления шаблона и типа комнаты
            // Например, сравниваем имена или используем специальное поле
            
            // Для стартовой комнаты
            if (roomType == dungeonConfig.startRoomType && template.templateName.Contains("Start"))
            {
                Debug.Log($"[DungeonGenerator] Найден шаблон стартовой комнаты: {template.templateName}");
                return true;
            }
            
            // Для комнаты босса
            if (roomType == dungeonConfig.bossRoomType && template.templateName.Contains("Boss"))
            {
                return true;
            }
            
            // Для базовой боевой комнаты
            if (roomType == dungeonConfig.basicCombatRoomType && template.templateName.Contains("Basic"))
            {
                return true;
            }
            
            // Для элитной боевой комнаты
            if (roomType == dungeonConfig.eliteCombatRoomType && template.templateName.Contains("Elite"))
            {
                return true;
            }
            
            // Если имя шаблона содержит типовое имя комнаты
            if (template.templateName.Contains(roomType.typeName))
            {
                return true;
            }
            
            return false;
        }

        public void DestroyRoom(string roomId)
        {
            var room = roomNodes.FirstOrDefault(r => r.Id == roomId);
            if (room != null)
            {
                room.DestroyRoom();
                roomNodes.Remove(room);
                Debug.Log($"[DungeonGenerator] Комната {roomId} уничтожена");
            }
        }

        public List<RoomNode> GetAvailableRooms()
        {
            return roomNodes.Where(r => 
                r.RoomType != dungeonConfig.bossRoomType && 
                r.RoomType != dungeonConfig.startRoomType && 
                !r.IsCleared).ToList();
        }

        public RoomNode GetBossRoom()
        {
            return roomNodes.FirstOrDefault(r => r.RoomType == dungeonConfig.bossRoomType);
        }
    }
} 