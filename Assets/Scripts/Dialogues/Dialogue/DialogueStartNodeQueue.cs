using System.Collections.Generic;
using UnityEngine;

namespace BreakTheCycle.Dialogue
{
    public class DialogueStartNodeQueue : MonoBehaviour
    {
        public static DialogueStartNodeQueue Instance { get; private set; }

        // Ключ — уникальный идентификатор NPC, значение — нужный стартовый узел
        private Dictionary<string, int> npcStartNodes = new Dictionary<string, int>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void SetStartNode(string npcId, int nodeId)
        {
            npcStartNodes[npcId] = nodeId;
            Debug.Log($"[DialogueStartNodeQueue] SetStartNode: NPC {npcId} -> Node {nodeId}");
        }

        public bool TryGetStartNode(string npcId, out int nodeId)
        {
            bool result = npcStartNodes.TryGetValue(npcId, out nodeId);
            Debug.Log($"[DialogueStartNodeQueue] TryGetStartNode: NPC {npcId} -> {(result ? nodeId.ToString() : "not found")}");
            return result;
        }

        public void RemoveStartNode(string npcId)
        {
            bool existed = npcStartNodes.Remove(npcId);
            Debug.Log($"[DialogueStartNodeQueue] RemoveStartNode: NPC {npcId} -> {(existed ? "removed" : "not found")}");
        }
    }
} 