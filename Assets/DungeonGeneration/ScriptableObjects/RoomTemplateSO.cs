using UnityEngine;

namespace DungeonGeneration.ScriptableObjects
{
    [CreateAssetMenu(fileName = "RoomTemplate", menuName = "Dungeon/Room Template")]
    public class RoomTemplateSO : ScriptableObject
    {
        [Header("Room Template Settings")]
        public string templateName;
        public GameObject roomPrefab; // The actual room prefab with colliders, decorations, etc.
        
        [Header("Door Locations")]
        public Transform[] possibleDoorPositions; // Positions where doors can be placed
        
        [Header("Spawn Locations")]
        public Transform[] enemySpawnPoints; // Points where enemies can spawn
        public Transform[] treasureSpawnPoints; // Points where treasures/rewards can spawn
        public Transform[] decorationSpawnPoints; // Points where decorations can spawn
        
        [Header("Visualization")]
        public Sprite roomPreview; // Preview image of the room for debugging or UI
        
        [Header("Room Difficulty")]
        [Range(1, 10)]
        public int difficultyRating = 1; // How difficult this room layout is
        
        [Header("Compatibility")]
        public RoomTypeSO[] compatibleRoomTypes; // Which room types can use this template
        
        [TextArea(3, 5)]
        public string description;
    }
} 