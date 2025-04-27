using UnityEngine;
using System.Linq;

namespace DungeonGeneration.ScriptableObjects
{
    [CreateAssetMenu(fileName = "EnemyConfig", menuName = "Dungeon/Enemy Configuration")]
    public class EnemyConfigSO : ScriptableObject
    {
        [Header("Enemy Settings")]
        public string enemyName;
        public GameObject[] enemyPrefabs;
        
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
        
        [TextArea(3, 5)]
        public string description;

        private void OnValidate()
        {
            // Проверяем наличие хотя бы одного префаба врага
            if (enemyPrefabs == null || enemyPrefabs.Length == 0 || enemyPrefabs.Any(p => p == null))
            {
                Debug.LogError($"Enemy prefabs are missing or contain null in {name}!");
            }

            // Проверяем, что максимальный размер группы не меньше минимального
            if (maxGroupSize < minGroupSize)
            {
                maxGroupSize = minGroupSize;
                Debug.LogWarning($"Max group size was less than min group size in {name}. Adjusted to {minGroupSize}");
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