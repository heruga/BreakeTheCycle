using System.Collections.Generic;
using UnityEngine;

namespace DungeonGenerator
{
    /// <summary>
    /// ScriptableObject that defines global settings for the dungeon generator
    /// </summary>
    [CreateAssetMenu(fileName = "Dungeon Configuration", menuName = "Dungeon Generator/Dungeon Configuration")]
    public class DungeonConfiguration : ScriptableObject
    {
        [Header("General Settings")]
        [Tooltip("Name of this dungeon configuration")]
        public string configName = "Default Dungeon";
        
        [Tooltip("Description of this dungeon configuration")]
        [TextArea(3, 5)]
        public string description;
        
        [Tooltip("Whether to use a seed for random generation")]
        public bool useRandomSeed = true;
        
        [Tooltip("Seed for random generation (used only if useRandomSeed is false)")]
        public int randomSeed = 0;
        
        [Header("Floor Settings")]
        [Tooltip("List of room pools for each floor of the dungeon")]
        public List<RoomPool> floorRoomPools = new List<RoomPool>();
        
        [Tooltip("Minimum number of floors to generate")]
        public int minFloors = 3;
        
        [Tooltip("Maximum number of floors to generate")]
        public int maxFloors = 5;
        
        [Header("Door Settings")]
        [Tooltip("Prefab for doors (if null, rooms should have their own doors)")]
        public GameObject defaultDoorPrefab;
        
        [Tooltip("Default transition type between rooms")]
        public TransitionType defaultTransitionType = TransitionType.Teleport;
        
        [Tooltip("Duration of transition animation or fade")]
        public float transitionDuration = 1.0f;
        
        [Header("Room Management")]
        [Tooltip("Number of rooms to keep loaded behind the player (memory vs. performance tradeoff)")]
        [Range(0, 5)]
        public int roomsToKeepLoaded = 1;
        
        [Tooltip("Whether to unload unused rooms to save memory")]
        public bool unloadUnusedRooms = true;
        
        [Tooltip("Time to wait before unloading a room (seconds)")]
        public float roomUnloadDelay = 5.0f;
        
        [Header("Visualization")]
        [Tooltip("Whether to show door indicators for each room type")]
        public bool showDoorIndicators = true;
        
        [Tooltip("Whether to show minimap")]
        public bool showMinimap = true;
        
        [Tooltip("Whether to show room name when entering")]
        public bool showRoomNames = true;
        
        [Tooltip("Duration to show room name (seconds)")]
        public float roomNameDisplayDuration = 3.0f;
        
        [Header("Reward Settings")]
        [Tooltip("Base chance for rare rewards (0-1)")]
        [Range(0, 1)]
        public float baseRareRewardChance = 0.15f;
        
        [Tooltip("Increase in rare reward chance per floor")]
        [Range(0, 0.5f)]
        public float rareRewardChanceIncreasePerFloor = 0.05f;
        
        [Tooltip("Whether to guarantee a shop before boss rooms")]
        public bool guaranteeShopBeforeBoss = true;
        
        [Tooltip("Whether to guarantee a healing room before boss rooms")]
        public bool guaranteeHealingBeforeBoss = false;
        
        [Tooltip("Whether to prevent the same room type from appearing multiple times in a row")]
        public bool preventConsecutiveSameRoomTypes = true;
        
        #if UNITY_EDITOR
        public void ValidateConfiguration()
        {
            // Simple validation to ensure configuration is complete
            if (floorRoomPools.Count == 0)
            {
                Debug.LogError($"Dungeon Configuration '{name}' has no floor room pools defined!");
            }
            
            foreach (var pool in floorRoomPools)
            {
                if (pool == null)
                {
                    Debug.LogError($"Dungeon Configuration '{name}' has a null room pool reference!");
                    continue;
                }
                
                if (pool.entryRooms.Count == 0)
                {
                    Debug.LogWarning($"Room Pool '{pool.name}' for floor {pool.floorDepth} has no entry rooms defined!");
                }
                
                if (pool.exitRooms.Count == 0 && pool.floorDepth < floorRoomPools.Count)
                {
                    Debug.LogWarning($"Room Pool '{pool.name}' for floor {pool.floorDepth} has no exit rooms defined!");
                }
                
                if (pool.bossRooms.Count == 0 && pool.floorDepth == floorRoomPools.Count)
                {
                    Debug.LogWarning($"Final Room Pool '{pool.name}' for floor {pool.floorDepth} has no boss rooms defined!");
                }
            }
        }
        #endif
    }
    
    public enum TransitionType
    {
        Teleport,
        FadeToBlack,
        CrossFade,
        Dissolve,
        CustomAnimation
    }
} 