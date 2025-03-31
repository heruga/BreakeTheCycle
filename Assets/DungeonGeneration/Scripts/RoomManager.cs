using UnityEngine;
using System.Collections.Generic;
using DungeonGeneration.ScriptableObjects;

namespace DungeonGeneration
{
    public class RoomManager : MonoBehaviour
    {
        [Header("Room Settings")]
        [SerializeField] private RoomTypeSO roomType;
        [SerializeField] private Transform[] doorPositions;
        [SerializeField] private Transform[] enemySpawnPoints;
        [SerializeField] private Transform[] rewardSpawnPoints;

        private DungeonGenerator dungeonGenerator;
        private RoomNode roomNode;
        private List<GameObject> activeEnemies = new List<GameObject>();
        private bool isCleared = false;

        private void Start()
        {
            dungeonGenerator = FindObjectOfType<DungeonGenerator>();
            if (dungeonGenerator == null)
            {
                Debug.LogError("DungeonGenerator not found in scene!");
                return;
            }

            // Находим соответствующий RoomNode
            roomNode = dungeonGenerator.GetRoomNodeAtPosition(transform.position);
            if (roomNode == null)
            {
                Debug.LogError("RoomNode not found for this room!");
                return;
            }

            // Настраиваем комнату
            SetupRoom();
        }

        private void SetupRoom()
        {
            // Спавним врагов
            if (roomType.roomType == RoomType.Combat || roomType.roomType == RoomType.Boss)
            {
                SpawnEnemies();
            }

            // Спавним награды
            if (roomType.roomType == RoomType.Reward)
            {
                SpawnRewards();
            }

            // Настраиваем двери
            SetupDoors();
        }

        private void SpawnEnemies()
        {
            foreach (var spawnPoint in enemySpawnPoints)
            {
                if (roomType.possibleEnemyPrefabs.Length == 0) continue;

                GameObject enemyPrefab = roomType.possibleEnemyPrefabs[
                    Random.Range(0, roomType.possibleEnemyPrefabs.Length)
                ];

                GameObject enemy = Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);
                activeEnemies.Add(enemy);

                // Подписываемся на уничтожение врага
                var enemyHealth = enemy.GetComponent<Health>();
                if (enemyHealth != null)
                {
                    enemyHealth.OnDeath += OnEnemyDeath;
                }
            }
        }

        private void SpawnRewards()
        {
            foreach (var spawnPoint in rewardSpawnPoints)
            {
                if (roomType.possibleRewardPrefabs.Length == 0) continue;

                GameObject rewardPrefab = roomType.possibleRewardPrefabs[
                    Random.Range(0, roomType.possibleRewardPrefabs.Length)
                ];

                Instantiate(rewardPrefab, spawnPoint.position, spawnPoint.rotation);
            }
        }

        private void SetupDoors()
        {
            // Деактивируем все двери по умолчанию
            foreach (var door in doorPositions)
            {
                door.gameObject.SetActive(false);
            }

            // Активируем только те двери, которые ведут к соединенным комнатам
            foreach (var connectedRoom in roomNode.ConnectedRooms)
            {
                Vector3 direction = new Vector3(
                    connectedRoom.Position.x - roomNode.Position.x,
                    0,
                    connectedRoom.Position.y - roomNode.Position.y
                );

                // Находим соответствующую дверь
                foreach (var door in doorPositions)
                {
                    if (Vector3.Dot(door.forward, direction) > 0.5f)
                    {
                        door.gameObject.SetActive(true);
                        break;
                    }
                }
            }
        }

        private void OnEnemyDeath(GameObject enemy)
        {
            activeEnemies.Remove(enemy);
            CheckRoomCleared();
        }

        private void CheckRoomCleared()
        {
            if (!isCleared && activeEnemies.Count == 0)
            {
                isCleared = true;
                OnRoomCleared();
            }
        }

        private void OnRoomCleared()
        {
            // Уведомляем генератор о том, что комната очищена
            dungeonGenerator.OnRoomCleared(roomNode);

            // Активируем все двери
            foreach (var door in doorPositions)
            {
                door.gameObject.SetActive(true);
            }
        }

        public void OnPlayerEnter()
        {
            // Здесь можно добавить логику при входе игрока в комнату
            // Например, закрыть двери, запустить анимацию и т.д.
        }

        public void OnPlayerExit()
        {
            // Здесь можно добавить логику при выходе игрока из комнаты
            // Например, уничтожить комнату, если она очищена
            if (isCleared)
            {
                Destroy(gameObject);
            }
        }
    }
} 