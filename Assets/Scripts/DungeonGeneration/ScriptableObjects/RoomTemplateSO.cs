using UnityEngine;
using System.Collections.Generic;

namespace DungeonGeneration.ScriptableObjects
{
    [CreateAssetMenu(fileName = "RoomTemplate", menuName = "Dungeon/Room Template")]
    public class RoomTemplateSO : ScriptableObject
    {
        [Header("Room Template Settings")]
        public string templateName;
        public GameObject roomPrefab;
        
        [Header("Room Type Compatibility")]
        public List<RoomTypeSO> supportedRoomTypes = new List<RoomTypeSO>();
        
        [TextArea(3, 5)]
        public string description;

        private void OnValidate()
        {
            // Проверяем наличие префаба комнаты
            if (roomPrefab == null)
            {
                Debug.LogError($"Room prefab is missing in {name}!");
            }
        }
    }
} 