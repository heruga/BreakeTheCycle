using UnityEngine;

namespace DungeonGeneration.ScriptableObjects
{
    [CreateAssetMenu(fileName = "EnemyConfig", menuName = "Dungeon/Enemy Configuration")]
    public class EnemyConfigSO : ScriptableObject
    {
        [Header("Enemy Settings")]
        public string enemyName;
        public GameObject enemyPrefab;
        public Sprite enemyIcon;
        
        [Header("Difficulty Settings")]
        [Range(1, 10)]
        public int difficultyRating = 1; // How difficult this enemy is
        [Range(1, 10)]
        public int minDungeonLevel = 1; // Minimum dungeon level this enemy appears at
        [Range(0f, 1f)]
        public float spawnWeight = 1f; // Relative weight for random selection
        
        [Header("Spawn Settings")]
        [Range(1, 10)]
        public int minGroupSize = 1; // Minimum number to spawn together
        [Range(1, 10)]
        public int maxGroupSize = 3; // Maximum number to spawn together
        
        [Header("Reward Settings")]
        public RewardItemSO[] possibleDrops; // Items this enemy can drop
        [Range(0f, 1f)]
        public float dropChance = 0.3f; // Chance of dropping an item
        
        [TextArea(3, 5)]
        public string description;
    }
} 