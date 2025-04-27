using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using DungeonGeneration.ScriptableObjects;
using System;
using DungeonGeneration.Scripts.Enemies;
using BreakTheCycle;
using DungeonGeneration.Scripts.Health;
using UnityEngine.AI;
using Unity.AI.Navigation;

namespace DungeonGeneration.Scripts
{
    public class RoomManager : MonoBehaviour
    {
        [Header("Room Settings")]
        [SerializeField] private RoomTypeSO roomType;
        [SerializeField] private Transform playerSpawnPoint;
        [SerializeField] private Transform[] portalPositions;
        [SerializeField] private Transform[] enemySpawnPoints;
        [SerializeField] private Transform[] rewardSpawnPoints;
        [SerializeField] private bool isRoomCleared = false;
        
        [Header("Prefabs")]
        [SerializeField] private GameObject portalPrefab;

        [Header("Boss Settings")]
        [SerializeField] private Transform bossSpawnPoint;

        [Header("Waves Settings")]
        [SerializeField] private int totalWaves = 1;
        private int currentWave = 0;
        [Header("Enemy Spawn Settings")]
        [SerializeField] private float enemySpawnScatterRadius = 1.0f;

        [Header("Enemy Configurations")]
        [SerializeField] public EnemyConfigSO[] enemyConfigs;

        private DungeonGenerator dungeonGenerator;
        private RoomNode roomNode;
        private List<GameObject> activeEnemies = new List<GameObject>();
        private List<GameObject> activePortals = new List<GameObject>();
        
        public event Action OnRoomCleared;
        public event Action OnPlayerEnter;
        public event Action OnPlayerExit;
        
        public RoomTypeSO RoomType => roomType;
        public bool IsRoomCleared => isRoomCleared;
        public Transform PlayerSpawnPoint => playerSpawnPoint;

        private bool isInitialized = false;

        private void Awake()
        {
            ValidateReferences();
        }

        private void Start()
        {
            if (!isInitialized)
            {
                InitializeRoom();
            }
        }

        private void ValidateReferences()
        {
            if (roomType == null)
            {
                Debug.LogError("RoomType не назначен!");
                return;
            }

            if (playerSpawnPoint == null)
            {
                Debug.LogError("PlayerSpawnPoint не назначен!");
                return;
            }

            if (portalPositions == null || portalPositions.Length == 0)
            {
                Debug.LogError("PortalPositions не назначены!");
                return;
            }

            if (portalPrefab == null)
            {
                Debug.LogError("PortalPrefab не назначен!");
                return;
            }

            // Ищем DungeonGenerator в сцене
            dungeonGenerator = FindObjectOfType<DungeonGenerator>();
            if (dungeonGenerator == null)
            {
                Debug.LogError("DungeonGenerator не найден в сцене!");
                return;
            }

            // Проверяем точки спавна врагов только если комната требует очистки
            if (roomType.requiresCleaning)
            {
                if (enemySpawnPoints == null || enemySpawnPoints.Length == 0)
                {
                    Debug.LogError($"Enemy spawn points not set in {gameObject.name}!");
                    return;
                }

                // Не проверяем enemyPrefabs для босс-комнаты
                if (roomType.typeName != null && roomType.typeName.ToLower().Contains("boss"))
                {
                    // Для босс-комнаты enemyPrefabs может быть пустым
                    return;
                }
            }
        }

        public void InitializeRoom()
        {
            if (isInitialized) return;
            
            if (dungeonGenerator == null)
            {
                Debug.LogError("DungeonGenerator not found! Please ensure ValidateReferences was called first.");
                return;
            }

            roomNode = dungeonGenerator.GetRoomNodeAtPosition(transform.position);
            if (roomNode == null)
            {
                Debug.LogError("RoomNode not found for this room!");
                return;
            }

            SetupRoom();
            isInitialized = true;
        }

        private void SetupRoom()
        {
            if (dungeonGenerator == null)
            {
                Debug.LogError("DungeonGenerator not found! Please ensure ValidateReferences was called first.");
                return;
            }

            if (roomType != null && roomType.typeName != null && roomType.typeName.ToLower().Contains("boss"))
            {
                // Новый способ: ищем EnemyConfigSO с isBoss в массиве enemyConfigs этой комнаты
                var bossConfig = enemyConfigs != null ? enemyConfigs.FirstOrDefault(cfg => cfg.isBoss && cfg.enemyPrefabs != null && cfg.enemyPrefabs.Length > 0 && cfg.enemyPrefabs[0] != null) : null;
                if (bossConfig != null)
                {
                    var prefabToSpawn = bossConfig.enemyPrefabs[0];
                    if (prefabToSpawn != null && bossSpawnPoint != null)
                    {
                        GameObject boss = Instantiate(prefabToSpawn, bossSpawnPoint.position, bossSpawnPoint.rotation, transform);
                        activeEnemies.Add(boss);
                        var enemyHealth = boss.GetComponent<EnemyHealth>();
                        if (enemyHealth != null)
                        {
                            enemyHealth.OnDeath += HandleEnemyDeath;
                        }
                        Debug.Log($"[RoomManager] Босс заспавнен в точке {bossSpawnPoint.position}");
                    }
                    else
                    {
                        Debug.LogError("[RoomManager] Не назначен bossSpawnPoint или prefabToSpawn для босс-комнаты!");
                    }
                }
                else
                {
                    Debug.LogError("[RoomManager] Не найден EnemyConfigSO с isBoss для этой комнаты!");
                }
            }

            currentWave = 0;
            if (IsCombatRoom())
            {
                StartNextWave();
            }

            SetupPortals();
        }

        private bool IsCombatRoom()
        {
            return roomType != null && roomType.minEnemies > 0;
        }

        private void StartNextWave()
        {
            if (currentWave < totalWaves)
            {
                currentWave++;
                Debug.Log($"[RoomManager] Запуск волны {currentWave}/{totalWaves}");
                SpawnEnemiesWave();
            }
            else
            {
                Debug.Log("[RoomManager] Все волны завершены!");
            }
        }

        private void SpawnEnemiesWave()
        {
            if (enemyConfigs == null || enemyConfigs.Length == 0)
            {
                Debug.LogWarning("[RoomManager] EnemyConfigs пустой — враги не будут заспавнены.");
                return;
            }

            if (roomType == null)
            {
                Debug.LogError($"Room type not set in {gameObject.name}!");
                return;
            }

            if (roomType.canBeFirst)
            {
                return;
            }

            List<Transform> availableSpawnPoints = new List<Transform>(enemySpawnPoints);
            Debug.Log($"[RoomManager] Спавним врагов в {availableSpawnPoints.Count} точках (волна {currentWave})");

            foreach (var spawnPoint in availableSpawnPoints)
            {
                // Фильтруем подходящие enemy configs по типу комнаты
                var suitableConfigs = enemyConfigs.Where(cfg => 
                    (roomType == dungeonGenerator.DungeonConfig.eliteCombatRoomType && cfg.isElite) ||
                    (roomType == dungeonGenerator.DungeonConfig.basicCombatRoomType && !cfg.isElite && !cfg.isBoss) ||
                    (roomType == dungeonGenerator.DungeonConfig.bossRoomType && cfg.isBoss)
                ).ToList();

                if (suitableConfigs.Count == 0)
                {
                    Debug.LogWarning($"[RoomManager] Нет подходящих enemy configs для типа комнаты {roomType.typeName}!");
                    continue;
                }

                // Случайно выбираем enemy config для этой точки
                var enemyConfig = suitableConfigs[UnityEngine.Random.Range(0, suitableConfigs.Count)];
                int minCount = Mathf.Max(0, enemyConfig.minGroupSize);
                int maxCount = Mathf.Max(minCount, enemyConfig.maxGroupSize);
                int count = UnityEngine.Random.Range(minCount, maxCount + 1);

                for (int i = 0; i < count; i++)
                {
                    if (enemyConfig.enemyPrefabs == null || enemyConfig.enemyPrefabs.Length == 0)
                    {
                        Debug.LogError($"[RoomManager] enemyPrefabs не назначены в EnemyConfigSO {enemyConfig.enemyName}!");
                        continue;
                    }
                    var prefab = enemyConfig.enemyPrefabs[UnityEngine.Random.Range(0, enemyConfig.enemyPrefabs.Length)];
                    if (prefab == null)
                    {
                        Debug.LogError($"[RoomManager] Один из enemyPrefabs равен null в EnemyConfigSO {enemyConfig.enemyName}!");
                        continue;
                    }
                    // Добавляем разброс позиции
                    Vector2 offset2D = UnityEngine.Random.insideUnitCircle * enemySpawnScatterRadius;
                    Vector3 spawnPos = spawnPoint.position + new Vector3(offset2D.x, 0, offset2D.y);
                    GameObject enemy = Instantiate(prefab, spawnPos, spawnPoint.rotation, transform);
                    activeEnemies.Add(enemy);

                    var enemyHealth = enemy.GetComponent<EnemyHealth>();
                    if (enemyHealth != null)
                    {
                        enemyHealth.OnDeath += HandleEnemyDeath;
                    }
                    Debug.Log($"[RoomManager] Создан враг {enemy.name} (тип: {enemyConfig.enemyName}) на позиции {spawnPoint.position} (волна {currentWave})");
                }
            }
        }

        private void SetupPortals()
        {
            if (portalPositions == null || portalPositions.Length == 0)
            {
                Debug.LogWarning("No portal positions set in room!");
                return;
            }

            // Создаем портал в комнате
            SpawnPortal(portalPositions[0].position);
            Debug.Log($"[RoomManager] Создан портал в комнате типа {roomType.typeName}");
        }

        private GameObject SpawnPortal(Vector3 position)
        {
            GameObject portal = Instantiate(portalPrefab, position, Quaternion.identity, transform);
            activePortals.Add(portal);
            return portal;
        }

        private void HandleEnemyDeath(GameObject enemy)
        {
            if (enemy != null)
            {
                activeEnemies.Remove(enemy);
                Debug.Log($"[RoomManager] Враг {enemy.name} погиб. Осталось врагов: {activeEnemies.Count}");
            }

            if (activeEnemies.Count == 0 && roomNode != null)
            {
                Debug.Log($"[RoomManager] Все враги волны {currentWave} побеждены");
                if (currentWave < totalWaves)
                {
                    StartNextWave();
                }
                else
                {
                    roomNode.ClearRoom();
                    isRoomCleared = true;
                    OnRoomCleared?.Invoke();
                    Debug.Log("[RoomManager] Все волны завершены, комната очищена!");
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                OnPlayerEnter?.Invoke();
            }
        }
        
        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                OnPlayerExit?.Invoke();
            }
        }
        
        public Vector3 GetPlayerSpawnPosition()
        {
            if (playerSpawnPoint == null)
            {
                Debug.LogError($"[RoomManager] Точка спавна игрока не назначена в комнате {gameObject.name}!");
                return transform.position; // Возвращаем позицию комнаты как запасной вариант
            }
            return playerSpawnPoint.position;
        }

        public bool AreAllEnemiesDefeated()
        {
            return activeEnemies.Count == 0;
        }

        public void LoadNextRoom(string nextRoomId)
        {
            if (string.IsNullOrEmpty(nextRoomId))
            {
                Debug.LogError("Invalid room ID provided!");
                return;
            }

            dungeonGenerator?.LoadRoom(nextRoomId);
        }

        public string GetRoomId()
        {
            return roomNode?.Id;
        }

        public void SetRoomType(RoomTypeSO type)
        {
            if (roomType == type) return; // Don't reinitialize if type hasn't changed
            roomType = type;
            InitializeRoom();
        }

        private void LoadRoom(RoomNode roomNode)
        {
            Debug.Log($"[RoomManager] Загрузка комнаты {roomNode?.Id ?? "null"}");
            
            // Проверяем наличие NavMeshSurface в любом случае
            var navMeshSurface = gameObject.GetComponent<NavMeshSurface>();
            if (navMeshSurface == null)
            {
                Debug.LogError("[RoomManager] NavMeshSurface не найден на префабе комнаты! Добавляем компонент.");
                navMeshSurface = gameObject.AddComponent<NavMeshSurface>();
                
                // Устанавливаем базовые настройки
                navMeshSurface.collectObjects = CollectObjects.All;
                navMeshSurface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
                navMeshSurface.layerMask = -1; // Все слои
            }

            // Строим NavMesh для всех комнат, где могут быть враги
            if (roomType != null && roomType.requiresCleaning)
            {
                Debug.Log($"[RoomManager] Начинаем построение NavMesh для комнаты {roomNode?.Id ?? "null"}");
                try
                {
                    navMeshSurface.BuildNavMesh();
                    Debug.Log("[RoomManager] NavMesh успешно построен");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[RoomManager] Ошибка при построении NavMesh: {e.Message}");
                }
            }
            else
            {
                Debug.Log("[RoomManager] Пропускаем построение NavMesh - комната не требует очистки");
            }
        }
    }
} 