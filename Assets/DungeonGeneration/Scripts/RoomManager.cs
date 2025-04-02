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
        
        [Header("Prefabs")]
        [SerializeField] private GameObject portalPrefab;
        [SerializeField] private GameObject[] enemyPrefabs;

        private DungeonGenerator dungeonGenerator;
        private RoomNode roomNode;
        private List<GameObject> activeEnemies = new List<GameObject>();
        private List<GameObject> activePortals = new List<GameObject>();
        private bool isRoomCleared = false;
        
        public event Action OnRoomCleared;
        public event Action OnPlayerEnter;
        public event Action OnPlayerExit;
        
        public RoomTypeSO RoomType => roomType;
        public bool IsRoomCleared => isRoomCleared;
        public Transform PlayerSpawnPoint => playerSpawnPoint;

        private void Awake()
        {
            ValidateReferences();
        }

        private void Start()
        {
            InitializeRoom();
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

                if (enemyPrefabs == null || enemyPrefabs.Length == 0)
                {
                    Debug.LogError($"Enemy prefabs not set in {gameObject.name}!");
                    return;
                }
            }
        }

        public void InitializeRoom()
        {
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
        }

        private void SetupRoom()
        {
            if (IsCombatRoom())
            {
                SpawnEnemies();
            }

            SetupPortals();
            }

        private bool IsCombatRoom()
        {
            return roomType != null && roomType.minEnemies > 0;
        }

        private void SpawnEnemies()
        {
            if (roomType == null)
            {
                Debug.LogError($"Room type not set in {gameObject.name}!");
                return;
            }

            // Не спавним врагов в стартовой комнате
            if (roomType.canBeFirst)
            {
                return;
            }

            int enemyCount = UnityEngine.Random.Range(roomType.minEnemies, roomType.maxEnemies + 1);
            List<Transform> availableSpawnPoints = new List<Transform>(enemySpawnPoints);

            for (int i = 0; i < enemyCount; i++)
            {
                if (availableSpawnPoints.Count == 0) break;

                int spawnIndex = UnityEngine.Random.Range(0, availableSpawnPoints.Count);
                Transform spawnPoint = availableSpawnPoints[spawnIndex];
                availableSpawnPoints.RemoveAt(spawnIndex);

                // Выбираем врага в зависимости от типа комнаты
                GameObject enemyPrefab;
                if (dungeonGenerator != null && roomType == dungeonGenerator.DungeonConfig.eliteCombatRoomType)
                {
                    // В элитной комнате всегда спавним элитного врага
                    enemyPrefab = enemyPrefabs[enemyPrefabs.Length - 1];
                }
                else
                {
                    // В обычной комнате случайный враг
                    enemyPrefab = enemyPrefabs[UnityEngine.Random.Range(0, enemyPrefabs.Length)];
                }

                GameObject enemy = Instantiate(enemyPrefab, spawnPoint.position, Quaternion.identity, transform);
                activeEnemies.Add(enemy);

                var enemyHealth = enemy.GetComponent<EnemyHealth>();
                if (enemyHealth != null)
                {
                    enemyHealth.OnDeath += HandleEnemyDeath;
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

            // В стартовой комнате создаем портал
            if (roomType == dungeonGenerator.DungeonConfig.startRoomType)
            {
                SpawnPortal(portalPositions[0].position);
            }
            // В обычных комнатах создаем портал
            else if (roomType != dungeonGenerator.DungeonConfig.bossRoomType)
            {
                SpawnPortal(portalPositions[0].position);
            }
            // В боссовой комнате не создаем порталов
        }

        private GameObject SpawnPortal(Vector3 position)
        {
            GameObject portal = Instantiate(portalPrefab, position, Quaternion.identity, transform);
            activePortals.Add(portal);
            
            // Добавляем компонент InteractablePortal, если его нет
            var interactablePortal = portal.GetComponent<InteractablePortal>();
            if (interactablePortal == null)
            {
                interactablePortal = portal.AddComponent<InteractablePortal>();
            }
            
            return portal;
        }

        private void HandleEnemyDeath(GameObject enemy)
        {
            if (enemy != null)
            {
                activeEnemies.Remove(enemy);
            }
            
            if (activeEnemies.Count == 0 && roomNode != null)
            {
                roomNode.ClearRoom();
                isRoomCleared = true;
                OnRoomCleared?.Invoke();
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
            roomType = type;
            InitializeRoom();
        }

        private void LoadRoom(RoomNode roomNode)
        {
            // Создаем NavMesh только для комнат, где могут быть враги
            if (roomType.requiresCleaning)
            {
                var navMeshSurface = gameObject.GetComponent<NavMeshSurface>();
                if (navMeshSurface != null)
                {
                    navMeshSurface.BuildNavMesh();
                }
                else
                {
                    Debug.LogError("[RoomManager] NavMeshSurface не найден на префабе комнаты!");
                }
            }
        }

        private void OnValidate()
        {
            // Проверяем и корректируем высоту точки спавна игрока
            if (playerSpawnPoint != null)
            {
                // Получаем высоту пола (предполагая, что пол находится на Y = 0)
                float floorHeight = 0f;
                
                // Проверяем, не слишком ли низко точка спавна
                if (playerSpawnPoint.position.y <= floorHeight)
                {
                    // Поднимаем точку спавна на 1 единицу над полом
                    Vector3 newPosition = playerSpawnPoint.position;
                    newPosition.y = floorHeight + 1f;
                    playerSpawnPoint.position = newPosition;
                    Debug.LogWarning($"[RoomManager] Точка спавна игрока была слишком низко. Высота скорректирована на {newPosition.y}");
                }
            }

            // Проверяем и корректируем высоту точек спавна врагов
            if (enemySpawnPoints != null)
            {
                float floorHeight = 0f;
                foreach (var spawnPoint in enemySpawnPoints)
                {
                    if (spawnPoint != null && spawnPoint.position.y <= floorHeight)
                    {
                        Vector3 newPosition = spawnPoint.position;
                        newPosition.y = floorHeight + 1f;
                        spawnPoint.position = newPosition;
                        Debug.LogWarning($"[RoomManager] Точка спавна врага была слишком низко. Высота скорректирована на {newPosition.y}");
                    }
                }
            }
        }
    }
} 