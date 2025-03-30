using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine.UI;

namespace DungeonGenerator.Editor
{
    /// <summary>
    /// Custom editor window for setting up the Dungeon Generator
    /// </summary>
    public class DungeonGeneratorWindow : EditorWindow
    {
        // Tab selection
        private int selectedTab = 0;
        private readonly string[] tabNames = new string[] { "Setup", "Room Types", "Rooms", "Floors", "Doors" };
        
        // Configuration
        private DungeonConfiguration dungeonConfig;
        private SerializedObject serializedConfig;
        
        // Settings
        private GameObject doorPrefab;
        private TransitionType transitionType = TransitionType.FadeToBlack;
        private float transitionDuration = 1.0f;
        private bool unloadUnusedRooms = true;
        private int roomsToKeepLoaded = 1;
        
        // Room Types
        private List<RoomType> roomTypes = new List<RoomType>();
        private RoomType defaultCombatRoomType;
        private RoomType defaultRewardRoomType;
        private RoomType defaultShopRoomType;
        private RoomType defaultBossRoomType;
        
        // Room prefabs
        private List<RoomData> roomPrefabs = new List<RoomData>();
        
        // Floor settings
        private List<RoomPool> floorPools = new List<RoomPool>();
        
        // Search fields
        private string roomTypeSearchString = "";
        private string roomSearchString = "";
        private string floorSearchString = "";
        
        // Foldout states
        private bool showGeneralSettings = true;
        private bool showRoomTypeSettings = true;
        private bool showRoomSettings = true;
        private bool showFloorSettings = true;
        private bool showDoorSettings = true;
        
        // Scroll positions
        private Vector2 setupScrollPos;
        private Vector2 roomTypesScrollPos;
        private Vector2 roomsScrollPos;
        private Vector2 floorsScrollPos;
        private Vector2 doorsScrollPos;
        
        // Error tracking
        private string errorMessage = "";
        private float errorDisplayTime = 0f;
        
        [MenuItem("Tools/Dungeon Generator/Setup Window")]
        public static void ShowWindow()
        {
            var window = GetWindow<DungeonGeneratorWindow>();
            window.titleContent = new GUIContent("Dungeon Generator");
            window.minSize = new Vector2(500, 600);
            window.Show();
        }
        
        private void OnEnable()
        {
            // Try to find existing configuration
            string[] guids = AssetDatabase.FindAssets("t:DungeonConfiguration");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                dungeonConfig = AssetDatabase.LoadAssetAtPath<DungeonConfiguration>(path);
                
                if (dungeonConfig != null)
                {
                    serializedConfig = new SerializedObject(dungeonConfig);
                    LoadConfigToUI();
                }
            }
            
            // Initialize lists
            RefreshRoomTypesList();
            RefreshRoomDataList();
            RefreshRoomPoolsList();
        }
        
        private void OnGUI()
        {
            if (errorDisplayTime > 0)
            {
                DrawErrorMessage();
            }
            
            // Draw tabs
            selectedTab = GUILayout.Toolbar(selectedTab, tabNames);
            
            // Draw selected tab content
            switch (selectedTab)
            {
                case 0: // Setup
                    DrawSetupTab();
                    break;
                case 1: // Room Types
                    DrawRoomTypesTab();
                    break;
                case 2: // Rooms
                    DrawRoomsTab();
                    break;
                case 3: // Floors
                    DrawFloorsTab();
                    break;
                case 4: // Doors
                    DrawDoorsTab();
                    break;
            }
            
            // Draw bottom buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Save Configuration", GUILayout.Height(30)))
            {
                SaveConfiguration();
            }
            
            if (GUILayout.Button("Create Demo Scene", GUILayout.Height(30)))
            {
                CreateDemoScene();
            }
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawErrorMessage()
        {
            if (Event.current.type == EventType.Repaint)
            {
                errorDisplayTime -= Time.deltaTime;
            }
            
            if (!string.IsNullOrEmpty(errorMessage))
            {
                EditorGUILayout.HelpBox(errorMessage, MessageType.Error);
            }
        }
        
        private void DrawSetupTab()
        {
            // Begin scrollview properly
            setupScrollPos = EditorGUILayout.BeginScrollView(setupScrollPos);
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Dungeon Generator Setup", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            // Configuration asset
            EditorGUILayout.BeginHorizontal();
            dungeonConfig = (DungeonConfiguration)EditorGUILayout.ObjectField(
                "Configuration Asset", dungeonConfig, typeof(DungeonConfiguration), false);
                
            if (GUILayout.Button("Create New", GUILayout.Width(100)))
            {
                CreateNewConfiguration();
            }
            EditorGUILayout.EndHorizontal();
            
            // Update serializedConfig if dungeonConfig changes
            if (dungeonConfig != null && (serializedConfig == null || serializedConfig.targetObject != dungeonConfig))
            {
                serializedConfig = new SerializedObject(dungeonConfig);
            }
            
            // General settings
            showGeneralSettings = EditorGUILayout.Foldout(showGeneralSettings, "General Settings", true);
            if (showGeneralSettings)
            {
                EditorGUI.indentLevel++;
                
                // General dungeon settings
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Dungeon Settings", EditorStyles.boldLabel);
                
                if (serializedConfig != null)
                {
                    serializedConfig.Update();
                    
                    // Draw configuration name and description
                    SerializedProperty configNameProp = serializedConfig.FindProperty("configName");
                    SerializedProperty descriptionProp = serializedConfig.FindProperty("description");
                    SerializedProperty useRandomSeedProp = serializedConfig.FindProperty("useRandomSeed");
                    SerializedProperty randomSeedProp = serializedConfig.FindProperty("randomSeed");
                    
                    if (configNameProp != null) EditorGUILayout.PropertyField(configNameProp);
                    if (descriptionProp != null) EditorGUILayout.PropertyField(descriptionProp);
                    if (useRandomSeedProp != null) EditorGUILayout.PropertyField(useRandomSeedProp);
                    
                    if (useRandomSeedProp != null && !useRandomSeedProp.boolValue && randomSeedProp != null)
                    {
                        EditorGUILayout.PropertyField(randomSeedProp);
                    }
                    
                    // Door settings
                    SerializedProperty doorPrefabProp = serializedConfig.FindProperty("defaultDoorPrefab");
                    SerializedProperty transitionTypeProp = serializedConfig.FindProperty("defaultTransitionType");
                    SerializedProperty transitionDurationProp = serializedConfig.FindProperty("transitionDuration");
                    
                    if (doorPrefabProp != null) EditorGUILayout.PropertyField(doorPrefabProp);
                    if (transitionTypeProp != null) EditorGUILayout.PropertyField(transitionTypeProp);
                    if (transitionDurationProp != null) EditorGUILayout.PropertyField(transitionDurationProp);
                    
                    // Room management
                    SerializedProperty unloadRoomsProp = serializedConfig.FindProperty("unloadUnusedRooms");
                    SerializedProperty roomsToKeepProp = serializedConfig.FindProperty("roomsToKeepLoaded");
                    SerializedProperty roomUnloadDelayProp = serializedConfig.FindProperty("roomUnloadDelay");
                    
                    if (unloadRoomsProp != null) EditorGUILayout.PropertyField(unloadRoomsProp);
                    if (roomsToKeepProp != null) EditorGUILayout.PropertyField(roomsToKeepProp);
                    if (roomUnloadDelayProp != null) EditorGUILayout.PropertyField(roomUnloadDelayProp);
                    
                    // Visualization
                    SerializedProperty showDoorIndicatorsProp = serializedConfig.FindProperty("showDoorIndicators");
                    SerializedProperty showMinimapProp = serializedConfig.FindProperty("showMinimap");
                    SerializedProperty showRoomNamesProp = serializedConfig.FindProperty("showRoomNames");
                    SerializedProperty roomNameDisplayDurationProp = serializedConfig.FindProperty("roomNameDisplayDuration");
                    
                    if (showDoorIndicatorsProp != null) EditorGUILayout.PropertyField(showDoorIndicatorsProp);
                    if (showMinimapProp != null) EditorGUILayout.PropertyField(showMinimapProp);
                    if (showRoomNamesProp != null) EditorGUILayout.PropertyField(showRoomNamesProp);
                    if (roomNameDisplayDurationProp != null) EditorGUILayout.PropertyField(roomNameDisplayDurationProp);
                    
                    // Reward settings
                    SerializedProperty baseRareRewardChanceProp = serializedConfig.FindProperty("baseRareRewardChance");
                    SerializedProperty rareRewardChanceIncreasePerFloorProp = serializedConfig.FindProperty("rareRewardChanceIncreasePerFloor");
                    SerializedProperty guaranteeShopBeforeBossProp = serializedConfig.FindProperty("guaranteeShopBeforeBoss");
                    SerializedProperty guaranteeHealingBeforeBossProp = serializedConfig.FindProperty("guaranteeHealingBeforeBoss");
                    SerializedProperty preventConsecutiveSameRoomTypesProp = serializedConfig.FindProperty("preventConsecutiveSameRoomTypes");
                    
                    if (baseRareRewardChanceProp != null) EditorGUILayout.PropertyField(baseRareRewardChanceProp);
                    if (rareRewardChanceIncreasePerFloorProp != null) EditorGUILayout.PropertyField(rareRewardChanceIncreasePerFloorProp);
                    if (guaranteeShopBeforeBossProp != null) EditorGUILayout.PropertyField(guaranteeShopBeforeBossProp);
                    if (guaranteeHealingBeforeBossProp != null) EditorGUILayout.PropertyField(guaranteeHealingBeforeBossProp);
                    if (preventConsecutiveSameRoomTypesProp != null) EditorGUILayout.PropertyField(preventConsecutiveSameRoomTypesProp);
                    
                    serializedConfig.ApplyModifiedProperties();
                }
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Default Room Types", EditorStyles.boldLabel);
            
            if (serializedConfig != null)
            {
                serializedConfig.Update();
                
                SerializedProperty defaultCombatProp = serializedConfig.FindProperty("defaultCombatRoomType");
                SerializedProperty defaultRewardProp = serializedConfig.FindProperty("defaultRewardRoomType");
                SerializedProperty defaultShopProp = serializedConfig.FindProperty("defaultShopRoomType");
                SerializedProperty defaultBossProp = serializedConfig.FindProperty("defaultBossRoomType");
                
                if (defaultCombatProp != null) EditorGUILayout.PropertyField(defaultCombatProp);
                if (defaultRewardProp != null) EditorGUILayout.PropertyField(defaultRewardProp);
                if (defaultShopProp != null) EditorGUILayout.PropertyField(defaultShopProp);
                if (defaultBossProp != null) EditorGUILayout.PropertyField(defaultBossProp);
                
                serializedConfig.ApplyModifiedProperties();
            }
            
            EditorGUILayout.Space(10);
            if (GUILayout.Button("Find Assets in Project", GUILayout.Height(30)))
            {
                RefreshRoomTypesList();
                RefreshRoomDataList();
                RefreshRoomPoolsList();
            }
            
            // End scrollview properly
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawRoomTypesTab()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Room Types", EditorStyles.boldLabel);
            
            // Search field
            roomTypeSearchString = EditorGUILayout.TextField("Search", roomTypeSearchString);
            
            // Create new room type button
            if (GUILayout.Button("Create New Room Type", GUILayout.Height(30)))
            {
                CreateNewRoomType();
            }
            
            EditorGUILayout.Space(5);
            
            // Room types list
            roomTypesScrollPos = EditorGUILayout.BeginScrollView(roomTypesScrollPos);
            
            foreach (var roomType in roomTypes)
            {
                // Skip if doesn't match search
                if (!string.IsNullOrEmpty(roomTypeSearchString) && 
                    !roomType.name.ToLower().Contains(roomTypeSearchString.ToLower()) &&
                    !roomType.typeName.ToLower().Contains(roomTypeSearchString.ToLower()))
                    continue;
                
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                
                // Display room type info
                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField(roomType.typeName, EditorStyles.boldLabel);
                EditorGUILayout.LabelField(roomType.description, EditorStyles.wordWrappedLabel);
                EditorGUILayout.EndVertical();
                
                // Room type icon/color preview
                if (roomType.doorIcon != null)
                {
                    GUILayout.Box(roomType.doorIcon.texture, GUILayout.Width(40), GUILayout.Height(40));
                }
                else
                {
                    Rect colorRect = GUILayoutUtility.GetRect(40, 40);
                    EditorGUI.DrawRect(colorRect, roomType.roomColor);
                }
                
                // Edit button
                if (GUILayout.Button("Edit", GUILayout.Width(60), GUILayout.Height(40)))
                {
                    Selection.activeObject = roomType;
                    EditorUtility.FocusProjectWindow();
                }
                
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(5);
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawRoomsTab()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Room Prefabs", EditorStyles.boldLabel);
            
            // Search field
            roomSearchString = EditorGUILayout.TextField("Search", roomSearchString);
            
            // Create new room button
            if (GUILayout.Button("Create New Room Data", GUILayout.Height(30)))
            {
                CreateNewRoomData();
            }
            
            EditorGUILayout.Space(5);
            
            // Room prefabs list
            roomsScrollPos = EditorGUILayout.BeginScrollView(roomsScrollPos);
            
            foreach (var roomData in roomPrefabs)
            {
                // Skip if doesn't match search
                if (!string.IsNullOrEmpty(roomSearchString) && 
                    !roomData.name.ToLower().Contains(roomSearchString.ToLower()) &&
                    !roomData.roomName.ToLower().Contains(roomSearchString.ToLower()))
                    continue;
                
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                
                // Display room info
                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField(roomData.roomName, EditorStyles.boldLabel);
                
                if (roomData.roomType != null)
                {
                    EditorGUILayout.LabelField($"Type: {roomData.roomType.typeName}");
                }
                
                EditorGUILayout.LabelField($"Difficulty: {roomData.difficulty}");
                
                // Special flags
                string flags = "";
                if (roomData.isEntryRoom) flags += "Entry ";
                if (roomData.isExitRoom) flags += "Exit ";
                if (roomData.uniquePerRun) flags += "Unique ";
                
                if (!string.IsNullOrEmpty(flags))
                {
                    EditorGUILayout.LabelField($"Flags: {flags}");
                }
                
                EditorGUILayout.EndVertical();
                
                // Preview
                if (roomData.roomPrefab != null)
                {
                    var preview = AssetPreview.GetAssetPreview(roomData.roomPrefab);
                    if (preview != null)
                    {
                        GUILayout.Box(preview, GUILayout.Width(60), GUILayout.Height(60));
                    }
                    else
                    {
                        GUILayout.Box("No Preview", GUILayout.Width(60), GUILayout.Height(60));
                    }
                }
                else
                {
                    GUILayout.Box("No Prefab", GUILayout.Width(60), GUILayout.Height(60));
                }
                
                // Edit button
                if (GUILayout.Button("Edit", GUILayout.Width(60), GUILayout.Height(60)))
                {
                    Selection.activeObject = roomData;
                    EditorUtility.FocusProjectWindow();
                }
                
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(5);
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawFloorsTab()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Floor/Level Configuration", EditorStyles.boldLabel);
            
            // Search field
            floorSearchString = EditorGUILayout.TextField("Search", floorSearchString);
            
            // Create new floor pool button
            if (GUILayout.Button("Create New Floor Pool", GUILayout.Height(30)))
            {
                CreateNewRoomPool();
            }
            
            EditorGUILayout.Space(5);
            
            // Floor pools list
            floorsScrollPos = EditorGUILayout.BeginScrollView(floorsScrollPos);
            
            if (serializedConfig != null)
            {
                serializedConfig.Update();
                SerializedProperty floorPoolsProp = serializedConfig.FindProperty("floorRoomPools");
                
                if (floorPoolsProp != null)
                {
                    EditorGUILayout.PropertyField(floorPoolsProp);
                }
                
                serializedConfig.ApplyModifiedProperties();
            }
            
            EditorGUILayout.Space(10);
            
            // Display floor pools
            foreach (var floorPool in floorPools)
            {
                // Skip if doesn't match search
                if (!string.IsNullOrEmpty(floorSearchString) && 
                    !floorPool.name.ToLower().Contains(floorSearchString.ToLower()) &&
                    !floorPool.floorName.ToLower().Contains(floorSearchString.ToLower()))
                    continue;
                
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Floor {floorPool.floorDepth}: {floorPool.floorName}", EditorStyles.boldLabel);
                
                // Edit button
                if (GUILayout.Button("Edit", GUILayout.Width(60)))
                {
                    Selection.activeObject = floorPool;
                    EditorUtility.FocusProjectWindow();
                }
                EditorGUILayout.EndHorizontal();
                
                // Display room counts
                EditorGUILayout.LabelField($"Entry Rooms: {floorPool.entryRooms.Count}");
                EditorGUILayout.LabelField($"Combat Rooms: {floorPool.combatRooms.Count}");
                EditorGUILayout.LabelField($"Reward Rooms: {floorPool.rewardRooms.Count}");
                EditorGUILayout.LabelField($"Shop Rooms: {floorPool.shopRooms.Count}");
                
                if (floorPool.specialRooms.Count > 0)
                {
                    EditorGUILayout.LabelField($"Special Rooms: {floorPool.specialRooms.Count}");
                }
                
                EditorGUILayout.LabelField($"Boss Rooms: {floorPool.bossRooms.Count}");
                EditorGUILayout.LabelField($"Exit Rooms: {floorPool.exitRooms.Count}");
                
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(5);
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawDoorsTab()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Door Configuration", EditorStyles.boldLabel);
            
            EditorGUILayout.Space(5);
            
            doorsScrollPos = EditorGUILayout.BeginScrollView(doorsScrollPos);
            
            showDoorSettings = EditorGUILayout.Foldout(showDoorSettings, "Door Settings", true);
            if (showDoorSettings)
            {
                EditorGUI.indentLevel++;
                
                if (serializedConfig != null)
                {
                    serializedConfig.Update();
                    
                    // Door prefab
                    SerializedProperty doorPrefabProp = serializedConfig.FindProperty("defaultDoorPrefab");
                    if (doorPrefabProp != null) EditorGUILayout.PropertyField(doorPrefabProp);
                    
                    // Door transition type
                    SerializedProperty transitionTypeProp = serializedConfig.FindProperty("defaultTransitionType");
                    if (transitionTypeProp != null) EditorGUILayout.PropertyField(transitionTypeProp);
                    
                    // Door transition duration
                    SerializedProperty transitionDurationProp = serializedConfig.FindProperty("transitionDuration");
                    if (transitionDurationProp != null) EditorGUILayout.PropertyField(transitionDurationProp);
                    
                    // Door indicator settings
                    SerializedProperty showDoorIndicatorsProp = serializedConfig.FindProperty("showDoorIndicators");
                    if (showDoorIndicatorsProp != null) EditorGUILayout.PropertyField(showDoorIndicatorsProp);
                    
                    serializedConfig.ApplyModifiedProperties();
                }
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Create Door Prefab", EditorStyles.boldLabel);
            
            doorPrefab = (GameObject)EditorGUILayout.ObjectField("Base Door Prefab", doorPrefab, typeof(GameObject), false);
            
            if (GUILayout.Button("Create Door Prefab", GUILayout.Height(30)))
            {
                CreateDoorPrefab();
            }
            
            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox(
                "To create door prefabs for your game:\n" +
                "1. Create a base door 3D model or prefab\n" +
                "2. Assign it to the field above\n" +
                "3. Click 'Create Door Prefab' to generate a prefab with the Door component\n" +
                "4. Customize the door prefab as needed\n" +
                "5. Assign it to your Dungeon Configuration", 
                MessageType.Info);
            
            EditorGUILayout.EndScrollView();
        }
        
        #region Creation Methods
        
        private void CreateNewConfiguration()
        {
            // Create save folder if it doesn't exist
            string folderPath = "Assets/Resources/DungeonGenerator";
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                string[] folderParts = folderPath.Split('/');
                string currentPath = folderParts[0];
                
                for (int i = 1; i < folderParts.Length; i++)
                {
                    string newFolderPath = Path.Combine(currentPath, folderParts[i]);
                    if (!AssetDatabase.IsValidFolder(newFolderPath))
                    {
                        AssetDatabase.CreateFolder(currentPath, folderParts[i]);
                    }
                    currentPath = newFolderPath;
                }
            }
            
            // Create a new configuration asset
            DungeonConfiguration newConfig = ScriptableObject.CreateInstance<DungeonConfiguration>();
            newConfig.configName = "My Dungeon";
            newConfig.description = "A procedurally generated dungeon with Hades-like progression.";
            
            // Save the asset
            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{folderPath}/DungeonConfiguration.asset");
            AssetDatabase.CreateAsset(newConfig, assetPath);
            AssetDatabase.SaveAssets();
            
            // Assign to the window
            dungeonConfig = newConfig;
            serializedConfig = new SerializedObject(dungeonConfig);
            
            // Select the new asset
            Selection.activeObject = newConfig;
            EditorUtility.FocusProjectWindow();
        }
        
        private void CreateNewRoomType()
        {
            // Create save folder if it doesn't exist
            string folderPath = "Assets/Resources/DungeonGenerator/RoomTypes";
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                string parentFolder = Path.GetDirectoryName(folderPath);
                string folderName = Path.GetFileName(folderPath);
                AssetDatabase.CreateFolder(parentFolder, folderName);
            }
            
            // Create a new room type asset
            RoomType newRoomType = ScriptableObject.CreateInstance<RoomType>();
            newRoomType.typeName = "New Room Type";
            newRoomType.description = "Description of this room type.";
            newRoomType.roomColor = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
            
            // Save the asset
            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{folderPath}/RoomType_{newRoomType.typeName}.asset");
            AssetDatabase.CreateAsset(newRoomType, assetPath);
            AssetDatabase.SaveAssets();
            
            // Add to list and select the new asset
            roomTypes.Add(newRoomType);
            Selection.activeObject = newRoomType;
            EditorUtility.FocusProjectWindow();
        }
        
        private void CreateNewRoomData()
        {
            // Create save folder if it doesn't exist
            string folderPath = "Assets/Resources/DungeonGenerator/Rooms";
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                string parentFolder = Path.GetDirectoryName(folderPath);
                string folderName = Path.GetFileName(folderPath);
                AssetDatabase.CreateFolder(parentFolder, folderName);
            }
            
            // Create a new room data asset
            RoomData newRoomData = ScriptableObject.CreateInstance<RoomData>();
            newRoomData.roomName = "New Room";
            newRoomData.roomId = System.Guid.NewGuid().ToString().Substring(0, 8);
            newRoomData.description = "Description of this room.";
            newRoomData.minDoorCount = 1;
            newRoomData.maxDoorCount = 3;
            newRoomData.difficulty = 1;
            
            // Save the asset
            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{folderPath}/RoomData_{newRoomData.roomName}.asset");
            AssetDatabase.CreateAsset(newRoomData, assetPath);
            AssetDatabase.SaveAssets();
            
            // Add to list and select the new asset
            roomPrefabs.Add(newRoomData);
            Selection.activeObject = newRoomData;
            EditorUtility.FocusProjectWindow();
        }
        
        private void CreateNewRoomPool()
        {
            // Create save folder if it doesn't exist
            string folderPath = "Assets/Resources/DungeonGenerator/Floors";
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                string parentFolder = Path.GetDirectoryName(folderPath);
                string folderName = Path.GetFileName(folderPath);
                AssetDatabase.CreateFolder(parentFolder, folderName);
            }
            
            // Create a new room pool asset
            RoomPool newRoomPool = ScriptableObject.CreateInstance<RoomPool>();
            newRoomPool.floorDepth = floorPools.Count + 1;
            newRoomPool.floorName = $"Floor {newRoomPool.floorDepth}";
            newRoomPool.minRoomsBeforeBoss = 10;
            newRoomPool.maxRoomsInFloor = 15;
            
            // Save the asset
            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{folderPath}/RoomPool_{newRoomPool.floorName}.asset");
            AssetDatabase.CreateAsset(newRoomPool, assetPath);
            AssetDatabase.SaveAssets();
            
            // Add to list and select the new asset
            floorPools.Add(newRoomPool);
            
            // Add to configuration if it exists
            if (dungeonConfig != null && serializedConfig != null)
            {
                serializedConfig.Update();
                SerializedProperty floorPoolsProp = serializedConfig.FindProperty("floorRoomPools");
                floorPoolsProp.arraySize++;
                floorPoolsProp.GetArrayElementAtIndex(floorPoolsProp.arraySize - 1).objectReferenceValue = newRoomPool;
                serializedConfig.ApplyModifiedProperties();
            }
            
            Selection.activeObject = newRoomPool;
            EditorUtility.FocusProjectWindow();
        }
        
        private void CreateDoorPrefab()
        {
            if (doorPrefab == null)
            {
                ShowError("Please assign a base door prefab first!");
                return;
            }
            
            // Create save folder if it doesn't exist
            string folderPath = "Assets/Resources/DungeonGenerator/Prefabs";
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                string parentFolder = Path.GetDirectoryName(folderPath);
                string folderName = Path.GetFileName(folderPath);
                AssetDatabase.CreateFolder(parentFolder, folderName);
            }
            
            // Create a new door prefab instance
            GameObject doorInstance = PrefabUtility.InstantiatePrefab(doorPrefab) as GameObject;
            
            // Add Door component
            Door doorComponent = doorInstance.AddComponent<Door>();
            
            // Add collider for interactions if not present
            Collider collider = doorInstance.GetComponent<Collider>();
            if (collider == null)
            {
                BoxCollider boxCollider = doorInstance.AddComponent<BoxCollider>();
                boxCollider.isTrigger = true;
                boxCollider.center = Vector3.zero;
                boxCollider.size = new Vector3(2f, 3f, 0.5f);
            }
            else
            {
                collider.isTrigger = true;
            }
            
            // Create canvas for UI elements
            GameObject canvasObj = new GameObject("DoorCanvas");
            canvasObj.transform.SetParent(doorInstance.transform, false);
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(2f, 2f);
            canvasRect.localPosition = new Vector3(0f, 1.5f, 0f);
            canvasRect.localRotation = Quaternion.Euler(0f, 180f, 0f);
            canvasRect.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            
            // Add door icon image
            GameObject iconObj = new GameObject("DoorIcon");
            iconObj.transform.SetParent(canvasObj.transform, false);
            Image iconImage = iconObj.AddComponent<Image>();
            
            RectTransform iconRect = iconImage.rectTransform;
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(100f, 100f);
            iconRect.anchoredPosition = Vector2.zero;
            
            // Add door label
            GameObject labelObj = new GameObject("DoorLabel");
            labelObj.transform.SetParent(canvasObj.transform, false);
            TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
            labelText.text = "Door";
            labelText.fontSize = 24;
            labelText.alignment = TextAlignmentOptions.Center;
            
            RectTransform labelRect = labelText.rectTransform;
            labelRect.anchorMin = new Vector2(0.5f, 0f);
            labelRect.anchorMax = new Vector2(0.5f, 0f);
            labelRect.sizeDelta = new Vector2(160f, 30f);
            labelRect.anchoredPosition = new Vector2(0f, -80f);
            
            // Add door frame image
            GameObject frameObj = new GameObject("DoorFrame");
            frameObj.transform.SetParent(canvasObj.transform, false);
            Image frameImage = frameObj.AddComponent<Image>();
            frameImage.color = Color.white;
            
            RectTransform frameRect = frameImage.rectTransform;
            frameRect.anchorMin = Vector2.zero;
            frameRect.anchorMax = Vector2.one;
            frameRect.sizeDelta = new Vector2(20f, 20f);
            frameRect.anchoredPosition = Vector2.zero;
            
            // Make door frame appear behind icon
            frameObj.transform.SetAsFirstSibling();
            
            // Set references on door component
            doorComponent.GetType().GetField("doorIconImage", System.Reflection.BindingFlags.Instance | 
                System.Reflection.BindingFlags.NonPublic).SetValue(doorComponent, iconImage);
                
            doorComponent.GetType().GetField("doorLabel", System.Reflection.BindingFlags.Instance | 
                System.Reflection.BindingFlags.NonPublic).SetValue(doorComponent, labelText);
                
            doorComponent.GetType().GetField("doorFrame", System.Reflection.BindingFlags.Instance | 
                System.Reflection.BindingFlags.NonPublic).SetValue(doorComponent, frameImage);
            
            // Create locked visuals
            GameObject lockedObj = new GameObject("LockedVisuals");
            lockedObj.transform.SetParent(doorInstance.transform, false);
            
            // Add a simple lock indicator (can be customized later)
            GameObject lockIndicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lockIndicator.transform.SetParent(lockedObj.transform, false);
            lockIndicator.transform.localPosition = new Vector3(0f, 1f, 0f);
            lockIndicator.transform.localScale = new Vector3(0.3f, 0.5f, 0.1f);
            
            Material lockMaterial = new Material(Shader.Find("Standard"));
            lockMaterial.color = Color.red;
            lockIndicator.GetComponent<Renderer>().material = lockMaterial;
            
            // Create unlocked visuals
            GameObject unlockedObj = new GameObject("UnlockedVisuals");
            unlockedObj.transform.SetParent(doorInstance.transform, false);
            
            // Add a simple door indicator (can be customized later)
            GameObject doorIndicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
            doorIndicator.transform.SetParent(unlockedObj.transform, false);
            doorIndicator.transform.localPosition = new Vector3(0f, 1f, 0f);
            doorIndicator.transform.localScale = new Vector3(0.3f, 0.5f, 0.1f);
            
            Material doorMaterial = new Material(Shader.Find("Standard"));
            doorMaterial.color = Color.green;
            doorIndicator.GetComponent<Renderer>().material = doorMaterial;
            
            // Set references on door component
            doorComponent.GetType().GetField("lockedVisuals", System.Reflection.BindingFlags.Instance | 
                System.Reflection.BindingFlags.NonPublic).SetValue(doorComponent, lockedObj);
                
            doorComponent.GetType().GetField("unlockedVisuals", System.Reflection.BindingFlags.Instance | 
                System.Reflection.BindingFlags.NonPublic).SetValue(doorComponent, unlockedObj);
            
            // Create player spawn point
            GameObject spawnPoint = new GameObject("PlayerSpawnPoint");
            spawnPoint.transform.SetParent(doorInstance.transform, false);
            spawnPoint.transform.localPosition = new Vector3(0f, 0f, 1f);
            
            // Set reference on door component
            doorComponent.GetType().GetField("playerSpawnPoint", System.Reflection.BindingFlags.Instance | 
                System.Reflection.BindingFlags.NonPublic).SetValue(doorComponent, spawnPoint.transform);
            
            // Create the prefab
            string prefabPath = AssetDatabase.GenerateUniqueAssetPath($"{folderPath}/Door_Prefab.prefab");
            GameObject doorPrefabAsset = PrefabUtility.SaveAsPrefabAsset(doorInstance, prefabPath);
            
            // Cleanup
            DestroyImmediate(doorInstance);
            
            // Assign to configuration if it exists
            if (dungeonConfig != null && serializedConfig != null)
            {
                serializedConfig.Update();
                SerializedProperty doorPrefabProp = serializedConfig.FindProperty("defaultDoorPrefab");
                doorPrefabProp.objectReferenceValue = doorPrefabAsset;
                serializedConfig.ApplyModifiedProperties();
            }
            
            Selection.activeObject = doorPrefabAsset;
            EditorUtility.FocusProjectWindow();
        }
        
        private void CreateDemoScene()
        {
            if (dungeonConfig == null)
            {
                ShowError("Please create or assign a Dungeon Configuration first!");
                return;
            }
            
            // Create a new scene
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            
            // Create a DungeonManager GameObject
            GameObject managerObj = new GameObject("DungeonManager");
            DungeonManager manager = managerObj.AddComponent<DungeonManager>();
            
            // Assign the configuration
            manager.GetType().GetField("dungeonConfiguration", System.Reflection.BindingFlags.Instance | 
                System.Reflection.BindingFlags.NonPublic).SetValue(manager, dungeonConfig);
            
            // Create a player prefab if it doesn't exist
            GameObject playerPrefab = null;
            string[] guids = AssetDatabase.FindAssets("t:GameObject Player");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
            
            if (playerPrefab == null)
            {
                // Create a simple player prefab
                GameObject playerObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                playerObj.name = "Player";
                
                // Add Rigidbody
                Rigidbody rb = playerObj.AddComponent<Rigidbody>();
                rb.constraints = RigidbodyConstraints.FreezeRotation;
                
                // Add simple player controller
                playerObj.AddComponent<UnityEngine.AI.NavMeshAgent>();
                
                // Set the tag
                playerObj.tag = "Player";
                
                // Create a camera and make it a child of player
                GameObject cameraObj = new GameObject("PlayerCamera");
                cameraObj.transform.SetParent(playerObj.transform, false);
                cameraObj.transform.localPosition = new Vector3(0, 1, -5);
                cameraObj.transform.localRotation = Quaternion.Euler(20, 0, 0);
                
                Camera camera = cameraObj.AddComponent<Camera>();
                camera.nearClipPlane = 0.1f;
                camera.farClipPlane = 50f;
                
                // Save as prefab
                string folderPath = "Assets/Resources/DungeonGenerator/Prefabs";
                if (!AssetDatabase.IsValidFolder(folderPath))
                {
                    string parentFolder = Path.GetDirectoryName(folderPath);
                    string folderName = Path.GetFileName(folderPath);
                    AssetDatabase.CreateFolder(parentFolder, folderName);
                }
                
                string prefabPath = $"{folderPath}/Player.prefab";
                playerPrefab = PrefabUtility.SaveAsPrefabAsset(playerObj, prefabPath);
                
                // Clean up
                DestroyImmediate(playerObj);
            }
            
            // Assign the player prefab to the dungeon manager
            manager.GetType().GetField("playerPrefab", System.Reflection.BindingFlags.Instance | 
                System.Reflection.BindingFlags.NonPublic).SetValue(manager, playerPrefab);
            
            // Save the scene
            string scenePath = "Assets/Scenes/DungeonDemo.unity";
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), scenePath);
            
            // Refresh
            AssetDatabase.Refresh();
            
            Debug.Log($"Demo scene created at {scenePath}");
        }
        
        #endregion
        
        #region Utility Methods
        
        private void SaveConfiguration()
        {
            if (dungeonConfig == null)
            {
                ShowError("No configuration to save!");
                return;
            }
            
            EditorUtility.SetDirty(dungeonConfig);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log("Dungeon configuration saved!");
        }
        
        private void LoadConfigToUI()
        {
            if (dungeonConfig == null)
                return;
                
            // Load general settings
            doorPrefab = dungeonConfig.defaultDoorPrefab;
            transitionType = dungeonConfig.defaultTransitionType;
            transitionDuration = dungeonConfig.transitionDuration;
            unloadUnusedRooms = dungeonConfig.unloadUnusedRooms;
            roomsToKeepLoaded = dungeonConfig.roomsToKeepLoaded;
            
            // Load default room types
            defaultCombatRoomType = dungeonConfig.GetType().GetField("defaultCombatRoomType", System.Reflection.BindingFlags.Instance | 
                System.Reflection.BindingFlags.NonPublic).GetValue(dungeonConfig) as RoomType;
                
            defaultRewardRoomType = dungeonConfig.GetType().GetField("defaultRewardRoomType", System.Reflection.BindingFlags.Instance | 
                System.Reflection.BindingFlags.NonPublic).GetValue(dungeonConfig) as RoomType;
                
            defaultShopRoomType = dungeonConfig.GetType().GetField("defaultShopRoomType", System.Reflection.BindingFlags.Instance | 
                System.Reflection.BindingFlags.NonPublic).GetValue(dungeonConfig) as RoomType;
                
            defaultBossRoomType = dungeonConfig.GetType().GetField("defaultBossRoomType", System.Reflection.BindingFlags.Instance | 
                System.Reflection.BindingFlags.NonPublic).GetValue(dungeonConfig) as RoomType;
            
            // Load floor pools
            floorPools.Clear();
            floorPools.AddRange(dungeonConfig.floorRoomPools);
        }
        
        private void RefreshRoomTypesList()
        {
            string[] guids = AssetDatabase.FindAssets("t:RoomType");
            roomTypes.Clear();
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                RoomType roomType = AssetDatabase.LoadAssetAtPath<RoomType>(path);
                if (roomType != null)
                {
                    roomTypes.Add(roomType);
                }
            }
        }
        
        private void RefreshRoomDataList()
        {
            string[] guids = AssetDatabase.FindAssets("t:RoomData");
            roomPrefabs.Clear();
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                RoomData roomData = AssetDatabase.LoadAssetAtPath<RoomData>(path);
                if (roomData != null)
                {
                    roomPrefabs.Add(roomData);
                }
            }
        }
        
        private void RefreshRoomPoolsList()
        {
            string[] guids = AssetDatabase.FindAssets("t:RoomPool");
            floorPools.Clear();
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                RoomPool roomPool = AssetDatabase.LoadAssetAtPath<RoomPool>(path);
                if (roomPool != null)
                {
                    floorPools.Add(roomPool);
                }
            }
            
            // Sort by floor depth
            floorPools = floorPools.OrderBy(x => x.floorDepth).ToList();
        }
        
        private void ShowError(string message)
        {
            errorMessage = message;
            errorDisplayTime = 5f;
            Debug.LogError(message);
        }
        
        #endregion
    }
} 