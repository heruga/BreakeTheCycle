using UnityEngine;
using System;

namespace DungeonGeneration.ScriptableObjects
{
    [CreateAssetMenu(fileName = "LootTable", menuName = "Dungeon/Loot Table")]
    public class LootTableSO : ScriptableObject
    {
        [System.Serializable]
        public class LootEntry
        {
            public RewardItemSO item;
            [Range(0f, 1f)]
            public float dropChance = 0.3f;
            [Range(1, 10)]
            public int minQuantity = 1;
            [Range(1, 10)]
            public int maxQuantity = 1;
        }

        [Header("Loot Settings")]
        public LootEntry[] possibleDrops;

        private void OnValidate()
        {
            if (possibleDrops != null)
            {
                foreach (var entry in possibleDrops)
                {
                    // Проверяем, что максимальное количество не меньше минимального
                    if (entry.maxQuantity < entry.minQuantity)
                    {
                        entry.maxQuantity = entry.minQuantity;
                        Debug.LogWarning($"Max quantity was less than min quantity in {name}. Adjusted to {entry.minQuantity}");
                    }
                }
            }
        }
    }
} 