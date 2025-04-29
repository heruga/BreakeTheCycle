using UnityEngine;

namespace BreakTheCycle.Dialogue
{
    public class RoomDialogueNodeSetter : MonoBehaviour
    {
        [Tooltip("ID NPC, которому нужно назначить стартовый узел")]
        public string npcId;
        [Tooltip("ID стартового узла для диалога")]
        public int startNodeId;

        private void Awake()
        {
            Debug.Log($"[RoomDialogueNodeSetter] Awake: npcId={npcId}, startNodeId={startNodeId}, gameObject={gameObject.name}. Attempting to enqueue.");
            
            if (!string.IsNullOrEmpty(npcId) && DialogueStartNodeQueue.Instance != null)
            {
                DialogueStartNodeQueue.Instance.EnqueueStartNode(npcId, startNodeId);
            }
            else
            {
                Debug.LogWarning($"[RoomDialogueNodeSetter] Cannot enqueue: npcId is empty or DialogueStartNodeQueue.Instance is null.");
            }
        }
    }
}
 