using UnityEngine;
using System.Collections.Generic;

namespace DungeonGeneration.ScriptableObjects
{
    [CreateAssetMenu(fileName = "New Dungeon Config", menuName = "Dungeon Generation/Dungeon Config")]
    public class DungeonConfigSO : ScriptableObject
    {
        [Header("Room Templates")]
        public RoomTemplateSO[] roomTemplates;

        [Header("Enemy Configs")]
        public EnemyConfigSO[] enemyConfigs;

        [Header("Dungeon Structure")]
        [Range(5, 20)]
        public int minRoomsPerLevel = 5;
        [Range(5, 20)]
        public int maxRoomsPerLevel = 10;
        public int totalLevels = 3;
        public float roomSize = 10f;
        public float roomSpacing = 12f;
        
        [Header("Room Distribution")]
        [Range(0, 1)]
        public float combatRoomChance = 0.6f;
        [Range(0, 1)]
        public float rewardRoomChance = 0.2f;
        [Range(0, 1)]
        public float shopRoomChance = 0.1f;
        
        [Header("Door Settings")]
        [Range(1, 3)]
        public int minDoorsPerRoom = 1;
        [Range(1, 3)]
        public int maxDoorsPerRoom = 3;
        
        [Header("Difficulty Scaling")]
        public AnimationCurve difficultyCurve = AnimationCurve.Linear(0, 1, 1, 2);
        [Range(1, 10)]
        public int startingDifficulty = 1;
        [Range(1, 10)]
        public int maxDifficulty = 10;
        
        [Header("Room Types")]
        public RoomTypeSO startRoomType;
        public RoomTypeSO combatRoomType;
        public RoomTypeSO rewardRoomType;
        public RoomTypeSO shopRoomType;
        public RoomTypeSO bossRoomType;
        
        [TextArea(3, 5)]
        public string dungeonDescription;
    }
} 