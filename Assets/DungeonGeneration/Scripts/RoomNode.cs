using UnityEngine;
using System.Collections.Generic;
using DungeonGeneration.ScriptableObjects;

namespace DungeonGeneration
{
    public class RoomNode
    {
        public Vector2Int Position { get; private set; }
        public RoomTypeSO RoomType { get; private set; }
        public RoomTemplateSO RoomTemplate { get; private set; }
        public GameObject RoomInstance { get; set; }
        public List<RoomNode> ConnectedRooms { get; private set; }
        public bool IsCleared { get; set; }
        public string Id { get; set; }

        public RoomNode(Vector2Int position, RoomTypeSO roomType, RoomTemplateSO roomTemplate)
        {
            Position = position;
            RoomType = roomType;
            RoomTemplate = roomTemplate;
            ConnectedRooms = new List<RoomNode>();
            IsCleared = false;
            Id = System.Guid.NewGuid().ToString();
        }

        public void AddConnection(RoomNode other)
        {
            if (!ConnectedRooms.Contains(other))
            {
                ConnectedRooms.Add(other);
            }
        }

        public void RemoveConnection(RoomNode other)
        {
            ConnectedRooms.Remove(other);
        }

        public bool HasConnection(RoomNode other)
        {
            return ConnectedRooms.Contains(other);
        }
    }
} 