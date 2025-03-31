using UnityEngine;

namespace DungeonGeneration.ScriptableObjects
{
    [CreateAssetMenu(fileName = "RoomType", menuName = "Dungeon/Room Type")]
    public class RoomTypeSO : ScriptableObject
    {
        [Header("Room Type Settings")]
        public string typeName;
        public RoomType roomType;
        public Color roomColor = Color.white; // For UI and visualization
        public Sprite roomIcon;
        
        [Header("Spawn Settings")]
        public float spawnWeight = 1f; // Relative probability of spawning
        [Range(0, 3)]
        public int minDoorsCount = 1; // Minimum number of doors this room can have
        [Range(1, 3)]
        public int maxDoorsCount = 3; // Maximum number of doors this room can have
        
        [Header("Content Settings")]
        public GameObject[] possibleEnemyPrefabs; // Enemy prefabs that can spawn in this room
        public GameObject[] possibleRewardPrefabs; // Reward prefabs that can spawn in this room
        public GameObject[] decorationPrefabs; // Decoration prefabs
        
        [TextArea(3, 5)]
        public string description;
    }

    public enum RoomType
    {
        Start,
        Combat,
        Reward,
        Shop,
        Boss
    }
} 