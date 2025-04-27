using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using DungeonGeneration.ScriptableObjects;

namespace DungeonGeneration.Scripts
{
    public class RoomNode
    {
        public Vector2Int Position { get; private set; }
        public RoomTypeSO RoomType { get; private set; }
        public string Id { get; private set; }
        public GameObject RoomInstance { get; private set; }
        public bool IsCleared { get; private set; }
        public bool WasVisited { get; private set; }

        public RoomNode(Vector2Int position, RoomTypeSO roomType, string id)
        {
            Position = position;
            RoomType = roomType;
            Id = id;
            IsCleared = false;
            WasVisited = false;
        }

        public void SetRoomInstance(GameObject instance)
        {
            RoomInstance = instance;
            if (instance != null)
            {
                WasVisited = true;
            }
        }

        public void SetRoomType(RoomTypeSO roomType)
        {
            RoomType = roomType;
        }

        public void ClearRoom()
        {
            IsCleared = true;
        }

        public void DestroyRoom()
        {
            if (RoomInstance != null)
            {
                Object.Destroy(RoomInstance);
                RoomInstance = null;
            }
        }
    }
} 