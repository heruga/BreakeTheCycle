using System.Collections.Generic;
using UnityEngine;
// Remove addressable reference since it requires the package to be installed
// using UnityEngine.AddressableAssets;

namespace DungeonGenerator
{
    /// <summary>
    /// ScriptableObject that defines a specific room prefab and its properties
    /// </summary>
    [CreateAssetMenu(fileName = "New Room", menuName = "Dungeon Generator/Room Data")]
    public class RoomData : ScriptableObject
    {
        [Tooltip("Unique identifier for this room")]
        public string roomId;
        
        [Tooltip("Display name of this room")]
        public string roomName;
        
        [Tooltip("Description of this room")]
        [TextArea(2, 5)]
        public string description;
        
        [Tooltip("Type of room")]
        public RoomType roomType;
        
        [Tooltip("Prefab for this room (will be instantiated when entered)")]
        public GameObject roomPrefab;
        
        // Remove addressable reference field
        // [Tooltip("OR use Addressable Asset for better performance (leave empty to use roomPrefab)")]
        // public AssetReference addressableRoomReference;
        
        [Tooltip("Minimum number of doors this room should have")]
        [Range(1, 8)]
        public int minDoorCount = 1;
        
        [Tooltip("Maximum number of doors this room should have")]
        [Range(1, 8)]
        public int maxDoorCount = 3;
        
        [Tooltip("Possible room types that doors from this room can lead to")]
        public List<RoomTypeWeight> possibleNextRoomTypes = new List<RoomTypeWeight>();
        
        [Tooltip("Difficulty level of this room (higher value = more difficult)")]
        [Range(1, 10)]
        public int difficulty = 1;
        
        [Tooltip("Whether this room can only appear once per run")]
        public bool uniquePerRun = false;
        
        [Tooltip("Whether this is an entry room (first room of dungeon/floor)")]
        public bool isEntryRoom = false;
        
        [Tooltip("Whether this is an exit room (last room of dungeon/floor)")]
        public bool isExitRoom = false;
        
        [Tooltip("Minimum floor/depth where this room can appear")]
        public int minFloorDepth = 0;
        
        [Tooltip("Maximum floor/depth where this room can appear (0 = no limit)")]
        public int maxFloorDepth = 0;
    }
    
    [System.Serializable]
    public class RoomTypeWeight
    {
        public RoomType roomType;
        
        [Tooltip("Relative weight for this room type (higher = more likely)")]
        [Range(0.1f, 10f)]
        public float weight = 1f;
        
        [Tooltip("Maximum number of doors that can lead to this room type")]
        public int maxDoorsOfThisType = 1;
    }
} 