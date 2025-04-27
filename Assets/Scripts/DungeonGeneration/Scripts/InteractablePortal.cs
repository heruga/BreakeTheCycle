using UnityEngine;
using BreakTheCycle;
using UnityEngine.SceneManagement;

namespace DungeonGeneration.Scripts
{
    public class InteractablePortal : BaseInteractable
    {
        [Header("Portal Settings")]
        [SerializeField] private string nextRoomId; // Если задано, портал ведет в конкретную комнату
        [SerializeField] private bool isRandomPortal = true; // Если true, выбирает случайную комнату
        [SerializeField] private Color interactionRadiusColor = new Color(0f, 1f, 0f, 0.2f);

        private RoomManager currentRoom;
        private DungeonGenerator dungeonGenerator;
        private GameObject player;
        private Rigidbody playerRb;
        private bool isInRange;
        private bool showDebugGizmos = true;
        private float lastLogTime = 0f;
        private const float LOG_INTERVAL = 1f;

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
                if (player != null)
                    playerRb = player.GetComponent<Rigidbody>();
            }
            if (player == null) return;

            float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
            bool wasInRange = isInRange;
            isInRange = distanceToPlayer <= InteractionRadius;

            // Периодическое логирование, если игрок в зоне взаимодействия
            if (isInRange && Time.time - lastLogTime >= LOG_INTERVAL)
            {
                Debug.Log($"[InteractablePortal] Игрок находится в зоне взаимодействия. Расстояние: {distanceToPlayer:F2} м");
                lastLogTime = Time.time;
            }
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

            string targetRoomId;

            if (isRandomPortal)
            {
                var availableRooms = dungeonGenerator.GetAvailableRooms();
                Debug.Log($"[InteractablePortal] Доступных комнат: {(availableRooms != null ? availableRooms.Count : 0)}");

                if (availableRooms == null || availableRooms.Count == 0)
                {
                    Debug.Log("[InteractablePortal] Нет доступных комнат для телепортации, возвращаемся в реальность");
                    if (GameManager.Instance == null)
                    {
                        var gameManager = FindObjectOfType<GameManager>();
                        if (gameManager == null)
                        {
                            Debug.LogError("[InteractablePortal] GameManager не найден в сцене! Переход в сцену Reality невозможен. Исправьте архитектуру переходов!");
                            return;
                        }
                        else
                        {
                            string targetScene = gameManager.IsInReality() ? gameManager.consciousnessSceneName : gameManager.realitySceneName;
                            gameManager.StartCoroutine(gameManager.SwitchWorldCoroutine(targetScene));
                            return;
                        }
                    }
                    string targetScene2 = GameManager.Instance.IsInReality() ? GameManager.Instance.consciousnessSceneName : GameManager.Instance.realitySceneName;
                    GameManager.Instance.StartCoroutine(GameManager.Instance.SwitchWorldCoroutine(targetScene2));
                    return;
                }

                targetRoomId = availableRooms[Random.Range(0, availableRooms.Count)].Id;
                Debug.Log($"[InteractablePortal] Выбрана случайная комната с ID: {targetRoomId}");
            }
            else
            {
                if (string.IsNullOrEmpty(nextRoomId))
                {
                    Debug.LogError("[InteractablePortal] Не задан ID комнаты для фиксированного портала!");
                    return;
                }
                targetRoomId = nextRoomId;
                Debug.Log($"[InteractablePortal] Используется фиксированная комната с ID: {targetRoomId}");
            }

            if (player == null)
            {
                player = GameObject.FindGameObjectWithTag("Player");
                if (player == null)
                {
                    Debug.LogError("[InteractablePortal] Игрок не найден для телепортации!");
                    return;
                }
            }

            Debug.Log($"[InteractablePortal] Попытка телепортации в комнату: {targetRoomId}, игрок: {player.name}, текущая позиция: {player.transform.position}");
            dungeonGenerator.LoadRoom(targetRoomId);

            Debug.Log("[InteractablePortal] Вызов телепортации завершен");
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