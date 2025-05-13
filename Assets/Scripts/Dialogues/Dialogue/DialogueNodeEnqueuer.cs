using UnityEngine;

namespace BreakTheCycle.Dialogue // Убедись, что пространство имен соответствует твоему проекту
{
    /// <summary>
    /// Компонент, отвечающий за постановку определенного стартового узла диалога в очередь для NPC.
    /// Содержит данные (npcId, startNodeId) и метод для выполнения этой операции.
    /// Может использоваться другими скриптами (триггерами), чтобы определить, КОГДА это нужно сделать.
    /// </summary>
    public class DialogueNodeEnqueuer : MonoBehaviour
    {
        [Tooltip("ID NPC, которому нужно назначить стартовый узел. Должен быть указан.")]
        public string npcId;

        [Tooltip("ID стартового узла для диалога. Должен быть 0 или больше.")]
        public int startNodeId = -1; // Инициализируем -1, чтобы была явная необходимость установки

        /// <summary>
        /// Пытается добавить указанные npcId и startNodeId в DialogueStartNodeQueue.
        /// </summary>
        /// <returns>True, если узел успешно добавлен в очередь, иначе false.</returns>
        public bool TryEnqueueNode()
        {
            if (string.IsNullOrEmpty(npcId))
            {
                Debug.LogWarning($"[DialogueNodeEnqueuer] Попытка добавить узел в очередь для объекта {gameObject.name}, но npcId не указан.", this);
                return false;
            }

            if (startNodeId < 0)
            {
                Debug.LogWarning($"[DialogueNodeEnqueuer] Попытка добавить узел в очередь для NPC '{npcId}' (объект {gameObject.name}), но startNodeId ({startNodeId}) недействителен. Он должен быть >= 0.", this);
                return false;
            }

            if (DialogueStartNodeQueue.Instance == null)
            {
                Debug.LogError($"[DialogueNodeEnqueuer] DialogueStartNodeQueue.Instance не найден в сцене! Невозможно добавить узел для NPC '{npcId}' (объект {gameObject.name}).", this);
                return false;
            }

            Debug.Log($"[DialogueNodeEnqueuer] Для объекта {gameObject.name}: Пытаемся добавить узел {startNodeId} для NPC '{npcId}' в очередь.");
            DialogueStartNodeQueue.Instance.EnqueueStartNode(npcId, startNodeId);
            // Предполагаем, что EnqueueStartNode всегда успешен, если дошли до сюда
            // (если он может выбрасывать исключения или возвращать статус, логику можно усложнить)
            return true;
        }
    }
} 