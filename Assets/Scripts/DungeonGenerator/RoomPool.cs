using System.Collections.Generic;
using UnityEngine;

namespace DungeonGenerator
{
    /// <summary>
    /// ScriptableObject that defines a pool of rooms for a specific floor/depth
    /// </summary>
    [CreateAssetMenu(fileName = "New Room Pool", menuName = "Dungeon Generator/Room Pool")]
    public class RoomPool : ScriptableObject
    {
        [Tooltip("Floor number or depth level this pool is for")]
        public int floorDepth = 1;
        
        [Tooltip("Name of this floor/depth (e.g., 'Tartarus', 'Asphodel', etc.)")]
        public string floorName;
        
        [Tooltip("Entry rooms for this floor (first room after descending from previous floor)")]
        public List<WeightedRoomEntry> entryRooms = new List<WeightedRoomEntry>();
        
        [Tooltip("Regular combat rooms for this floor")]
        public List<WeightedRoomEntry> combatRooms = new List<WeightedRoomEntry>();
        
        [Tooltip("Reward rooms for this floor (treasure, boons, etc.)")]
        public List<WeightedRoomEntry> rewardRooms = new List<WeightedRoomEntry>();
        
        [Tooltip("Shop rooms for this floor")]
        public List<WeightedRoomEntry> shopRooms = new List<WeightedRoomEntry>();
        
        [Tooltip("Special rooms for this floor (challenge, story, etc.)")]
        public List<WeightedRoomEntry> specialRooms = new List<WeightedRoomEntry>();
        
        [Tooltip("Boss rooms for this floor")]
        public List<WeightedRoomEntry> bossRooms = new List<WeightedRoomEntry>();
        
        [Tooltip("Exit rooms for this floor (final room before descending to next floor)")]
        public List<WeightedRoomEntry> exitRooms = new List<WeightedRoomEntry>();
        
        [Tooltip("Minimum number of rooms to complete before boss room becomes available")]
        public int minRoomsBeforeBoss = 10;
        
        [Tooltip("Maximum number of rooms in this floor (0 = unlimited)")]
        public int maxRoomsInFloor = 15;
        
        [Tooltip("Whether shop rooms have limited availability in this floor")]
        public bool limitedShops = true;
        
        [Tooltip("Maximum number of shop rooms in this floor (if limited)")]
        public int maxShopRooms = 2;
        
        [Tooltip("Whether special rooms have limited availability in this floor")]
        public bool limitedSpecialRooms = true;
        
        [Tooltip("Maximum number of special rooms in this floor (if limited)")]
        public int maxSpecialRooms = 3;
        
        /// <summary>
        /// Get a random room of a specific type from this pool
        /// </summary>
        public RoomData GetRandomRoom(RoomType roomType, List<string> excludedRoomIds = null)
        {
            List<WeightedRoomEntry> roomList = GetRoomListByType(roomType);
            if (roomList == null || roomList.Count == 0)
                return null;
            
            // Filter out excluded rooms
            List<WeightedRoomEntry> availableRooms = new List<WeightedRoomEntry>();
            foreach (var entry in roomList)
            {
                if (excludedRoomIds == null || !excludedRoomIds.Contains(entry.roomData.roomId))
                {
                    availableRooms.Add(entry);
                }
            }
            
            if (availableRooms.Count == 0)
                return null;
            
            // Calculate total weight
            float totalWeight = 0;
            foreach (var entry in availableRooms)
            {
                totalWeight += entry.weight;
            }
            
            // Select random room based on weight
            float randomValue = Random.Range(0, totalWeight);
            float currentWeight = 0;
            
            foreach (var entry in availableRooms)
            {
                currentWeight += entry.weight;
                if (randomValue <= currentWeight)
                {
                    return entry.roomData;
                }
            }
            
            // Fallback to first available room
            return availableRooms[0].roomData;
        }
        
        private List<WeightedRoomEntry> GetRoomListByType(RoomType roomType)
        {
            // Check room type's typeName or compare reference to determine the right list
            
            // If it's an entry room type
            if (roomType.typeName.ToLower().Contains("entry"))
                return entryRooms;
                
            // If it's a combat room type
            if (roomType.typeName.ToLower().Contains("combat"))
                return combatRooms;
                
            // If it's a reward room type
            if (roomType.typeName.ToLower().Contains("reward") || 
                roomType.typeName.ToLower().Contains("treasure"))
                return rewardRooms;
                
            // If it's a shop room type
            if (roomType.typeName.ToLower().Contains("shop"))
                return shopRooms;
                
            // If it's a special room type
            if (roomType.typeName.ToLower().Contains("special") || 
                roomType.typeName.ToLower().Contains("challenge"))
                return specialRooms;
                
            // If it's a boss room type
            if (roomType.typeName.ToLower().Contains("boss"))
                return bossRooms;
                
            // If it's an exit room type
            if (roomType.typeName.ToLower().Contains("exit"))
                return exitRooms;
                
            // Default to combat rooms
            return combatRooms;
        }
    }
    
    [System.Serializable]
    public class WeightedRoomEntry
    {
        public RoomData roomData;
        
        [Tooltip("Relative weight for this room (higher = more likely)")]
        [Range(0.1f, 10f)]
        public float weight = 1f;
    }
} 