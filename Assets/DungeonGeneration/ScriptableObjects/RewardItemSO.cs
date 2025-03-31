using UnityEngine;

namespace DungeonGeneration.ScriptableObjects
{
    [CreateAssetMenu(fileName = "RewardItem", menuName = "Dungeon/Reward Item")]
    public class RewardItemSO : ScriptableObject
    {
        [Header("Item Settings")]
        public string itemName;
        public GameObject itemPrefab;
        public Sprite itemIcon;
        
        [Header("Item Type")]
        public ItemType itemType;
        public int itemValue; // Value in coins or power level
        
        [Header("Rarity Settings")]
        public ItemRarity rarity;
        [Range(0f, 1f)]
        public float dropChance = 0.1f;
        
        [Header("Requirements")]
        public int minDungeonLevel = 1; // Minimum dungeon level required for this item to drop
        
        [TextArea(3, 5)]
        public string description;
    }
    
    public enum ItemType
    {
        Currency,
        Artifact,
        Consumable,
        UpgradeToken,
        Key
    }
    
    public enum ItemRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }
} 