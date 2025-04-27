using UnityEngine;

namespace BreakTheCycle.Dialogue
{
    public class RoomDialogueNodeSetter : MonoBehaviour
    {
        [Tooltip("ID NPC, которому нужно назначить стартовый узел")]
        public string npcId;
        [Tooltip("ID стартового узла для диалога")]
        public int startNodeId;

        private void Start()
        {
            string playedKey = $"RoomDialogue_{npcId}_{startNodeId}_Played";
            if (PlayerPrefs.GetInt(playedKey, 0) == 1)
            {
                Debug.Log($"[RoomDialogueNodeSetter] Диалог для NPC {npcId} с узлом {startNodeId} уже был проигран, не добавляем в очередь.");
                return;
            }
            if (!string.IsNullOrEmpty(npcId) && DialogueStartNodeQueue.Instance != null)
            {
                DialogueStartNodeQueue.Instance.SetStartNode(npcId, startNodeId);
                Debug.Log($"[RoomDialogueNodeSetter] Для NPC {npcId} назначен стартовый узел {startNodeId}");
            }
        }
    }
} 