using UnityEngine;

namespace BreakTheCycle.Dialogue
{
    public class MonologueTriggerData : MonoBehaviour
    {
        [Tooltip("ID монолога, который должен проигрываться после взаимодействия или осмотра этого объекта")]
        public int monologueID = -1;
    }
} 