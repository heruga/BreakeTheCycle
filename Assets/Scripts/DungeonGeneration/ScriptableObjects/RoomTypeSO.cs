using UnityEngine;

namespace DungeonGeneration.ScriptableObjects
{
    [CreateAssetMenu(fileName = "New Room Type", menuName = "Dungeon/Room Type")]
    public class RoomTypeSO : ScriptableObject
    {
        [Header("Room Type Settings")]
        public string typeName = "New Room Type";
        public RoomType roomType;
        public bool canBeFirst = false;
        public bool canBeLast = false;
        public bool requiresCleaning = true;
        public string description;
        
        [Header("Spawn Settings")]
        public int minEnemies = 1;
        public int maxEnemies = 3;
        public float eliteEnemyChance = 0.2f;
        public float bossEnemyChance = 0f;

        private void OnValidate()
        {
            // Проверяем, что максимальное количество врагов не меньше минимального
            if (maxEnemies < minEnemies)
            {
                maxEnemies = minEnemies;
                Debug.LogWarning($"Max enemies was less than min enemies in {name}. Adjusted to {minEnemies}");
            }
        }
    }
} 