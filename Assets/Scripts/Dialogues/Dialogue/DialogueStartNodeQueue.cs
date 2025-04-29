using System.Collections.Generic;
using UnityEngine;

namespace BreakTheCycle.Dialogue
{
    public class DialogueStartNodeQueue : MonoBehaviour
    {
        public static DialogueStartNodeQueue Instance { get; private set; }

        // Используем очередь для каждого NPC ID
        private Dictionary<string, Queue<int>> npcNodeQueues = new Dictionary<string, Queue<int>>();
        // --- ДОБАВЛЕНО: Хранилище для уже добавленных узлов (для гарантии уникальности) ---
        private Dictionary<string, HashSet<int>> alreadyEnqueuedNodes = new Dictionary<string, HashSet<int>>();

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

        // Добавляет ID узла в конец очереди для NPC (если его там еще не было)
        public void EnqueueStartNode(string npcId, int nodeId)
        {
            // --- ДОБАВЛЕНО: Инициализация структур, если NPC новый ---
            if (!npcNodeQueues.ContainsKey(npcId))
            {
                npcNodeQueues[npcId] = new Queue<int>();
            }
            if (!alreadyEnqueuedNodes.ContainsKey(npcId))
            {
                alreadyEnqueuedNodes[npcId] = new HashSet<int>();
            }

            // --- ДОБАВЛЕНО: Проверка, был ли узел добавлен ранее ---
            if (alreadyEnqueuedNodes[npcId].Contains(nodeId))
            {
                Debug.Log($"[DialogueStartNodeQueue] Node {nodeId} for NPC {npcId} was already enqueued before. Skipping.");
                return; // Не добавляем снова
            }

            // --- ДОБАВЛЕНО: Добавляем в HashSet и затем в Queue ---
            alreadyEnqueuedNodes[npcId].Add(nodeId);
            npcNodeQueues[npcId].Enqueue(nodeId);
            Debug.Log($"[DialogueStartNodeQueue] Enqueued: NPC {npcId} -> Node {nodeId} (Queue size: {npcNodeQueues[npcId].Count}, Total enqueued for this NPC: {alreadyEnqueuedNodes[npcId].Count})");
        }

        // Пытается извлечь и удалить следующий ID из очереди для NPC
        public bool TryDequeueNextStartNode(string npcId, out int nodeId)
        {
            nodeId = -1;
            if (npcNodeQueues.TryGetValue(npcId, out Queue<int> queue) && queue.Count > 0)
            {
                nodeId = queue.Dequeue();
                Debug.Log($"[DialogueStartNodeQueue] Dequeued: NPC {npcId} -> Node {nodeId} (Queue size left: {queue.Count})");
                return true;
            }
            Debug.Log($"[DialogueStartNodeQueue] TryDequeue: Queue empty or not found for NPC {npcId}");
            return false;
        }

        // Очищает очередь и историю добавленных узлов для конкретного NPC (опционально)
        public void ClearQueueForNpc(string npcId)
        {
            bool queueRemoved = npcNodeQueues.Remove(npcId);
            bool historyRemoved = alreadyEnqueuedNodes.Remove(npcId); // --- ДОБАВЛЕНО: Очистка истории

            if (queueRemoved || historyRemoved)
            {
                Debug.Log($"[DialogueStartNodeQueue] Queue and history cleared for NPC {npcId}");
            }
        }
    }
} 