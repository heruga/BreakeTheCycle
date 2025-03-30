using UnityEngine;

namespace DungeonGenerator
{
    /// <summary>
    /// ScriptableObject that defines a type of room in the dungeon
    /// </summary>
    [CreateAssetMenu(fileName = "New Room Type", menuName = "Dungeon Generator/Room Type")]
    public class RoomType : ScriptableObject
    {
        [Tooltip("Name of this room type")]
        public string typeName;
        
        [Tooltip("Description of this room type")]
        [TextArea(3, 5)]
        public string description;
        
        [Tooltip("Icon to show on doors leading to this room type")]
        public Sprite doorIcon;
        
        [Tooltip("Color associated with this room type (for UI elements, door frames, etc.)")]
        public Color roomColor = Color.white;
        
        [Tooltip("Whether this room locks doors until cleared (e.g., combat rooms)")]
        public bool locksDoorsUntilCleared = false;
        
        [Tooltip("Whether this is a special room type (boss, shop, etc.) with limited appearance")]
        public bool isSpecialRoom = false;
        
        [Tooltip("Minimum floor/depth where this room can appear")]
        public int minFloorDepth = 0;
        
        [Tooltip("Maximum floor/depth where this room can appear (0 = no limit)")]
        public int maxFloorDepth = 0;
        
        [Tooltip("Relative probability of this room appearing (higher = more likely)")]
        [Range(0.1f, 10f)]
        public float spawnProbability = 1f;
    }
} 