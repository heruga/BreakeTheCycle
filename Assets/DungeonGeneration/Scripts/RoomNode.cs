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
        public List<RoomNode> ConnectedRooms { get; private set; }

        public RoomNode(Vector2Int position, RoomTypeSO roomType, string id)
        {
            Position = position;
            RoomType = roomType;
            Id = id;
            ConnectedRooms = new List<RoomNode>();
            IsCleared = false;
        }

        public void SetRoomInstance(GameObject instance)
        {
            RoomInstance = instance;
        }

        public void AddConnection(RoomNode room)
        {
            if (room != null && !ConnectedRooms.Contains(room))
            {
                ConnectedRooms.Add(room);
            }
        }

        public void RemoveConnection(RoomNode room)
        {
            if (room != null)
            {
                ConnectedRooms.Remove(room);
            }
        }

        public bool IsConnectedTo(RoomNode room)
        {
            return room != null && ConnectedRooms.Contains(room);
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