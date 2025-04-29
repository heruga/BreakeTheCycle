using UnityEngine;
using BreakTheCycle;
using UnityEngine.SceneManagement;
using System.Linq;

namespace DungeonGeneration.Scripts
{
    public class InteractablePortal : BaseInteractable
    {
        [SerializeField] private Color interactionRadiusColor = new Color(0f, 1f, 0f, 0.2f);

        private RoomManager currentRoom;
        private DungeonGenerator dungeonGenerator;
        private GameObject player;
        private bool isInRange;
        private bool showDebugGizmos = true;

        public bool IsInRange => isInRange;

        private void Start()
        {
            currentRoom = GetComponentInParent<RoomManager>();
            if (currentRoom == null)
            {
                Debug.LogWarning("[InteractablePortal] RoomManager не найден!");
            }

            dungeonGenerator = FindObjectOfType<DungeonGenerator>();
            if (dungeonGenerator == null)
            {
                Debug.LogWarning("[InteractablePortal] DungeonGenerator не найден!");
            }
        }

        private void Update()
        {
            if (player == null)
            {
                player = GameObject.FindGameObjectWithTag("Player");
            }
            if (player == null) return;

            float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
            bool wasInRange = isInRange;
            isInRange = distanceToPlayer <= InteractionRadius;
        }

        public override void OnInteract()
        {
            Debug.Log("[InteractablePortal] Начало взаимодействия с порталом");

            if (currentRoom != null && !currentRoom.IsRoomCleared)
            {
                Debug.Log("[InteractablePortal] Комната не зачищена! Нужно победить всех врагов.");
                return;
            }

            if (dungeonGenerator == null)
            {
                dungeonGenerator = FindObjectOfType<DungeonGenerator>();
                if (dungeonGenerator == null)
                {
                    Debug.LogError("[InteractablePortal] DungeonGenerator не найден!");
                    return;
                }
            }

            // Получаем доступные комнаты через метод генератора
            var availableRooms = dungeonGenerator.GetAvailableRooms();

            if (availableRooms == null || availableRooms.Count == 0)
            {
                Debug.Log("[InteractablePortal] Нет доступных комнат (вероятно, это конец данжа/комната босса). Возвращаемся в реальность.");
                
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.SwitchWorld();
                }
                else
                {
                    Debug.LogError("[InteractablePortal] GameManager не найден! Не удалось вернуться в реальность.");
                }
                return;
            }

            // Выбираем случайную из доступных комнат (это может быть обычная или босс-комната)
            var targetRoom = availableRooms[Random.Range(0, availableRooms.Count)];
            Debug.Log($"[InteractablePortal] Телепортируемся в комнату: {targetRoom.Id} (Тип: {targetRoom.RoomType?.typeName ?? "Неизвестен"})");
            dungeonGenerator.LoadRoom(targetRoom.Id);
        }

        public override void OnPlayerEnter()
        {
            // Можно добавить визуальную обратную связь при входе в зону
        }

        public override void OnPlayerExit()
        {
            // Можно добавить визуальную обратную связь при выходе из зоны
        }

        private void OnDrawGizmos()
        {
            if (showDebugGizmos)
            {
                Gizmos.color = interactionRadiusColor;
                Gizmos.DrawSphere(transform.position, InteractionRadius);
            }
        }
    }
} 