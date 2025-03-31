using UnityEngine;
using UnityEngine.SceneManagement;

namespace BreakTheCycle
{
    public class DoorInteraction : InteractableObject
    {
        [Header("Настройки двери")]
        [SerializeField] private RoomType roomType;
        [SerializeField] private KeyCode interactionKey = KeyCode.N;
        [SerializeField] private string nextRoomId;

        private bool isInteractionAllowed = false;
        private RoomManager roomManager;
        private GameManager gameManager;
        protected bool isPlayerInRange = false;

        private void Start()
        {
            roomManager = FindObjectOfType<RoomManager>();
            gameManager = GameManager.Instance;

            // В StartRoom дверь всегда доступна для взаимодействия
            if (roomType == RoomType.StartRoom)
            {
                isInteractionAllowed = true;
            }
        }

        private void Update()
        {
            if (roomType == RoomType.CombatRoom)
            {
                // В Combat Room проверяем, все ли враги побеждены
                isInteractionAllowed = roomManager != null && roomManager.AreAllEnemiesDefeated();
            }

            // Если игрок в зоне взаимодействия и нажал клавишу
            if (isPlayerInRange && isInteractionAllowed && Input.GetKeyDown(interactionKey))
            {
                OnInteract();
            }
        }

        public override string GetInteractionText()
        {
            if (!isInteractionAllowed)
            {
                return "Дверь заперта. Победите всех врагов, чтобы открыть её.";
            }
            return "Нажмите N чтобы перейти в следующую комнату";
        }

        public override void Interact()
        {
            if (isInteractionAllowed)
            {
                OnInteract();
            }
        }

        private void OnInteract()
        {
            if (roomManager != null)
            {
                roomManager.LoadNextRoom(nextRoomId);
            }
        }

        public override bool IsPlayerInRange(Transform playerTransform)
        {
            isPlayerInRange = Vector3.Distance(transform.position, playerTransform.position) <= InteractionRadius;
            return isPlayerInRange;
        }
    }

    public enum RoomType
    {
        StartRoom,
        CombatRoom,
        BossRoom
    }
} 