using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace DungeonGenerator
{
    /// <summary>
    /// Main manager class for the dungeon generator
    /// </summary>
    public class DungeonManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private DungeonConfiguration dungeonConfiguration;
        
        [Header("Player")]
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private Transform playerSpawnPoint;
        
        [Header("UI")]
        [SerializeField] private Canvas uiCanvas;
        [SerializeField] private GameObject loadingScreen;
        [SerializeField] private Image loadingFillBar;
        [SerializeField] private TextMeshProUGUI loadingText;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private GameObject mapUI;
        
        [Header("Default Room Types")]
        [SerializeField] private RoomType defaultCombatRoomType;
        [SerializeField] private RoomType defaultRewardRoomType;
        [SerializeField] private RoomType defaultShopRoomType;
        [SerializeField] private RoomType defaultBossRoomType;
        
        [Header("Rewards")]
        [SerializeField] private List<GameObject> rewardPrefabs = new List<GameObject>();
        
        // Static instance
        private static DungeonManager _instance;
        public static DungeonManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    // Try to find an existing instance in the scene
                    _instance = FindObjectOfType<DungeonManager>();
                    
                    // If no instance exists, create a new one
                    if (_instance == null)
                    {
                        GameObject obj = new GameObject("DungeonManager");
                        _instance = obj.AddComponent<DungeonManager>();
                    }
                }
                return _instance;
            }
        }
        
        // State tracking
        private Dictionary<string, Room> loadedRooms = new Dictionary<string, Room>();
        private Dictionary<string, RoomData> generatedRoomData = new Dictionary<string, RoomData>();
        private Dictionary<string, string> doorToRoomMapping = new Dictionary<string, string>();
        private List<string> roomHistory = new List<string>();
        private List<string> clearedRooms = new List<string>();
        private List<RoomType> previousRoomTypes = new List<RoomType>();
        
        // Current state
        private Room currentRoom;
        private Door currentDoor;
        private int currentFloor = 1;
        private int roomsCompletedInFloor = 0;
        private int roomsCompletedTotal = 0;
        private int shopRoomsVisited = 0;
        private int specialRoomsVisited = 0;
        private bool isGeneratingRoom = false;
        private bool isTransitioning = false;
        private GameObject playerInstance;
        private bool isInitialized = false;
        
        // Events
        public event Action<string> OnRoomEntered;
        public event Action<string> OnRoomCleared;
        public event Action<string> OnRoomExited;
        public event Action<int> OnFloorChanged;
        public event Action<Door> OnPlayerApproachDoor;
        public event Action<Door> OnPlayerLeaveDoor;
        
        public DungeonConfiguration Configuration => dungeonConfiguration;
        public Room CurrentRoom => currentRoom;
        public Door CurrentDoor => currentDoor;
        public int CurrentFloor => currentFloor;
        public int TotalRoomsCompleted => roomsCompletedTotal;
        
        private void Awake()
        {
            // Ensure singleton behavior
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Initialize seed for random generation
            if (dungeonConfiguration != null && !dungeonConfiguration.useRandomSeed)
            {
                UnityEngine.Random.InitState(dungeonConfiguration.randomSeed);
            }
            
            SetupUI();
        }
        
        private void Start()
        {
            if (!isInitialized)
            {
                InitializeDungeon();
            }
        }
        
        private void SetupUI()
        {
            try
            {
                // Create UI canvas if not assigned
                if (uiCanvas == null)
                {
                    GameObject canvasObj = new GameObject("DungeonUICanvas");
                    uiCanvas = canvasObj.AddComponent<Canvas>();
                    uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    
                    // Add required canvas components
                    canvasObj.AddComponent<CanvasScaler>();
                    canvasObj.AddComponent<GraphicRaycaster>();
                    
                    // Set the canvas to persist between scenes
                    DontDestroyOnLoad(canvasObj);
                }
                
                // Create loading screen if not assigned
                if (loadingScreen == null)
                {
                    GameObject loadingObj = new GameObject("LoadingScreen");
                    loadingObj.transform.SetParent(uiCanvas.transform, false);
                    
                    // Set up background
                    Image bgImage = loadingObj.AddComponent<Image>();
                    if (bgImage != null)
                    {
                        bgImage.color = new Color(0, 0, 0, 0.8f);
                        
                        RectTransform bgRect = bgImage.rectTransform;
                        if (bgRect != null)
                        {
                            bgRect.anchorMin = Vector2.zero;
                            bgRect.anchorMax = Vector2.one;
                            bgRect.sizeDelta = Vector2.zero;
                        }
                    }
                    
                    // Create loading bar
                    GameObject barObj = new GameObject("LoadingBar");
                    barObj.transform.SetParent(loadingObj.transform, false);
                    
                    Image barBg = barObj.AddComponent<Image>();
                    if (barBg != null)
                    {
                        barBg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
                        
                        RectTransform barRect = barBg.rectTransform;
                        if (barRect != null)
                        {
                            barRect.anchorMin = new Vector2(0.2f, 0.45f);
                            barRect.anchorMax = new Vector2(0.8f, 0.55f);
                            barRect.sizeDelta = Vector2.zero;
                        }
                    }
                    
                    // Create fill bar
                    GameObject fillObj = new GameObject("FillBar");
                    fillObj.transform.SetParent(barObj.transform, false);
                    
                    loadingFillBar = fillObj.AddComponent<Image>();
                    if (loadingFillBar != null)
                    {
                        loadingFillBar.color = new Color(0.8f, 0.2f, 0.2f, 1f);
                        
                        RectTransform fillRect = loadingFillBar.rectTransform;
                        if (fillRect != null)
                        {
                            fillRect.anchorMin = Vector2.zero;
                            fillRect.anchorMax = Vector2.one;
                            fillRect.sizeDelta = Vector2.zero;
                            fillRect.anchoredPosition = Vector2.zero;
                        }
                    }
                    
                    // Instead of using TextMeshProUGUI directly, use a Unity Text component if TextMeshProUGUI fails
                    GameObject textObj = new GameObject("LoadingText");
                    textObj.transform.SetParent(loadingObj.transform, false);
                    
                    try
                    {
                        // Try to add TextMeshProUGUI first
                        loadingText = textObj.AddComponent<TextMeshProUGUI>();
                        if (loadingText != null)
                        {
                            loadingText.text = "Loading...";
                            loadingText.color = Color.white;
                            loadingText.alignment = TextAlignmentOptions.Center;
                            loadingText.fontSize = 36;
                            
                            RectTransform textRect = loadingText.rectTransform;
                            if (textRect != null)
                            {
                                textRect.anchorMin = new Vector2(0.2f, 0.6f);
                                textRect.anchorMax = new Vector2(0.8f, 0.7f);
                                textRect.sizeDelta = Vector2.zero;
                            }
                        }
                    }
                    catch (System.Exception)
                    {
                        // If TextMeshProUGUI fails, try regular Unity Text
                        Debug.LogWarning("TextMeshProUGUI not available, falling back to Unity Text");
                        Destroy(loadingText); // Clean up partial TMPro component if it exists
                        
                        UnityEngine.UI.Text unityText = textObj.AddComponent<UnityEngine.UI.Text>();
                        // We can't assign this to loadingText since it's a different type
                        // Instead, we'll just set it up and use it directly
                        if (unityText != null)
                        {
                            unityText.text = "Loading...";
                            unityText.color = Color.white;
                            unityText.alignment = TextAnchor.MiddleCenter;
                            unityText.fontSize = 24;
                            
                            RectTransform textRect = unityText.rectTransform;
                            if (textRect != null)
                            {
                                textRect.anchorMin = new Vector2(0.2f, 0.6f);
                                textRect.anchorMax = new Vector2(0.8f, 0.7f);
                                textRect.sizeDelta = Vector2.zero;
                            }
                        }
                    }
                    
                    loadingScreen = loadingObj;
                }
                
                // Create message text if not assigned
                if (messageText == null)
                {
                    GameObject messageObj = new GameObject("MessageText");
                    messageObj.transform.SetParent(uiCanvas.transform, false);
                    
                    try
                    {
                        // Try to add TextMeshProUGUI first
                        messageText = messageObj.AddComponent<TextMeshProUGUI>();
                        if (messageText != null)
                        {
                            messageText.text = "";
                            messageText.color = Color.white;
                            messageText.alignment = TextAlignmentOptions.Center;
                            messageText.fontSize = 28;
                            
                            RectTransform messageRect = messageText.rectTransform;
                            if (messageRect != null)
                            {
                                messageRect.anchorMin = new Vector2(0.2f, 0.8f);
                                messageRect.anchorMax = new Vector2(0.8f, 0.9f);
                                messageRect.sizeDelta = Vector2.zero;
                            }
                            
                            // Add background
                            Image messageBg = messageObj.AddComponent<Image>();
                            if (messageBg != null)
                            {
                                messageBg.color = new Color(0, 0, 0, 0.7f);
                                messageBg.transform.SetAsFirstSibling(); // Ensure text is rendered on top
                            }
                        }
                    }
                    catch (System.Exception)
                    {
                        // If TextMeshProUGUI fails, try regular Unity Text
                        Debug.LogWarning("TextMeshProUGUI not available, falling back to Unity Text");
                        Destroy(messageText); // Clean up partial TMPro component if it exists
                        
                        UnityEngine.UI.Text unityText = messageObj.AddComponent<UnityEngine.UI.Text>();
                        // We can't assign this to messageText since it's a different type
                        if (unityText != null)
                        {
                            unityText.text = "";
                            unityText.color = Color.white;
                            unityText.alignment = TextAnchor.MiddleCenter;
                            unityText.fontSize = 20;
                            
                            RectTransform messageRect = unityText.rectTransform;
                            if (messageRect != null)
                            {
                                messageRect.anchorMin = new Vector2(0.2f, 0.8f);
                                messageRect.anchorMax = new Vector2(0.8f, 0.9f);
                                messageRect.sizeDelta = Vector2.zero;
                            }
                            
                            // Add background
                            Image messageBg = messageObj.AddComponent<Image>();
                            if (messageBg != null)
                            {
                                messageBg.color = new Color(0, 0, 0, 0.7f);
                                messageBg.transform.SetAsFirstSibling(); // Ensure text is rendered on top
                            }
                        }
                    }
                    
                    messageObj.SetActive(false);
                }
                
                // Hide UI elements initially
                if (loadingScreen != null) loadingScreen.SetActive(false);
                if (mapUI != null) mapUI.SetActive(false);
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error setting up UI: " + e.Message + "\n" + e.StackTrace);
            }
        }
        
        /// <summary>
        /// Initialize the dungeon and start with the first room
        /// </summary>
        public void InitializeDungeon()
        {
            if (isInitialized) return;
            
            // Show loading screen
            ShowLoadingScreen("Initializing Dungeon...");
            
            // Clean up any existing state
            CleanupExistingRooms();
            
            // Validate configuration
            if (dungeonConfiguration == null)
            {
                Debug.LogError("No dungeon configuration assigned!");
                HideLoadingScreen();
                return;
            }
            
            // Initialize random seed if using random generation
            if (dungeonConfiguration.useRandomSeed)
            {
                UnityEngine.Random.InitState((int)System.DateTime.Now.Ticks);
            }
            else
            {
                UnityEngine.Random.InitState(dungeonConfiguration.randomSeed);
            }
            
            // Reset state
            currentFloor = 1;
            roomsCompletedInFloor = 0;
            roomsCompletedTotal = 0;
            shopRoomsVisited = 0;
            specialRoomsVisited = 0;
            roomHistory.Clear();
            clearedRooms.Clear();
            generatedRoomData.Clear();
            doorToRoomMapping.Clear();
            previousRoomTypes.Clear();
            
            // Generate and load the first room
            StartCoroutine(GenerateFirstRoom());
            
            isInitialized = true;
        }
        
        private IEnumerator GenerateFirstRoom()
        {
            isGeneratingRoom = true;
            
            // Get the room pool for the current floor
            RoomPool roomPool = GetRoomPoolForFloor(currentFloor);
            if (roomPool == null)
            {
                Debug.LogError($"No room pool found for floor {currentFloor}");
                isGeneratingRoom = false;
                HideLoadingScreen();
                yield break;
            }
            
            // Find an entry room
            RoomData entryRoomData = GetEntryRoomForFloor(roomPool);
            if (entryRoomData == null)
            {
                Debug.LogError($"No entry room found for floor {currentFloor}");
                isGeneratingRoom = false;
                HideLoadingScreen();
                yield break;
            }
            
            // Generate a unique ID for this room
            string roomId = GenerateRoomId();
            
            // Store the room data
            generatedRoomData[roomId] = entryRoomData;
            
            // Load the room
            yield return StartCoroutine(LoadRoom(roomId, null));
            
            // Spawn player if needed
            if (playerInstance == null && playerPrefab != null)
            {
                SpawnPlayer();
            }
            
            isGeneratingRoom = false;
            
            // Hide loading screen
            HideLoadingScreen();
        }
        
        /// <summary>
        /// Load a room by ID and optionally teleport the player to a specified position
        /// </summary>
        private IEnumerator LoadRoom(string roomId, Vector3? playerPosition = null)
        {
            if (string.IsNullOrEmpty(roomId) || !generatedRoomData.ContainsKey(roomId))
            {
                Debug.LogError($"Cannot load room with ID {roomId}: Room not found in generated data");
                yield break;
            }
            
            // Already loaded?
            if (loadedRooms.ContainsKey(roomId) && loadedRooms[roomId] != null)
            {
                // Room is already loaded, just activate it
                Room room = loadedRooms[roomId];
                ActivateRoom(room, playerPosition);
                yield break;
            }
            
            // Get the room data
            RoomData roomData = generatedRoomData[roomId];
            if (roomData == null)
            {
                Debug.LogError($"Cannot load room with ID {roomId}: Room data is null");
                yield break;
            }
            
            // Update loading UI
            UpdateLoadingProgress(0.1f, $"Loading {roomData.roomName}...");
            
            // Instantiate the room
            GameObject roomInstance = null;
            
            // Modified code to remove Addressables dependency
            if (roomData.roomPrefab != null)
            {
                roomInstance = Instantiate(roomData.roomPrefab);
                UpdateLoadingProgress(0.9f, $"Setting up {roomData.roomName}...");
                yield return new WaitForSeconds(0.1f); // Give a small delay to show progress
            }
            else
            {
                Debug.LogError($"Room {roomData.roomName} has no prefab reference!");
                yield break;
            }
            
            // Set up the room instance
            if (roomInstance != null)
            {
                roomInstance.name = $"Room_{roomId}";
                
                // Get or add Room component
                Room room = roomInstance.GetComponent<Room>();
                if (room == null)
                {
                    room = roomInstance.AddComponent<Room>();
                }
                
                // Initialize room
                room.Initialize(roomData, currentFloor);
                
                // Add to loaded rooms dictionary
                loadedRooms[roomId] = room;
                
                // Configure doors
                ConfigureRoomDoors(room);
                
                // Activate the room and teleport player if needed
                ActivateRoom(room, playerPosition);
                
                // Add to room history
                if (!roomHistory.Contains(roomId))
                {
                    roomHistory.Add(roomId);
                }
                
                yield return new WaitForSeconds(0.1f); // Small delay to ensure everything is set up
            }
            
            // Update loading UI
            UpdateLoadingProgress(1.0f, "Ready!");
        }
        
        /// <summary>
        /// Activate a room and teleport the player if needed
        /// </summary>
        private void ActivateRoom(Room room, Vector3? playerPosition = null)
        {
            if (room == null) return;
            
            // Deactivate current room first
            if (currentRoom != null && currentRoom != room)
            {
                currentRoom.OnExitRoom();
            }
            
            // Set as current room
            currentRoom = room;
            
            // Teleport player if position is specified
            if (playerPosition.HasValue && playerInstance != null)
            {
                // Find the teleport target (entry point) if no position is specified
                if (playerPosition.Value == Vector3.zero && room.EntryPoint != null)
                {
                    playerPosition = room.EntryPoint.position;
                }
                
                // Teleport player
                playerInstance.transform.position = playerPosition.Value;
            }
            
            // Trigger room enter event
            room.OnEnterRoom();
            OnRoomEntered?.Invoke(room.RoomId);
            
            // Show room name if enabled
            if (dungeonConfiguration.showRoomNames && messageText != null && !string.IsNullOrEmpty(room.RoomData.roomName))
            {
                ShowMessage(room.RoomData.roomName, dungeonConfiguration.roomNameDisplayDuration);
            }
            
            // Unload distant rooms
            UnloadDistantRooms();
        }
        
        /// <summary>
        /// Configure doors in a room
        /// </summary>
        private void ConfigureRoomDoors(Room room)
        {
            if (room == null || room.Doors == null) return;
            
            // Set up doors and register them
            foreach (Door door in room.Doors)
            {
                if (door == null) continue;
                
                // Check if this door already has a target room
                if (!string.IsNullOrEmpty(door.TargetRoomId) && doorToRoomMapping.ContainsKey(door.TargetRoomId))
                {
                    // Door already configured, skip
                    continue;
                }
                
                // Generate next room for this door
                string targetRoomId = GenerateRoomForDoor(door, room.RoomData);
                
                if (!string.IsNullOrEmpty(targetRoomId))
                {
                    // Set the target room for this door
                    door.SetTargetRoom(targetRoomId);
                    
                    // Register the door-to-room mapping
                    string doorId = $"{room.RoomId}_{door.GetInstanceID()}";
                    doorToRoomMapping[doorId] = targetRoomId;
                }
            }
        }
        
        /// <summary>
        /// Generate a room based on door type
        /// </summary>
        private string GenerateRoomForDoor(Door door, RoomData currentRoomData)
        {
            if (door == null || door.TargetRoomType == null) return null;
            
            // Get room pool for current floor
            RoomPool roomPool = GetRoomPoolForFloor(currentFloor);
            if (roomPool == null) return null;
            
            // Generate a unique ID for the new room
            string newRoomId = GenerateRoomId();
            
            // Get a list of excluded room IDs (rooms we've already visited)
            List<string> excludedRoomIds = new List<string>();
            foreach (var kvp in generatedRoomData)
            {
                if (kvp.Value.uniquePerRun)
                {
                    excludedRoomIds.Add(kvp.Key);
                }
            }
            
            // Check if this is a boss door (based on room count) and need to enforce a boss room
            bool forceBossRoom = (roomsCompletedInFloor >= roomPool.minRoomsBeforeBoss) &&
                                (door.TargetRoomType.typeName.ToLower().Contains("boss"));
            
            // Check if we need to enforce a shop before boss
            bool forceShopRoom = dungeonConfiguration.guaranteeShopBeforeBoss && 
                                (roomsCompletedInFloor >= roomPool.minRoomsBeforeBoss - 1) &&
                                shopRoomsVisited == 0 &&
                                door.TargetRoomType.typeName.ToLower().Contains("shop");
            
            // Select a room based on the door's target room type
            RoomData selectedRoomData = null;
            
            if (forceBossRoom)
            {
                // Get a boss room
                selectedRoomData = roomPool.GetRandomRoom(defaultBossRoomType ?? door.TargetRoomType, excludedRoomIds);
            }
            else if (forceShopRoom)
            {
                // Get a shop room
                selectedRoomData = roomPool.GetRandomRoom(defaultShopRoomType ?? door.TargetRoomType, excludedRoomIds);
            }
            else
            {
                // Get a random room of the specified type
                selectedRoomData = roomPool.GetRandomRoom(door.TargetRoomType, excludedRoomIds);
            }
            
            // If no room found, use a default combat room
            if (selectedRoomData == null)
            {
                selectedRoomData = roomPool.GetRandomRoom(defaultCombatRoomType, excludedRoomIds);
                
                if (selectedRoomData == null)
                {
                    Debug.LogError($"Could not find any valid room for door of type {door.TargetRoomType.typeName}");
                    return null;
                }
            }
            
            // Store the selected room data
            generatedRoomData[newRoomId] = selectedRoomData;
            
            return newRoomId;
        }
        
        /// <summary>
        /// Transition to a new room when the player goes through a door
        /// </summary>
        public void TransitionToRoom(string roomId, Vector3 playerPosition)
        {
            if (string.IsNullOrEmpty(roomId) || isTransitioning) return;
            
            StartCoroutine(TransitionToRoomCoroutine(roomId, playerPosition));
        }
        
        private IEnumerator TransitionToRoomCoroutine(string roomId, Vector3 playerPosition)
        {
            isTransitioning = true;
            
            // Show transition effect based on configuration
            yield return StartCoroutine(ShowTransition());
            
            // Attempt to load the room if not already loaded
            if (!loadedRooms.ContainsKey(roomId) || loadedRooms[roomId] == null)
            {
                yield return StartCoroutine(LoadRoom(roomId, playerPosition));
            }
            else
            {
                // Room is already loaded, just activate it
                ActivateRoom(loadedRooms[roomId], playerPosition);
            }
            
            // Hide transition effect
            yield return StartCoroutine(HideTransition());
            
            isTransitioning = false;
        }
        
        private IEnumerator ShowTransition()
        {
            // Different transition effects based on configuration
            switch (dungeonConfiguration.defaultTransitionType)
            {
                case TransitionType.FadeToBlack:
                    ShowLoadingScreen("Transitioning...");
                    yield return new WaitForSeconds(dungeonConfiguration.transitionDuration * 0.5f);
                    break;
                    
                case TransitionType.CrossFade:
                    // Implement cross fade transition here
                    yield return new WaitForSeconds(dungeonConfiguration.transitionDuration * 0.3f);
                    break;
                    
                case TransitionType.Dissolve:
                    // Implement dissolve transition here
                    yield return new WaitForSeconds(dungeonConfiguration.transitionDuration * 0.3f);
                    break;
                    
                case TransitionType.CustomAnimation:
                    // Implement custom animation transition here
                    yield return new WaitForSeconds(dungeonConfiguration.transitionDuration * 0.5f);
                    break;
                    
                case TransitionType.Teleport:
                default:
                    // Instant transition
                    yield return null;
                    break;
            }
        }
        
        private IEnumerator HideTransition()
        {
            // Different transition effects based on configuration
            switch (dungeonConfiguration.defaultTransitionType)
            {
                case TransitionType.FadeToBlack:
                    yield return new WaitForSeconds(dungeonConfiguration.transitionDuration * 0.2f);
                    HideLoadingScreen();
                    break;
                    
                case TransitionType.CrossFade:
                    // Implement cross fade transition here
                    yield return new WaitForSeconds(dungeonConfiguration.transitionDuration * 0.3f);
                    break;
                    
                case TransitionType.Dissolve:
                    // Implement dissolve transition here
                    yield return new WaitForSeconds(dungeonConfiguration.transitionDuration * 0.3f);
                    break;
                    
                case TransitionType.CustomAnimation:
                    // Implement custom animation transition here
                    yield return new WaitForSeconds(dungeonConfiguration.transitionDuration * 0.3f);
                    break;
                    
                case TransitionType.Teleport:
                default:
                    // Instant transition
                    yield return null;
                    break;
            }
        }
        
        /// <summary>
        /// Unload rooms that are far from the current room
        /// </summary>
        private void UnloadDistantRooms()
        {
            if (!dungeonConfiguration.unloadUnusedRooms || currentRoom == null)
                return;
                
            // Get the rooms to keep loaded (current room and recent history)
            List<string> roomsToKeep = new List<string> { currentRoom.RoomId };
            
            // Add rooms from recent history based on configuration
            int historyCount = Mathf.Min(dungeonConfiguration.roomsToKeepLoaded, roomHistory.Count);
            for (int i = roomHistory.Count - 1; i >= roomHistory.Count - historyCount && i >= 0; i--)
            {
                roomsToKeep.Add(roomHistory[i]);
            }
            
            // Unload rooms that are not in the keep list
            List<string> roomsToUnload = new List<string>();
            foreach (var kvp in loadedRooms)
            {
                if (!roomsToKeep.Contains(kvp.Key))
                {
                    roomsToUnload.Add(kvp.Key);
                }
            }
            
            // Start unloading coroutine for each room
            foreach (string roomId in roomsToUnload)
            {
                StartCoroutine(UnloadRoomAfterDelay(roomId, dungeonConfiguration.roomUnloadDelay));
            }
        }
        
        /// <summary>
        /// Unload a room after a delay
        /// </summary>
        private IEnumerator UnloadRoomAfterDelay(string roomId, float delay)
        {
            yield return new WaitForSeconds(delay);
            
            // Check if the room is still valid to unload (not the current room)
            if (currentRoom != null && currentRoom.RoomId == roomId)
                yield break;
                
            // Unload the room
            if (loadedRooms.ContainsKey(roomId) && loadedRooms[roomId] != null)
            {
                Room room = loadedRooms[roomId];
                
                // Destroy the room game object
                Destroy(room.gameObject);
                
                // Remove from loaded rooms dictionary
                loadedRooms.Remove(roomId);
                
                Debug.Log($"Unloaded room: {roomId}");
            }
        }
        
        /// <summary>
        /// Register a room as cleared
        /// </summary>
        public void RegisterRoomCleared(string roomId)
        {
            if (string.IsNullOrEmpty(roomId) || clearedRooms.Contains(roomId))
                return;
                
            clearedRooms.Add(roomId);
            
            // Update room counters
            roomsCompletedInFloor++;
            roomsCompletedTotal++;
            
            // Check room type for special counters
            if (loadedRooms.ContainsKey(roomId) && loadedRooms[roomId] != null)
            {
                Room room = loadedRooms[roomId];
                if (room.RoomData != null && room.RoomData.roomType != null)
                {
                    string roomTypeName = room.RoomData.roomType.typeName.ToLower();
                    
                    // Update shop counter
                    if (roomTypeName.Contains("shop"))
                    {
                        shopRoomsVisited++;
                    }
                    
                    // Update special room counter
                    if (roomTypeName.Contains("special") || roomTypeName.Contains("challenge"))
                    {
                        specialRoomsVisited++;
                    }
                    
                    // Track room type history
                    previousRoomTypes.Add(room.RoomData.roomType);
                    if (previousRoomTypes.Count > 3)
                    {
                        previousRoomTypes.RemoveAt(0);
                    }
                    
                    // Check if this is an exit room
                    if (room.RoomData.isExitRoom)
                    {
                        // Prepare for next floor
                        PrepareNextFloor();
                    }
                }
            }
            
            // Trigger room cleared event
            OnRoomCleared?.Invoke(roomId);
        }
        
        /// <summary>
        /// Prepare for the next floor (called when player reaches exit room)
        /// </summary>
        private void PrepareNextFloor()
        {
            currentFloor++;
            
            // Reset floor-specific counters
            roomsCompletedInFloor = 0;
            shopRoomsVisited = 0;
            specialRoomsVisited = 0;
            
            // Trigger floor changed event
            OnFloorChanged?.Invoke(currentFloor);
            
            // Show floor transition message
            RoomPool nextFloorPool = GetRoomPoolForFloor(currentFloor);
            if (nextFloorPool != null && !string.IsNullOrEmpty(nextFloorPool.floorName))
            {
                ShowMessage($"Entering {nextFloorPool.floorName}", 3f);
            }
            else
            {
                ShowMessage($"Entering Floor {currentFloor}", 3f);
            }
        }
        
        /// <summary>
        /// Set the current door when player approaches it
        /// </summary>
        public void SetCurrentDoor(Door door)
        {
            if (door == null) return;
            
            currentDoor = door;
            OnPlayerApproachDoor?.Invoke(door);
        }
        
        /// <summary>
        /// Clear the current door when player moves away
        /// </summary>
        public void ClearCurrentDoor(Door door)
        {
            if (currentDoor != door) return;
            
            OnPlayerLeaveDoor?.Invoke(door);
            currentDoor = null;
        }
        
        /// <summary>
        /// Spawn a reward based on room type and difficulty
        /// </summary>
        public GameObject SpawnReward(RoomType roomType, int difficulty)
        {
            if (roomType == null || rewardPrefabs.Count == 0)
                return null;
                
            // Calculate rarity chance based on floor depth
            float rareRewardChance = dungeonConfiguration.baseRareRewardChance +
                                    (currentFloor - 1) * dungeonConfiguration.rareRewardChanceIncreasePerFloor;
            
            // Filter rewards by room type and difficulty
            List<GameObject> appropriateRewards = new List<GameObject>();
            
            foreach (GameObject rewardPrefab in rewardPrefabs)
            {
                if (rewardPrefab == null) continue;
                
                Reward reward = rewardPrefab.GetComponent<Reward>();
                if (reward != null && 
                    (reward.ValidRoomTypes.Count == 0 || reward.ValidRoomTypes.Contains(roomType)) &&
                    difficulty >= reward.MinDifficulty &&
                    (difficulty <= reward.MaxDifficulty || reward.MaxDifficulty == 0))
                {
                    // Add the reward to the appropriate list based on rarity
                    if (reward.IsRare && UnityEngine.Random.value <= rareRewardChance)
                    {
                        appropriateRewards.Add(rewardPrefab);
                    }
                    else if (!reward.IsRare)
                    {
                        appropriateRewards.Add(rewardPrefab);
                    }
                }
            }
            
            // Select a random reward
            if (appropriateRewards.Count > 0)
            {
                int index = UnityEngine.Random.Range(0, appropriateRewards.Count);
                return Instantiate(appropriateRewards[index]);
            }
            
            return null;
        }
        
        /// <summary>
        /// Clean up existing rooms (used when restarting dungeon)
        /// </summary>
        private void CleanupExistingRooms()
        {
            // Destroy all loaded rooms
            foreach (var kvp in loadedRooms)
            {
                if (kvp.Value != null)
                {
                    Destroy(kvp.Value.gameObject);
                }
            }
            
            // Clear dictionaries and lists
            loadedRooms.Clear();
            generatedRoomData.Clear();
            doorToRoomMapping.Clear();
            roomHistory.Clear();
            clearedRooms.Clear();
            previousRoomTypes.Clear();
            
            // Reset current state
            currentRoom = null;
            currentDoor = null;
        }
        
        /// <summary>
        /// Spawn the player in the current room
        /// </summary>
        private void SpawnPlayer()
        {
            if (playerPrefab == null)
            {
                Debug.LogError("Player prefab not assigned!");
                return;
            }
            
            Vector3 spawnPosition = Vector3.zero;
            
            // Use the spawn point if assigned
            if (playerSpawnPoint != null)
            {
                spawnPosition = playerSpawnPoint.position;
            }
            else if (currentRoom != null && currentRoom.EntryPoint != null)
            {
                spawnPosition = currentRoom.EntryPoint.position;
            }
            
            // Instantiate the player
            playerInstance = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
            
            // Set player tag if not already set
            if (string.IsNullOrEmpty(playerInstance.tag) || playerInstance.tag != "Player")
            {
                playerInstance.tag = "Player";
            }
        }
        
        /// <summary>
        /// Get the room pool for a specific floor
        /// </summary>
        private RoomPool GetRoomPoolForFloor(int floor)
        {
            if (dungeonConfiguration == null || dungeonConfiguration.floorRoomPools.Count == 0)
                return null;
                
            // Clamp floor to the available pools
            int floorIndex = Mathf.Clamp(floor - 1, 0, dungeonConfiguration.floorRoomPools.Count - 1);
            
            return dungeonConfiguration.floorRoomPools[floorIndex];
        }
        
        /// <summary>
        /// Get an entry room for the current floor
        /// </summary>
        private RoomData GetEntryRoomForFloor(RoomPool roomPool)
        {
            if (roomPool == null)
                return null;
                
            // Find an entry room in the pool
            foreach (var entry in roomPool.entryRooms)
            {
                if (entry.roomData != null && entry.roomData.isEntryRoom)
                {
                    return entry.roomData;
                }
            }
            
            // If no entry room found, return the first room in the pool
            if (roomPool.entryRooms.Count > 0 && roomPool.entryRooms[0].roomData != null)
            {
                return roomPool.entryRooms[0].roomData;
            }
            
            return null;
        }
        
        /// <summary>
        /// Update loading screen progress
        /// </summary>
        private void UpdateLoadingProgress(float progress, string message = null)
        {
            if (loadingScreen == null)
                return;
                
            // Ensure loading screen is visible
            loadingScreen.SetActive(true);
            
            // Update fill bar
            if (loadingFillBar != null)
            {
                loadingFillBar.fillAmount = progress;
            }
            
            // Update message text
            if (loadingText != null && !string.IsNullOrEmpty(message))
            {
                loadingText.text = message;
            }
            else if (loadingScreen != null && !string.IsNullOrEmpty(message))
            {
                // Try to find regular Text component if TextMeshProUGUI is not available
                UnityEngine.UI.Text regularText = loadingScreen.GetComponentInChildren<UnityEngine.UI.Text>();
                if (regularText != null)
                {
                    regularText.text = message;
                }
            }
        }
        
        /// <summary>
        /// Show loading screen
        /// </summary>
        private void ShowLoadingScreen(string message = "Loading...")
        {
            if (loadingScreen == null)
                return;
                
            loadingScreen.SetActive(true);
            
            if (loadingText != null)
            {
                loadingText.text = message;
            }
            else 
            {
                // Try to find regular Text component if TextMeshProUGUI is not available
                UnityEngine.UI.Text regularText = loadingScreen.GetComponentInChildren<UnityEngine.UI.Text>();
                if (regularText != null)
                {
                    regularText.text = message;
                }
            }
            
            if (loadingFillBar != null)
            {
                loadingFillBar.fillAmount = 0f;
            }
        }
        
        /// <summary>
        /// Hide loading screen
        /// </summary>
        private void HideLoadingScreen()
        {
            if (loadingScreen == null)
                return;
                
            loadingScreen.SetActive(false);
        }
        
        /// <summary>
        /// Show a message to the player
        /// </summary>
        public void ShowMessage(string message, float duration = 2f)
        {
            StartCoroutine(ShowMessageCoroutine(message, duration));
        }
        
        private IEnumerator ShowMessageCoroutine(string message, float duration)
        {
            if (messageText == null)
            {
                // Try to find a GameObject with "MessageText" to show the message
                GameObject messageObj = GameObject.Find("MessageText");
                if (messageObj != null)
                {
                    // Try to get TextMeshProUGUI component
                    messageText = messageObj.GetComponent<TextMeshProUGUI>();
                    
                    // If not found, try to get regular Text component
                    if (messageText == null)
                    {
                        UnityEngine.UI.Text regularText = messageObj.GetComponent<UnityEngine.UI.Text>();
                        if (regularText != null)
                        {
                            regularText.text = message;
                            messageObj.SetActive(true);
                            
                            yield return new WaitForSeconds(duration);
                            
                            messageObj.SetActive(false);
                            yield break;
                        }
                    }
                }
                else
                {
                    Debug.LogWarning("Cannot show message: MessageText component not found");
                    yield break;
                }
            }
            
            if (messageText != null && messageText.transform.parent != null)
            {
                messageText.text = message;
                messageText.transform.parent.gameObject.SetActive(true);
                
                yield return new WaitForSeconds(duration);
                
                messageText.transform.parent.gameObject.SetActive(false);
            }
        }
        
        /// <summary>
        /// Generate a unique room ID
        /// </summary>
        private string GenerateRoomId()
        {
            string roomId = Guid.NewGuid().ToString().Substring(0, 8);
            return $"room_{roomId}";
        }
        
        /// <summary>
        /// Get the default room type for a specific category
        /// </summary>
        public RoomType GetDefaultRoomType()
        {
            return defaultCombatRoomType;
        }
        
        /// <summary>
        /// Get the default room type for a specific category
        /// </summary>
        public RoomType GetDefaultRoomType(string category)
        {
            category = category.ToLower();
            
            if (category.Contains("combat"))
                return defaultCombatRoomType;
                
            if (category.Contains("reward") || category.Contains("treasure"))
                return defaultRewardRoomType;
                
            if (category.Contains("shop"))
                return defaultShopRoomType;
                
            if (category.Contains("boss"))
                return defaultBossRoomType;
                
            return defaultCombatRoomType;
        }
    }
    
    /// <summary>
    /// Base class for room rewards
    /// </summary>
    public class Reward : MonoBehaviour
    {
        [Tooltip("Whether this is a rare reward")]
        public bool IsRare = false;
        
        [Tooltip("Minimum difficulty level for this reward to appear")]
        public int MinDifficulty = 1;
        
        [Tooltip("Maximum difficulty level for this reward to appear (0 = no limit)")]
        public int MaxDifficulty = 0;
        
        [Tooltip("Room types where this reward can appear (empty = all rooms)")]
        public List<RoomType> ValidRoomTypes = new List<RoomType>();
    }
} 