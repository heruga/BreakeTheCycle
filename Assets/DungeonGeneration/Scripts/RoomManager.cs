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
        [SerializeField] private GameObject[] enemyPrefabs;

        [Header("Boss Settings")]
        [SerializeField] private GameObject bossPrefab;
        [SerializeField] private Transform bossSpawnPoint;

        [Header("Waves Settings")]
        [SerializeField] private int totalWaves = 1;
        private int currentWave = 0;

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

                if (enemyPrefabs == null || enemyPrefabs.Length == 0)
                {
                    Debug.LogError($"Enemy prefabs not set in {gameObject.name}!");
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
                GameObject prefabToSpawn = bossPrefab;
                if (prefabToSpawn == null)
                {
                    var bossConfig = Resources.FindObjectsOfTypeAll<EnemyConfigSO>().FirstOrDefault(cfg => cfg.isBoss && cfg.enemyPrefab != null);
                    if (bossConfig != null)
                        prefabToSpawn = bossConfig.enemyPrefab;
                }
                if (prefabToSpawn != null && bossSpawnPoint != null)
                {
                    Instantiate(prefabToSpawn, bossSpawnPoint.position, bossSpawnPoint.rotation, transform);
                    Debug.Log($"[RoomManager] Босс заспавнен в точке {bossSpawnPoint.position}");
                }
                else
                {
                    Debug.LogError("[RoomManager] Не назначен bossPrefab или bossSpawnPoint для босс-комнаты, и не найден BossEnemyConfigSO!");
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
            if (enemyPrefabs == null || enemyPrefabs.Length == 0)
            {
                Debug.LogWarning("[RoomManager] EnemyPrefabs пустой — враги не будут заспавнены.");
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
            int spawnCount = Mathf.Min(UnityEngine.Random.Range(roomType.minEnemies, roomType.maxEnemies + 1), availableSpawnPoints.Count);
            Debug.Log($"[RoomManager] Спавним {spawnCount} врагов в волне {currentWave}");

            for (int i = 0; i < spawnCount; i++)
            {
                if (availableSpawnPoints.Count == 0) break;
                int spawnIndex = UnityEngine.Random.Range(0, availableSpawnPoints.Count);
                Transform spawnPoint = availableSpawnPoints[spawnIndex];
                availableSpawnPoints.RemoveAt(spawnIndex);

                GameObject enemyPrefab;
                if (dungeonGenerator != null && roomType == dungeonGenerator.DungeonConfig.eliteCombatRoomType)
                {
                    enemyPrefab = enemyPrefabs[enemyPrefabs.Length - 1];
                }
                else if (dungeonGenerator != null && roomType == dungeonGenerator.DungeonConfig.bossRoomType)
                {
                    enemyPrefab = enemyPrefabs[enemyPrefabs.Length - 1];
                }
                else
                {
                    int enemyIndex = UnityEngine.Random.Range(0, enemyPrefabs.Length);
                    enemyPrefab = enemyPrefabs[enemyIndex];
                }

                GameObject enemy = Instantiate(enemyPrefab, spawnPoint.position, Quaternion.identity, transform);
                activeEnemies.Add(enemy);

                var enemyHealth = enemy.GetComponent<EnemyHealth>();
                if (enemyHealth != null)
                {
                    enemyHealth.OnDeath += HandleEnemyDeath;
                }
                Debug.Log($"[RoomManager] Создан враг {enemy.name} на позиции {spawnPoint.position} (волна {currentWave})");
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