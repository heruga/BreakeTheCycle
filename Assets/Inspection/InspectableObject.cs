using UnityEngine;

namespace Inspection
{
    public class InspectableObject : MonoBehaviour
    {
        [Header("Initial Settings")] 
        public Vector3 spawnPositionOffset;
        public Vector3 spawnRotationOffset;
        public Vector2 minMaxZoom = new Vector2(0.5f,2);
        public float defaultZoomValue = 1f;

        [SerializeField] private string objectName = "Inspectable Object";
        [SerializeField] private string description = "This is an inspectable object.";

        public string ObjectName => objectName;
        public string Description => description;
    }
}
