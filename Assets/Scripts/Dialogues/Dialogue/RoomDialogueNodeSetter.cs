using UnityEngine;

namespace BreakTheCycle.Dialogue
{
    /// <summary>
    /// Триггер, который при активации объекта (в Awake) пытается поставить
    /// предопределенный узел диалога в очередь для NPC, используя компонент DialogueNodeEnqueuer.
    /// </summary>
    [RequireComponent(typeof(DialogueNodeEnqueuer))] // Гарантируем наличие DialogueNodeEnqueuer
    public class RoomDialogueNodeSetter : MonoBehaviour
    {
        // Удаляем поля npcId и startNodeId, так как они теперь в DialogueNodeEnqueuer
        // [Tooltip("ID NPC, которому нужно назначить стартовый узел")]
        // public string npcId;
        // [Tooltip("ID стартового узла для диалога")]
        // public int startNodeId;

        private DialogueNodeEnqueuer nodeEnqueuer;

        private void Awake()
        {
            nodeEnqueuer = GetComponent<DialogueNodeEnqueuer>();
            
            // Компонент DialogueNodeEnqueuer должен быть на этом же объекте,
            // и его поля npcId и startNodeId должны быть настроены в инспекторе.
            if (nodeEnqueuer != null)
            {
                Debug.Log($"[RoomDialogueNodeSetter] Объект {gameObject.name} активирован. Пытаемся использовать DialogueNodeEnqueuer для постановки узла в очередь.");
                bool success = nodeEnqueuer.TryEnqueueNode();
                if (success)
                {
                    Debug.Log($"[RoomDialogueNodeSetter] Узел для NPC '{nodeEnqueuer.npcId}' (startNodeId: {nodeEnqueuer.startNodeId}) успешно добавлен в очередь через DialogueNodeEnqueuer при активации {gameObject.name}.");
                }
                else
                {
                    Debug.LogWarning($"[RoomDialogueNodeSetter] Не удалось добавить узел в очередь через DialogueNodeEnqueuer при активации {gameObject.name}. Подробности см. в логах DialogueNodeEnqueuer.");
                }
            }
            else
            {
                // Этого не должно произойти благодаря [RequireComponent], но на всякий случай
                Debug.LogError($"[RoomDialogueNodeSetter] На объекте {gameObject.name} отсутствует необходимый компонент DialogueNodeEnqueuer!", this);
            }
        }
    }
}
 