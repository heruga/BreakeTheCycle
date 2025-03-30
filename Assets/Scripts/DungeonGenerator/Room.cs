using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DungeonGenerator
{
    /// <summary>
    /// Component for managing a dungeon room
    /// </summary>
    public class Room : MonoBehaviour
    {
        [Header("Room Info")]
        [SerializeField] private string roomId;
        [SerializeField] private RoomData roomData;
        [SerializeField] private bool isCleared = false;
        
        [Header("Doors")]
        [SerializeField] private List<Door> doors = new List<Door>();
        [SerializeField] private Transform entryPoint;
        
        [Header("Enemies")]
        [SerializeField] private List<GameObject> enemySpawners = new List<GameObject>();
        [SerializeField] private List<GameObject> enemies = new List<GameObject>();
        [SerializeField] private int enemyCount = 0;
        
        [Header("Rewards")]
        [SerializeField] private List<Transform> rewardSpawnPoints = new List<Transform>();
        [SerializeField] private List<GameObject> rewards = new List<GameObject>();
        
        [Header("Room UI")]
        [SerializeField] private TextMeshProUGUI roomNameText;
        [SerializeField] private Image roomBackgroundImage;
        [SerializeField] private GameObject roomClearedUI;
        [SerializeField] private GameObject roomEntryUI;
        
        [Header("Events")]
        [SerializeField] private bool autoCloseDoorsOnEnter = true;
        [SerializeField] private bool autoOpenDoorsOnCleared = true;
        
        private bool isActive = false;
        private bool hasSpawnedEnemies = false;
        private bool hasActivatedRewards = false;
        
        public event Action<Room> OnRoomEntered;
        public event Action<Room> OnRoomCleared;
        public event Action<Room> OnRoomExited;
        
        public string RoomId => string.IsNullOrEmpty(roomId) ? name : roomId;
        public RoomData RoomData => roomData;
        public bool IsCleared => isCleared;
        public List<Door> Doors => doors;
        public bool IsActive => isActive;
        public Transform EntryPoint => entryPoint;
        
        private void Awake()
        {
            // Generate a unique room ID if not set
            if (string.IsNullOrEmpty(roomId))
            {
                roomId = GenerateRoomId();
            }
            
            // Find all doors if not set
            if (doors.Count == 0)
            {
                doors.AddRange(GetComponentsInChildren<Door>());
            }
            
            // Find entry point if not set
            if (entryPoint == null)
            {
                // Try to find a GameObject named "EntryPoint" or similar
                Transform entry = transform.Find("EntryPoint");
                if (entry == null) entry = transform.Find("PlayerSpawn");
                if (entry == null) entry = transform.Find("SpawnPoint");
                
                if (entry != null)
                {
                    entryPoint = entry;
                }
                else
                {
                    // Create a new entry point at the bottom of the room
                    GameObject entryObj = new GameObject("EntryPoint");
                    entryObj.transform.SetParent(transform);
                    entryObj.transform.localPosition = new Vector3(0, 0, -5); // Assuming -Z is "bottom" of room
                    entryPoint = entryObj.transform;
                }
            }
            
            // Set up room UI
            SetupRoomUI();
            
            // Count initial enemies
            CountEnemies();
        }
        
        private void OnEnable()
        {
            // Subscribe to enemy death events
            foreach (var enemy in enemies)
            {
                if (enemy != null)
                {
                    IEnemy enemyComponent = enemy.GetComponent<IEnemy>();
                    if (enemyComponent != null)
                    {
                        enemyComponent.OnEnemyDeath += HandleEnemyDeath;
                    }
                }
            }
        }
        
        private void OnDisable()
        {
            // Unsubscribe from enemy death events
            foreach (var enemy in enemies)
            {
                if (enemy != null)
                {
                    IEnemy enemyComponent = enemy.GetComponent<IEnemy>();
                    if (enemyComponent != null)
                    {
                        enemyComponent.OnEnemyDeath -= HandleEnemyDeath;
                    }
                }
            }
        }
        
        /// <summary>
        /// Initialize room with data and configuration
        /// </summary>
        public void Initialize(RoomData data, int floorDepth, bool generateDoors = true)
        {
            roomData = data;
            
            if (roomData != null)
            {
                // Set room name in UI
                if (roomNameText != null)
                {
                    roomNameText.text = roomData.roomName;
                }
                
                // Check if this room is pre-cleared (e.g., entry rooms, shops)
                if (roomData.roomType != null && !roomData.roomType.locksDoorsUntilCleared)
                {
                    isCleared = true;
                }
            }
            
            // Generate doors if needed
            if (generateDoors && roomData != null)
            {
                ConfigureDoors();
            }
            
            // Initialize room ID if not already set
            if (string.IsNullOrEmpty(roomId))
            {
                roomId = GenerateRoomId();
            }
        }
        
        /// <summary>
        /// Configure doors based on room data
        /// </summary>
        private void ConfigureDoors()
        {
            if (roomData == null) return;
            
            // Check if we have enough doors
            int doorCount = UnityEngine.Random.Range(roomData.minDoorCount, roomData.maxDoorCount + 1);
            
            // Create random door positions if we don't have enough
            if (doors.Count < doorCount && DungeonManager.Instance != null && 
                DungeonManager.Instance.Configuration.defaultDoorPrefab != null)
            {
                // Add more doors
                int doorsToAdd = doorCount - doors.Count;
                for (int i = 0; i < doorsToAdd; i++)
                {
                    // Calculate position around the perimeter
                    float angle = 360f * i / doorsToAdd;
                    Vector3 direction = Quaternion.Euler(0, angle, 0) * Vector3.forward;
                    
                    // Raycast to find walls
                    RaycastHit hit;
                    if (Physics.Raycast(transform.position, direction, out hit, 50f))
                    {
                        // Instantiate door at hit position
                        GameObject doorObj = Instantiate(
                            DungeonManager.Instance.Configuration.defaultDoorPrefab, 
                            hit.point + hit.normal * 0.1f, 
                            Quaternion.LookRotation(-hit.normal),
                            transform
                        );
                        
                        Door door = doorObj.GetComponent<Door>();
                        if (door != null)
                        {
                            doors.Add(door);
                        }
                    }
                }
            }
            
            // We need to calculate which room types each door will lead to
            if (roomData.possibleNextRoomTypes.Count > 0)
            {
                Dictionary<RoomType, int> doorTypeCount = new Dictionary<RoomType, int>();
                
                // Initialize tracking of door types
                foreach (var roomTypeWeight in roomData.possibleNextRoomTypes)
                {
                    doorTypeCount[roomTypeWeight.roomType] = 0;
                }
                
                // First, randomly assign room types to doors based on weights
                for (int i = 0; i < doors.Count; i++)
                {
                    Door door = doors[i];
                    if (door == null) continue;
                    
                    // Select a room type for this door based on weighted random selection
                    float totalWeight = 0;
                    foreach (var roomTypeWeight in roomData.possibleNextRoomTypes)
                    {
                        // Skip if we've reached the maximum doors for this type
                        if (doorTypeCount.ContainsKey(roomTypeWeight.roomType) && 
                            doorTypeCount[roomTypeWeight.roomType] >= roomTypeWeight.maxDoorsOfThisType)
                            continue;
                            
                        totalWeight += roomTypeWeight.weight;
                    }
                    
                    if (totalWeight <= 0)
                        break; // No more room types available
                        
                    float randomValue = UnityEngine.Random.Range(0, totalWeight);
                    float currentWeight = 0;
                    
                    RoomType selectedRoomType = null;
                    float selectedWeight = 1f;
                    
                    foreach (var roomTypeWeight in roomData.possibleNextRoomTypes)
                    {
                        // Skip if we've reached the maximum doors for this type
                        if (doorTypeCount.ContainsKey(roomTypeWeight.roomType) && 
                            doorTypeCount[roomTypeWeight.roomType] >= roomTypeWeight.maxDoorsOfThisType)
                            continue;
                            
                        currentWeight += roomTypeWeight.weight;
                        if (randomValue <= currentWeight)
                        {
                            selectedRoomType = roomTypeWeight.roomType;
                            selectedWeight = roomTypeWeight.weight;
                            break;
                        }
                    }
                    
                    if (selectedRoomType != null)
                    {
                        door.Setup(selectedRoomType, selectedWeight);
                        doorTypeCount[selectedRoomType]++;
                    }
                }
            }
            else
            {
                // Default door settings if no specific room types are defined
                foreach (Door door in doors)
                {
                    if (door != null)
                    {
                        // If no specific room types are defined, use a default combat room type
                        RoomType defaultType = DungeonManager.Instance?.GetDefaultRoomType();
                        door.Setup(defaultType);
                    }
                }
            }
        }
        
        /// <summary>
        /// Called when player enters this room
        /// </summary>
        public void OnEnterRoom()
        {
            isActive = true;
            
            // Show room entry UI
            if (roomEntryUI != null)
            {
                roomEntryUI.SetActive(true);
                StartCoroutine(HideUIAfterDelay(roomEntryUI, 3f));
            }
            
            // Spawn enemies if not already spawned
            if (!hasSpawnedEnemies && roomData != null && 
                roomData.roomType != null && roomData.roomType.locksDoorsUntilCleared)
            {
                SpawnEnemies();
                hasSpawnedEnemies = true;
                
                // Close doors if this is a combat room
                if (autoCloseDoorsOnEnter)
                {
                    foreach (Door door in doors)
                    {
                        if (door != null)
                        {
                            door.Lock();
                        }
                    }
                }
            }
            
            // Activate rewards if this is a reward room and not already activated
            if (!hasActivatedRewards && !roomData.roomType.locksDoorsUntilCleared)
            {
                ActivateRewards();
                hasActivatedRewards = true;
            }
            
            // Trigger the room entered event
            OnRoomEntered?.Invoke(this);
        }
        
        /// <summary>
        /// Called when player exits this room
        /// </summary>
        public void OnExitRoom()
        {
            isActive = false;
            
            // Trigger the room exited event
            OnRoomExited?.Invoke(this);
        }
        
        /// <summary>
        /// Mark room as cleared, unlock doors, and spawn rewards
        /// </summary>
        public void ClearRoom()
        {
            if (isCleared)
                return;
                
            isCleared = true;
            
            // Show room cleared UI
            if (roomClearedUI != null)
            {
                roomClearedUI.SetActive(true);
                StartCoroutine(HideUIAfterDelay(roomClearedUI, 3f));
            }
            
            // Unlock doors if auto-opening is enabled
            if (autoOpenDoorsOnCleared)
            {
                foreach (Door door in doors)
                {
                    if (door != null)
                    {
                        door.Unlock();
                    }
                }
            }
            
            // Activate rewards
            ActivateRewards();
            hasActivatedRewards = true;
            
            // Notify the dungeon manager
            DungeonManager.Instance?.RegisterRoomCleared(roomId);
            
            // Trigger the room cleared event
            OnRoomCleared?.Invoke(this);
        }
        
        /// <summary>
        /// Handle enemy death
        /// </summary>
        private void HandleEnemyDeath(GameObject enemy)
        {
            if (enemies.Contains(enemy))
            {
                enemies.Remove(enemy);
                enemyCount = enemies.Count;
                
                // If no more enemies and room is not cleared, clear it
                if (enemyCount <= 0 && !isCleared && 
                    roomData != null && roomData.roomType != null && 
                    roomData.roomType.locksDoorsUntilCleared)
                {
                    ClearRoom();
                }
            }
        }
        
        /// <summary>
        /// Count enemies in the room
        /// </summary>
        private void CountEnemies()
        {
            // Find enemies in child objects if not set
            if (enemies.Count == 0)
            {
                foreach (Transform child in transform)
                {
                    IEnemy enemy = child.GetComponent<IEnemy>();
                    if (enemy != null)
                    {
                        enemies.Add(child.gameObject);
                        enemy.OnEnemyDeath += HandleEnemyDeath;
                    }
                }
            }
            
            // Find enemy spawners
            if (enemySpawners.Count == 0)
            {
                foreach (Transform child in transform)
                {
                    // Only check if name contains "EnemySpawner" instead of also checking the tag
                    if (child.name.Contains("EnemySpawner"))
                    {
                        enemySpawners.Add(child.gameObject);
                    }
                }
            }
            
            enemyCount = enemies.Count;
        }
        
        /// <summary>
        /// Spawn enemies from spawners
        /// </summary>
        private void SpawnEnemies()
        {
            // Activate enemy spawners
            foreach (GameObject spawner in enemySpawners)
            {
                if (spawner == null) continue;
                
                IEnemySpawner spawnerComponent = spawner.GetComponent<IEnemySpawner>();
                if (spawnerComponent != null)
                {
                    GameObject[] spawnedEnemies = spawnerComponent.SpawnEnemies();
                    if (spawnedEnemies != null)
                    {
                        foreach (GameObject enemy in spawnedEnemies)
                        {
                            if (enemy != null)
                            {
                                enemies.Add(enemy);
                                
                                IEnemy enemyComponent = enemy.GetComponent<IEnemy>();
                                if (enemyComponent != null)
                                {
                                    enemyComponent.OnEnemyDeath += HandleEnemyDeath;
                                }
                            }
                        }
                    }
                }
            }
            
            // Update enemy count
            enemyCount = enemies.Count;
            
            // If no enemies were spawned and this is a combat room, automatically clear it
            if (enemyCount <= 0 && roomData != null && 
                roomData.roomType != null && roomData.roomType.locksDoorsUntilCleared)
            {
                ClearRoom();
            }
        }
        
        /// <summary>
        /// Activate rewards in the room
        /// </summary>
        private void ActivateRewards()
        {
            if (rewards.Count > 0)
            {
                // Activate existing rewards
                foreach (GameObject reward in rewards)
                {
                    if (reward != null)
                    {
                        reward.SetActive(true);
                    }
                }
            }
            else if (rewardSpawnPoints.Count > 0 && DungeonManager.Instance != null)
            {
                // Spawn rewards at spawn points
                foreach (Transform spawnPoint in rewardSpawnPoints)
                {
                    if (spawnPoint == null) continue;
                    
                    GameObject reward = DungeonManager.Instance.SpawnReward(roomData.roomType, roomData.difficulty);
                    if (reward != null)
                    {
                        reward.transform.position = spawnPoint.position;
                        reward.transform.rotation = spawnPoint.rotation;
                        reward.transform.SetParent(transform);
                        
                        rewards.Add(reward);
                    }
                }
            }
        }
        
        /// <summary>
        /// Set up room UI components
        /// </summary>
        private void SetupRoomUI()
        {
            // Find room name text if not set
            if (roomNameText == null)
            {
                // Check if there's a Canvas child with a TextMeshProUGUI component
                Canvas canvas = GetComponentInChildren<Canvas>();
                if (canvas != null)
                {
                    roomNameText = canvas.GetComponentInChildren<TextMeshProUGUI>();
                }
            }
            
            // Hide UI elements initially
            if (roomClearedUI != null)
            {
                roomClearedUI.SetActive(false);
            }
            
            if (roomEntryUI != null)
            {
                roomEntryUI.SetActive(false);
            }
        }
        
        /// <summary>
        /// Generate a unique room ID
        /// </summary>
        private string GenerateRoomId()
        {
            string randomId = Guid.NewGuid().ToString().Substring(0, 8);
            return $"room_{randomId}";
        }
        
        /// <summary>
        /// Coroutine to hide UI element after a delay
        /// </summary>
        private IEnumerator HideUIAfterDelay(GameObject uiElement, float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (uiElement != null)
            {
                uiElement.SetActive(false);
            }
        }
    }
    
    /// <summary>
    /// Interface for enemy characters
    /// </summary>
    public interface IEnemy
    {
        event Action<GameObject> OnEnemyDeath;
    }
    
    /// <summary>
    /// Interface for enemy spawners
    /// </summary>
    public interface IEnemySpawner
    {
        GameObject[] SpawnEnemies();
    }
} 