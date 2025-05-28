using System.Collections.Generic;
using UnityEngine;
using System.Linq; // Для работы с ToArray и Join

namespace BreakTheCycle.Dialogue
{
    public class DialogueStartNodeQueue : MonoBehaviour
    {
        public static DialogueStartNodeQueue Instance { get; private set; }

        // Используем очередь для каждого NPC ID
        private Dictionary<string, Queue<int>> npcNodeQueues = new Dictionary<string, Queue<int>>();
        // Хранилище для уже добавленных узлов (для гарантии уникальности)
        private Dictionary<string, HashSet<int>> alreadyEnqueuedNodes = new Dictionary<string, HashSet<int>>();
        
        // Префикс для ключей PlayerPrefs
        private const string PREFS_PREFIX = "DialogueEnqueued_";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Загружаем информацию о ранее добавленных узлах
            LoadEnqueuedNodesFromPrefs();
        }
        
        // Загружает информацию о ранее добавленных узлах из PlayerPrefs
        private void LoadEnqueuedNodesFromPrefs()
        {
            // Получаем все ключи, начинающиеся с префикса
            List<string> relevantKeys = new List<string>();
            foreach (string key in PlayerPrefs.GetString("SavedDialogueKeys", "").Split(','))
            {
                if (!string.IsNullOrEmpty(key) && key.StartsWith(PREFS_PREFIX))
                {
                    relevantKeys.Add(key);
                }
            }
            
            // Загружаем узлы для каждого NPC
            foreach (string key in relevantKeys)
            {
                string npcId = key.Substring(PREFS_PREFIX.Length);
                string savedNodes = PlayerPrefs.GetString(key, "");
                
                if (!string.IsNullOrEmpty(savedNodes))
                {
                    string[] nodeIdsStr = savedNodes.Split(',');
                    HashSet<int> nodeIds = new HashSet<int>();
                    
                    foreach (string nodeIdStr in nodeIdsStr)
                    {
                        if (int.TryParse(nodeIdStr, out int nodeId))
                        {
                            nodeIds.Add(nodeId);
                        }
                    }
                    
                    // Сохраняем загруженные узлы
                    if (nodeIds.Count > 0)
                    {
                        alreadyEnqueuedNodes[npcId] = nodeIds;
                        Debug.Log($"[DialogueStartNodeQueue] Загружено {nodeIds.Count} узлов для NPC {npcId}: {string.Join(", ", nodeIds)}");
                    }
                }
            }
        }
        
        // Сохраняет информацию о добавленных узлах в PlayerPrefs
        public void SaveEnqueuedNodesToPrefs()
        {
            List<string> savedKeys = new List<string>();
            
            foreach (var pair in alreadyEnqueuedNodes)
            {
                string npcId = pair.Key;
                HashSet<int> nodeIds = pair.Value;
                
                if (nodeIds.Count > 0)
                {
                    string key = PREFS_PREFIX + npcId;
                    string value = string.Join(",", nodeIds);
                    
                    PlayerPrefs.SetString(key, value);
                    savedKeys.Add(key);
                    
                    Debug.Log($"[DialogueStartNodeQueue] Сохранено {nodeIds.Count} узлов для NPC {npcId}: {value}");
                }
            }
            
            // Сохраняем список ключей для последующей загрузки
            PlayerPrefs.SetString("SavedDialogueKeys", string.Join(",", savedKeys));
            PlayerPrefs.Save();
        }

        // Возвращает общее количество сохраненных ID диалогов
        public int GetTotalSavedNodesCount()
        {
            int total = 0;
            foreach (var pair in alreadyEnqueuedNodes)
            {
                total += pair.Value.Count;
            }
            return total;
        }

        // Добавляет ID узла в конец очереди для NPC (если его там еще не было)
        public void EnqueueStartNode(string npcId, int nodeId)
        {
            // Инициализация структур, если NPC новый
            if (!npcNodeQueues.ContainsKey(npcId))
            {
                npcNodeQueues[npcId] = new Queue<int>();
            }
            if (!alreadyEnqueuedNodes.ContainsKey(npcId))
            {
                alreadyEnqueuedNodes[npcId] = new HashSet<int>();
            }

            // Проверка, был ли узел добавлен ранее
            if (alreadyEnqueuedNodes[npcId].Contains(nodeId))
            {
                Debug.Log($"[DialogueStartNodeQueue] Node {nodeId} for NPC {npcId} was already enqueued before. Skipping.");
                return; // Не добавляем снова
            }

            // Добавляем в HashSet и затем в Queue
            alreadyEnqueuedNodes[npcId].Add(nodeId);
            npcNodeQueues[npcId].Enqueue(nodeId);
            Debug.Log($"[DialogueStartNodeQueue] Enqueued: NPC {npcId} -> Node {nodeId} (Queue size: {npcNodeQueues[npcId].Count}, Total enqueued for this NPC: {alreadyEnqueuedNodes[npcId].Count})");
            
            // Сохраняем изменения в PlayerPrefs
            SaveEnqueuedNodesToPrefs();
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
            bool historyRemoved = alreadyEnqueuedNodes.Remove(npcId);

            if (queueRemoved || historyRemoved)
            {
                Debug.Log($"[DialogueStartNodeQueue] Queue and history cleared for NPC {npcId}");
                
                // Удаляем соответствующий ключ из PlayerPrefs
                string key = PREFS_PREFIX + npcId;
                if (PlayerPrefs.HasKey(key))
                {
                    PlayerPrefs.DeleteKey(key);
                    Debug.Log($"[DialogueStartNodeQueue] Удален ключ {key} из PlayerPrefs");
                    SaveEnqueuedNodesToPrefs(); // Обновляем список сохраненных ключей
                }
            }
        }
        
        // Полная очистка всех данных (например, при начале новой игры)
        public void ClearAllData()
        {
            Debug.Log("[DialogueStartNodeQueue] Начата полная очистка данных диалогов");
            npcNodeQueues.Clear();
            alreadyEnqueuedNodes.Clear();
            
            // Удаляем все ключи из PlayerPrefs, связанные с диалогами
            string savedKeys = PlayerPrefs.GetString("SavedDialogueKeys", "");
            if (!string.IsNullOrEmpty(savedKeys))
            {
                foreach (string key in savedKeys.Split(','))
                {
                    if (!string.IsNullOrEmpty(key))
                    {
                        PlayerPrefs.DeleteKey(key);
                        Debug.Log($"[DialogueStartNodeQueue] Удален ключ диалога: {key}");
                    }
                }
                PlayerPrefs.DeleteKey("SavedDialogueKeys");
                PlayerPrefs.Save();
                Debug.Log("[DialogueStartNodeQueue] Все данные о диалогах очищены из PlayerPrefs");
            }
            else
            {
                Debug.Log("[DialogueStartNodeQueue] Нет сохраненных ключей диалогов для очистки");
            }
        }
        
        private void OnDestroy()
        {
            // Сохраняем данные перед уничтожением объекта
            SaveEnqueuedNodesToPrefs();
        }
    }
} 