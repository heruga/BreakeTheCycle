using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using DungeonGeneration.ScriptableObjects;
using System.Collections;
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
            isGenerating = true;
            Debug.Log("[DungeonGenerator] Начало генерации подземелья");

            // Очищаем старые данные
            ClearDungeon();

            // Создаем стартовую комнату
            yield return StartCoroutine(CreateStartRoom());
            Debug.Log($"[DungeonGenerator] После CreateStartRoom: startRoomNode = {(startRoomNode == null ? "null" : startRoomNode.Id)}");

            // Создаем остальные комнаты
            yield return StartCoroutine(CreateRooms());
            Debug.Log($"[DungeonGenerator] После CreateRooms: startRoomNode = {(startRoomNode == null ? "null" : startRoomNode.Id)}");

            // Создаем босс-комнату
            yield return StartCoroutine(CreateBossRoom());
            Debug.Log($"[DungeonGenerator] После CreateBossRoom: startRoomNode = {(startRoomNode == null ? "null" : startRoomNode.Id)}");

            // Размещаем игрока
            yield return StartCoroutine(PlacePlayer());
            Debug.Log($"[DungeonGenerator] После PlacePlayer: startRoomNode = {(startRoomNode == null ? "null" : startRoomNode.Id)}");

            isGenerating = false;
            Debug.Log("[DungeonGenerator] Генерация подземелья завершена");
        }

        private IEnumerator CreateStartRoom()
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

        private IEnumerator CreateRooms()
        {
            Debug.Log($"[DungeonGenerator] В начале GenerateRooms: startRoomNode = {(startRoomNode == null ? "null" : startRoomNode.Id)}");
            Debug.Log("[DungeonGenerator] Начинаем создание комнат");
            
            // Создаем только стартовую комнату
            if (startRoomNode != null)
            {
                Debug.Log($"[DungeonGenerator] Создаю стартовую комнату {startRoomNode.Id}");
                CreateRoomInstance(startRoomNode);
                yield return new WaitForSeconds(generationDelay);
                            }
                            else
                            {
                Debug.LogError("[DungeonGenerator] startRoomNode is null в CreateRooms!");
            }
            
            Debug.Log($"[DungeonGenerator] В конце GenerateRooms: startRoomNode = {(startRoomNode == null ? "null" : startRoomNode.Id)}");
            Debug.Log($"[DungeonGenerator] В конце GenerateRooms: startRoomNode.RoomInstance = {(startRoomNode?.RoomInstance == null ? "null" : startRoomNode.RoomInstance.name)}");
        }

        private IEnumerator CreateBossRoom()
        {
            // Находим самую дальнюю комнату от стартовой
            RoomNode bossRoom = null;
            float maxDistance = float.MinValue;

            foreach (var room in roomNodes)
            {
                if (room == startRoomNode) continue;

                float distance = Vector2.Distance(startRoomNode.Position, room.Position);
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    bossRoom = room;
                }
            }

            if (bossRoom != null)
            {
                // Устанавливаем тип комнаты как босс-комната
                bossRoom.SetRoomType(dungeonConfig.bossRoomType);
                Debug.Log($"[DungeonGenerator] Создана босс-комната на позиции {bossRoom.Position}");
                            }
                            else
                            {
                Debug.LogError("[DungeonGenerator] Не удалось создать босс-комнату!");
            }

            yield return null;
        }

        private void PlaceBossRoom()
        {
            // Находим самую дальнюю комнату от стартовой
            RoomNode bossRoom = null;
            float maxDistance = float.MinValue;

            foreach (var room in roomNodes)
            {
                if (room == startRoomNode) continue;

                float distance = Vector2.Distance(startRoomNode.Position, room.Position);
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    bossRoom = room;
                }
            }

            if (bossRoom != null)
            {
                bossRoom.SetRoomType(dungeonConfig.bossRoomType);
                Debug.Log($"[DungeonGenerator] Размещена босс-комната на позиции {bossRoom.Position}");
            }
                            else
                            {
                Debug.LogError("[DungeonGenerator] Не удалось разместить босс-комнату!");
            }
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

            // Ищем существующего игрока
                    GameObject existingPlayer = GameObject.FindGameObjectWithTag("Player");
            
                    if (existingPlayer != null)
                    {
                // Если игрок существует, перемещаем его на точку спавна
                Debug.Log($"[DungeonGenerator] Найден существующий игрок. Перемещаем его в точку спавна.");
                MovePlayerToSpawnPoint(existingPlayer, spawnPoint);
                playerInstance = existingPlayer;
                playerInstance.SetActive(true);
            }
            else
            {
                // Если игрок не существует, создаем нового
                Debug.Log($"[DungeonGenerator] Существующий игрок не найден. Создаем нового игрока.");
                playerInstance = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
                playerInstance.tag = "Player";
                playerInstance.SetActive(false);
                MovePlayerToSpawnPoint(playerInstance, spawnPoint);
                playerInstance.SetActive(true);
            }
            
            Debug.Log($"[DungeonGenerator] Игрок находится в стартовой комнате типа: {startRoomNode.RoomType.typeName} на позиции {playerInstance.transform.position}");

            // Уведомляем о создании/перемещении игрока
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

        private void CreateRoomInstance(RoomNode room)
        {
            if (room == null)
            {
                Debug.LogError("[DungeonGenerator] CreateRoomInstance: room is null!");
                return;
            }

            // Проверяем, не существует ли уже инстанс
            if (room.RoomInstance != null)
            {
                Debug.LogWarning($"[DungeonGenerator] CreateRoomInstance: Попытка создать уже существующую комнату {room.Id}!");
                return;
            }

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

            // Создаем комнату
            Vector3 position = new Vector3(room.Position.x * dungeonConfig.roomSpacing, 0, room.Position.y * dungeonConfig.roomSpacing);
            GameObject roomInstance = Instantiate(template.roomPrefab, position, Quaternion.identity, dungeonContainer);
            
            // Устанавливаем имя для удобства отладки
            roomInstance.name = $"Room_{room.Id}";
            
            room.SetRoomInstance(roomInstance);
            Debug.Log($"[DungeonGenerator] CreateRoomInstance: Создан инстанс комнаты {room.Id} на позиции {position}");

            // Настраиваем комнату
            var roomManager = roomInstance.GetComponent<RoomManager>();
            if (roomManager != null)
            {
                roomManager.InitializeRoom();
                roomManager.SetRoomType(room.RoomType);
                
                roomInstance.SetActive(isStartRoom);
                Debug.Log($"[DungeonGenerator] Комната {room.Id} {(isStartRoom ? "активирована" : "деактивирована")}");
                        }
                        else
                        {
                Debug.LogError($"[DungeonGenerator] RoomManager не найден на префабе комнаты {template.templateName}!");
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
            Debug.Log($"[DungeonGenerator] GetRoomNodeAtPosition: Получена позиция {worldPosition}");
            
            if (roomNodes == null || roomNodes.Count == 0)
            {
                Debug.LogError("[DungeonGenerator] GetRoomNodeAtPosition: roomNodes пуст или null!");
                return null;
            }

            // Преобразуем мировые координаты в координаты сетки
            Vector2Int gridPosition = new Vector2Int(
                Mathf.RoundToInt(worldPosition.x / dungeonConfig.roomSpacing),
                Mathf.RoundToInt(worldPosition.z / dungeonConfig.roomSpacing)
            );
            
            Debug.Log($"[DungeonGenerator] GetRoomNodeAtPosition: Преобразовано в координаты сетки {gridPosition}, roomSpacing = {dungeonConfig.roomSpacing}");
            
            // Логируем все комнаты и их позиции
            Debug.Log($"[DungeonGenerator] GetRoomNodeAtPosition: Всего комнат: {roomNodes.Count}");
            
            // Ищем комнату с точным соответствием координат
            var foundRoom = roomNodes.FirstOrDefault(r => r.Position == gridPosition);
            
            // Если точного соответствия нет, ищем ближайшую комнату в радиусе 1 клетки
            if (foundRoom == null)
            {
                Debug.LogWarning($"[DungeonGenerator] GetRoomNodeAtPosition: Не найдена комната в точной позиции {gridPosition}, ищем в ближайших ячейках");
                
                float minDistance = float.MaxValue;
                foreach (var room in roomNodes)
                {
                    float distance = Vector2.Distance(gridPosition, room.Position);
                    if (distance < minDistance && distance <= 1.0f) // Проверяем расстояние до 1 (включая диагонали)
                    {
                        minDistance = distance;
                        foundRoom = room;
                    }
                }
                
                if (foundRoom != null)
                {
                    Debug.LogWarning($"[DungeonGenerator] GetRoomNodeAtPosition: Найдена ближайшая комната {foundRoom.Id} в позиции {foundRoom.Position}, расстояние = {minDistance}");
                }
            }
            else
            {
                Debug.Log($"[DungeonGenerator] GetRoomNodeAtPosition: Найдена комната {foundRoom.Id} в позиции {gridPosition}");
            }
            
            if (foundRoom == null)
            {
                Debug.LogError($"[DungeonGenerator] GetRoomNodeAtPosition: Не найдена комната в позиции {gridPosition} или рядом с ней");
            }
            
            return foundRoom;
        }

        private IEnumerator MovePlayerAndDestroyRoom(GameObject player, Transform spawnPoint, RoomNode currentRoom, string nextRoomId)
        {
            if (player == null || spawnPoint == null)
            {
                Debug.LogError("[DungeonGenerator] MovePlayerAndDestroyRoom: Неверные параметры (null)!");
                yield break;
            }
            
            // Перемещаем игрока
            MovePlayerToSpawnPoint(player, spawnPoint);
            
            // Даем физике время обработать перемещение
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            yield return new WaitForEndOfFrame();
            
            // Проверяем, что игрок действительно переместился
            float distance = Vector3.Distance(player.transform.position, spawnPoint.position);
            Debug.Log($"[DungeonGenerator] MovePlayerAndDestroyRoom: Расстояние между игроком и точкой спавна: {distance}");
            
            if (distance <= 0.5f) // Увеличиваем допустимое расстояние
            {
                Debug.Log("[DungeonGenerator] Игрок успешно перемещен, начинаем процесс уничтожения старой комнаты");
                
                // Только после успешного перемещения удаляем старую комнату
                if (currentRoom != null && currentRoom.Id != nextRoomId)
                {
                    DestroyRoom(currentRoom.Id);
                }
            }
            else
            {
                Debug.LogError($"[DungeonGenerator] Не удалось переместить игрока! Текущая позиция: {player.transform.position}, целевая позиция: {spawnPoint.position}, расстояние: {distance}");
                
                // Пробуем еще раз принудительно
                Debug.Log($"[DungeonGenerator] Повторная попытка переместить игрока в позицию {spawnPoint.position}");
                player.transform.position = spawnPoint.position;
                
                if (player.TryGetComponent<Rigidbody>(out var rb))
                {
                    rb.position = spawnPoint.position;
                    rb.linearVelocity = Vector3.zero;
                }
                
                // Проверяем еще раз
                yield return new WaitForFixedUpdate();
                yield return new WaitForEndOfFrame();
                
                distance = Vector3.Distance(player.transform.position, spawnPoint.position);
                if (distance <= 0.5f)
                {
                    Debug.Log("[DungeonGenerator] Игрок успешно перемещен после повторной попытки, уничтожаем старую комнату");
                    
                    if (currentRoom != null && currentRoom.Id != nextRoomId)
                    {
                        DestroyRoom(currentRoom.Id);
                    }
                }
                else
                {
                    Debug.LogError($"[DungeonGenerator] Не удалось переместить игрока даже после повторной попытки! Расстояние: {distance}. Старая комната не будет уничтожена.");
                }
            }
        }

        public void LoadRoom(string nextRoomId)
        {
            if (isGenerating)
            {
                Debug.LogWarning("[DungeonGenerator] Нельзя загрузить комнату во время генерации!");
                return;
            }

            StartCoroutine(LoadRoomCoroutine(nextRoomId));
        }

        private Transform FindSpawnPoint(GameObject roomInstance, RoomManager roomManager)
        {
            if (roomInstance == null || roomManager == null)
            {
                Debug.LogError("[DungeonGenerator] FindSpawnPoint: Неверные параметры!");
                return null;
            }

            // Сначала ищем через Transform
            Transform spawnPoint = roomInstance.transform.Find("PlayerSpawnPoint");
            if (spawnPoint != null)
            {
                Debug.Log($"[DungeonGenerator] FindSpawnPoint: Найдена точка спавна через Transform на позиции {spawnPoint.position}");
                return spawnPoint;
            }

            // Если не нашли, пробуем через RoomManager
            if (roomManager.PlayerSpawnPoint != null)
            {
                Debug.Log($"[DungeonGenerator] FindSpawnPoint: Найдена точка спавна через RoomManager на позиции {roomManager.PlayerSpawnPoint.position}");
                return roomManager.PlayerSpawnPoint;
            }

            Debug.LogError("[DungeonGenerator] FindSpawnPoint: Точка спавна не найдена!");
            return null;
        }

        private IEnumerator ActivateRoom(RoomNode room)
        {
            if (room == null || room.RoomInstance == null)
            {
                Debug.LogError("[DungeonGenerator] ActivateRoom: Неверные параметры!");
                yield break;
            }

            // Активируем новую комнату
            room.RoomInstance.SetActive(true);
            Debug.Log($"[DungeonGenerator] Комната {room.Id} активирована");

            // Ищем RoomManager
            var roomManager = room.RoomInstance.GetComponent<RoomManager>();
            if (roomManager == null)
            {
                Debug.LogError($"[DungeonGenerator] RoomManager не найден в комнате {room.Id}!");
                yield break;
            }

            yield return null;
        }

        private IEnumerator MovePlayerToNewRoom(RoomNode nextRoom, GameObject player, RoomNode currentRoom)
        {
            if (nextRoom == null || nextRoom.RoomInstance == null || player == null)
            {
                Debug.LogError("[DungeonGenerator] MovePlayerToNewRoom: Неверные параметры!");
                yield break;
            }

            var roomManager = nextRoom.RoomInstance.GetComponent<RoomManager>();
            if (roomManager == null)
            {
                Debug.LogError($"[DungeonGenerator] RoomManager не найден в комнате {nextRoom.Id}!");
                yield break;
            }

            // Ищем точку спавна
            Transform spawnPoint = FindSpawnPoint(nextRoom.RoomInstance, roomManager);
            if (spawnPoint == null)
            {
                yield break;
            }

            // Перемещаем игрока и уничтожаем старую комнату
            yield return StartCoroutine(MovePlayerAndDestroyRoom(player, spawnPoint, currentRoom, nextRoom.Id));
        }

        private IEnumerator LoadRoomCoroutine(string nextRoomId)
        {
            Debug.Log($"[DungeonGenerator] LoadRoom: Начало загрузки комнаты {nextRoomId}");

            // Находим комнату по ID
            RoomNode nextRoom = roomNodes.Find(r => r.Id == nextRoomId);
            if (nextRoom == null)
            {
                Debug.LogError($"[DungeonGenerator] Комната с ID {nextRoomId} не найдена!");
                yield break;
            }

            // Находим текущую комнату игрока
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                Debug.LogError("[DungeonGenerator] Игрок не найден!");
                yield break;
            }

            // Находим текущую комнату по позиции игрока
            var currentRoom = GetRoomNodeAtPosition(player.transform.position);
            if (currentRoom == null)
            {
                Debug.LogError("[DungeonGenerator] Текущая комната не найдена!");
                yield break;
            }

            // Создаем инстанс комнаты, если его еще нет
            if (nextRoom.RoomInstance == null)
            {
                Debug.Log($"[DungeonGenerator] Создание инстанса комнаты {nextRoomId}");
                CreateRoomInstance(nextRoom);
                if (nextRoom.RoomInstance == null)
                {
                    Debug.LogError($"[DungeonGenerator] Не удалось создать инстанс комнаты {nextRoomId}!");
                    yield break;
                }
            }

            // Активируем комнату
            yield return StartCoroutine(ActivateRoom(nextRoom));

            // Перемещаем игрока в новую комнату
            yield return StartCoroutine(MovePlayerToNewRoom(nextRoom, player, currentRoom));

            Debug.Log($"[DungeonGenerator] Загрузка комнаты {nextRoomId} завершена");
        }

        private string GetNextRoomId(RoomNode currentRoom, Vector2Int direction)
        {
            if (currentRoom == null)
            {
                Debug.LogError("[DungeonGenerator] Текущая комната не найдена!");
                return null;
            }

            Vector2Int nextPosition = currentRoom.Position + direction;
            Debug.Log($"[DungeonGenerator] Позиция следующей комнаты: {nextPosition}");

            // Ищем комнату по позиции в списке всех комнат
            var nextRoom = roomNodes.FirstOrDefault(r => r.Position == nextPosition);
            if (nextRoom != null)
            {
                Debug.Log($"[DungeonGenerator] Найдена следующая комната с ID: {nextRoom.Id}");
                return nextRoom.Id;
            }

            Debug.LogWarning("[DungeonGenerator] Следующая комната не найдена!");
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
            
            try 
            {
                // Отключаем физику на время перемещения
                Rigidbody rb = player.GetComponent<Rigidbody>();
                bool wasKinematic = false;
                
                if (rb != null)
                {
                    wasKinematic = rb.isKinematic;
                    rb.isKinematic = true;
                    rb.detectCollisions = false;
                    Debug.Log("[DungeonGenerator] MovePlayerToSpawnPoint: Физика игрока временно отключена");
                }
                
                // Запоминаем и отключаем коллайдеры
                Collider[] playerColliders = player.GetComponentsInChildren<Collider>();
                bool[] collidersState = new bool[playerColliders.Length];
                
                for (int i = 0; i < playerColliders.Length; i++)
                {
                    collidersState[i] = playerColliders[i].enabled;
                    playerColliders[i].enabled = false;
                }
                
                // Отключаем все скрипты
                MonoBehaviour[] scripts = player.GetComponentsInChildren<MonoBehaviour>();
                bool[] scriptsState = new bool[scripts.Length];
                
                for (int i = 0; i < scripts.Length; i++)
                {
                    if (scripts[i] != null && scripts[i] is not DungeonGenerator)
                    {
                        scriptsState[i] = scripts[i].enabled;
                        scripts[i].enabled = false;
                    }
                }
                
                // Перемещаем игрока
                player.transform.position = spawnPoint.position;
                player.transform.rotation = spawnPoint.rotation;
                
                // Обновляем Rigidbody
                if (rb != null)
                {
                    // Сбрасываем скорости только если тело не кинематическое
                    if (!wasKinematic)
                    {
                        rb.linearVelocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                    }
                    
                    rb.position = spawnPoint.position;
                    rb.rotation = spawnPoint.rotation;
                    rb.Sleep();
                    
                    // Восстанавливаем исходное состояние
                    rb.isKinematic = wasKinematic;
                    rb.detectCollisions = true;
                }
                
                // Восстанавливаем скрипты
                for (int i = 0; i < scripts.Length; i++) 
                {
                    if (scripts[i] != null && scripts[i] is not DungeonGenerator)
                    {
                        scripts[i].enabled = scriptsState[i];
                    }
                }
                
                // Восстанавливаем коллайдеры
                for (int i = 0; i < playerColliders.Length; i++)
                {
                    playerColliders[i].enabled = collidersState[i];
                }
                
                Debug.Log($"[DungeonGenerator] MovePlayerToSpawnPoint: Игрок успешно перемещен в позицию: {player.transform.position}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[DungeonGenerator] MovePlayerToSpawnPoint: Ошибка при перемещении игрока: {e.Message}\n{e.StackTrace}");
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
            if (room == null)
            {
                Debug.LogWarning($"[DungeonGenerator] DestroyRoom: Комната с ID {roomId} не найдена!");
                return;
            }

            if (room.RoomInstance == null)
            {
                Debug.LogWarning($"[DungeonGenerator] DestroyRoom: RoomInstance комнаты {roomId} уже уничтожен!");
                return;
            }

            Debug.Log($"[DungeonGenerator] DestroyRoom: Начало уничтожения комнаты {roomId}");
            
            // Сначала деактивируем
            room.RoomInstance.SetActive(false);
            
            // Уничтожаем GameObject
            Destroy(room.RoomInstance);
            
            // Очищаем ссылку
            room.SetRoomInstance(null);
            
            Debug.Log($"[DungeonGenerator] DestroyRoom: Комната {roomId} успешно уничтожена");
        }

        public List<RoomNode> GetAvailableRooms()
        {
            Debug.Log("[DungeonGenerator] GetAvailableRooms: Начало поиска доступных комнат");
            
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                Debug.LogError("[DungeonGenerator] GetAvailableRooms: Игрок не найден!");
                return new List<RoomNode>();
            }

            var currentRoom = GetRoomNodeAtPosition(player.transform.position);
            if (currentRoom == null)
            {
                Debug.LogError("[DungeonGenerator] GetAvailableRooms: Текущая комната не найдена! Попытаемся продолжить со всеми непосещенными комнатами.");
                // Возвращаем все непосещенные обычные комнаты даже если текущая комната не найдена
                return roomNodes
                    .Where(r => r.RoomType != dungeonConfig.bossRoomType && !r.WasVisited)
                    .ToList();
            }

            Debug.Log($"[DungeonGenerator] GetAvailableRooms: Текущая комната: {currentRoom.Id}, всего комнат в подземелье: {roomNodes?.Count ?? 0}");

            // Проверяем состояние каждой комнаты
            foreach (var room in roomNodes)
            {
                string reason = "";
                if (room.RoomType == dungeonConfig.bossRoomType)
                    reason += "Это комната босса. ";
                if (room.RoomType == dungeonConfig.startRoomType)
                    reason += "Это стартовая комната. ";
                if (room == currentRoom)
                    reason += "Это текущая комната. ";
                if (room.WasVisited)
                {
                    reason += "Комната уже была посещена. ";
                    Debug.Log($"[DungeonGenerator] GetAvailableRooms: Комната {room.Id} в позиции {room.Position} была посещена");
                }
                
                Debug.Log($"[DungeonGenerator] Комната {room.Id}: {(string.IsNullOrEmpty(reason) ? "Доступна для телепортации" : $"Недоступна потому что: {reason}")}");
            }

            // Проверяем, все ли обычные комнаты посещены
            bool allRegularRoomsVisited = roomNodes
                .Where(r => r.RoomType != dungeonConfig.bossRoomType && r.RoomType != dungeonConfig.startRoomType)
                .All(r => r.WasVisited);

            // Если все обычные комнаты посещены, даем доступ к комнате босса
            if (allRegularRoomsVisited)
            {
                Debug.Log("[DungeonGenerator] Все обычные комнаты посещены, открываем доступ к комнате босса");
                var bossRoom = roomNodes.FirstOrDefault(r => 
                    r.RoomType == dungeonConfig.bossRoomType && 
                    r != currentRoom && 
                    !r.WasVisited);

                if (bossRoom != null)
                {
                    Debug.Log($"[DungeonGenerator] GetAvailableRooms: Найдена комната босса {bossRoom.Id} для телепортации");
                    return new List<RoomNode> { bossRoom };
            }
            else
            {
                    Debug.LogWarning("[DungeonGenerator] GetAvailableRooms: Не найдена непосещенная комната босса!");
                }
            }

            // Возвращаем все непосещенные комнаты (кроме босса и стартовой)
            var availableRooms = roomNodes.Where(r => 
                r.RoomType != dungeonConfig.bossRoomType && 
                r.RoomType != dungeonConfig.startRoomType && 
                r != currentRoom && 
                !r.WasVisited
            ).ToList();

            Debug.Log($"[DungeonGenerator] GetAvailableRooms: Найдено доступных комнат: {availableRooms.Count}");
            foreach (var room in availableRooms)
            {
                Debug.Log($"[DungeonGenerator] GetAvailableRooms: Доступная комната: {room.Id} в позиции {room.Position}");
            }
            
            // Если нет доступных комнат и не все посещены (странная ситуация)
            if (availableRooms.Count == 0 && !allRegularRoomsVisited)
            {
                Debug.LogWarning("[DungeonGenerator] GetAvailableRooms: Нет доступных комнат, но не все комнаты посещены! Возвращаем все непосещенные обычные комнаты.");
                return roomNodes
                    .Where(r => r.RoomType != dungeonConfig.bossRoomType && !r.WasVisited)
                    .ToList();
            }
            
            return availableRooms;
        }

        public RoomNode GetBossRoom()
        {
            return roomNodes.FirstOrDefault(r => r.RoomType == dungeonConfig.bossRoomType);
        }

        private void UpdateRoomVisibility(RoomNode currentRoom)
        {
            if (currentRoom == null)
            {
                Debug.LogWarning("[DungeonGenerator] UpdateRoomVisibility: currentRoom is null");
                return;
            }

            Debug.Log($"[DungeonGenerator] UpdateRoomVisibility: Обновление видимости комнат. Текущая комната: {currentRoom.Id}");

            // Активируем текущую комнату
            if (currentRoom.RoomInstance != null)
            {
                currentRoom.RoomInstance.SetActive(true);
                Debug.Log($"[DungeonGenerator] UpdateRoomVisibility: Активирована комната {currentRoom.Id}");
                }
                else
                {
                Debug.LogWarning($"[DungeonGenerator] UpdateRoomVisibility: RoomInstance текущей комнаты {currentRoom.Id} не существует!");
            }

            // Деактивируем все остальные комнаты
            foreach (var room in roomNodes)
            {
                if (room != currentRoom && room.RoomInstance != null)
                {
                    room.RoomInstance.SetActive(false);
                    Debug.Log($"[DungeonGenerator] UpdateRoomVisibility: Деактивирована комната {room.Id}");
                }
            }
        }
    }
} 