using UnityEngine;
using System;

namespace DungeonGeneration.ScriptableObjects
{
    [CreateAssetMenu(fileName = "New Dungeon Config", menuName = "Dungeon/Dungeon Config")]
    public class DungeonConfigSO : ScriptableObject
    {
        [Header("Room Generation Settings")]
        [Range(3, 10)]
        public int minRooms = 5;
        [Range(3, 10)]
        public int maxRooms = 8;
        public float roomSpacing = 12f;
        
        [Header("Room Types")]
        public RoomTypeSO startRoomType;
        public RoomTypeSO basicCombatRoomType;
        public RoomTypeSO eliteCombatRoomType;
        public RoomTypeSO bossRoomType;
        
        [Header("Room Spawn Weights")]
        [Range(0f, 1f)]
        public float basicCombatRoomWeight = 0.7f;
        [Range(0f, 1f)]
        public float eliteCombatRoomWeight = 0.3f;
        
        [Header("Room Templates")]
        public RoomTemplateSO[] roomTemplates;
        
        [Header("Enemy Configurations")]
        public EnemyConfigSO[] enemyConfigs;
        
        [Header("Generation Settings")]
        public bool ensureBossRoom = true;
        public bool ensureStartRoom = true;
        public bool randomizeRoomOrder = true;

        private void OnValidate()
        {
            // Проверяем, что максимальное количество комнат не меньше минимального
            if (maxRooms < minRooms)
            {
                maxRooms = minRooms;
                Debug.LogWarning($"Max rooms was less than min rooms in {name}. Adjusted to {minRooms}");
            }

            // Проверяем наличие всех необходимых типов комнат
            if (startRoomType == null || basicCombatRoomType == null || 
                eliteCombatRoomType == null || bossRoomType == null)
            {
                Debug.LogError($"Missing required room types in {name}!");
            }

            // Проверяем, что все типы комнат правильно настроены
            if (startRoomType != null && !startRoomType.canBeFirst)
            {
                Debug.LogError($"Start room type in {name} is not configured to be first!");
            }
            if (bossRoomType != null && !bossRoomType.canBeLast)
            {
                Debug.LogError($"Boss room type in {name} is not configured to be last!");
            }

            // Проверяем, что сумма весов не превышает 1
            float totalWeight = basicCombatRoomWeight + eliteCombatRoomWeight;
            if (totalWeight > 1f)
            {
                Debug.LogWarning($"Total room spawn weights exceed 1 in {name}. Adjusted weights.");
                float ratio = 1f / totalWeight;
                basicCombatRoomWeight *= ratio;
                eliteCombatRoomWeight *= ratio;
            }
        }
    }
} 