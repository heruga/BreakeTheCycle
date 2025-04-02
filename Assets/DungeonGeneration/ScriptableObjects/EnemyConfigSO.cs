using UnityEngine;

namespace DungeonGeneration.ScriptableObjects
{
    [CreateAssetMenu(fileName = "EnemyConfig", menuName = "Dungeon/Enemy Configuration")]
    public class EnemyConfigSO : ScriptableObject
    {
        [Header("Enemy Settings")]
        public string enemyName;
        public GameObject enemyPrefab;
        
        [Header("Enemy Type")]
        public bool isBoss = false;
        public bool isElite = false;
        
        [Header("Spawn Settings")]
        [Range(0f, 1f)]
        public float spawnWeight = 1f;
        [Range(1, 10)]
        public int minGroupSize = 1;
        [Range(1, 10)]
        public int maxGroupSize = 3;
        
        [Header("Loot Table")]
        public LootTableSO lootTable; // Reference to the enemy's loot table
        
        [TextArea(3, 5)]
        public string description;

        private void OnValidate()
        {
            // Проверяем наличие префаба врага
            if (enemyPrefab == null)
            {
                Debug.LogError($"Enemy prefab is missing in {name}!");
            }

            // Проверяем, что максимальный размер группы не меньше минимального
            if (maxGroupSize < minGroupSize)
            {
                maxGroupSize = minGroupSize;
                Debug.LogWarning($"Max group size was less than min group size in {name}. Adjusted to {minGroupSize}");
            }

            // Проверяем наличие таблицы дропа
            if (lootTable == null)
            {
                Debug.LogWarning($"No loot table assigned in {name}!");
            }

            // Проверяем, что босс не может быть элитным врагом
            if (isBoss && isElite)
            {
                isElite = false;
                Debug.LogWarning($"Boss cannot be elite in {name}. Elite flag was removed.");
            }
        }
    }
} 