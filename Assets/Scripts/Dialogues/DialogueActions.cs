using UnityEngine;
using VIDE_Data; // Необходимо для доступа к VIDE_Data
using BreakTheCycle.Dialogue; // Необходимо для доступа к DialogueStartNodeQueue и NPCDialogueInteract
using System.Collections.Generic; // Необходимо для работы со списками/словарями
using System.Linq; // Необходимо для метода ToList()
using DungeonGeneration; // Необходимо для доступа к DungeonGenerator
using DungeonGeneration.Scripts; // Необходимо для доступа к RoomManager

namespace BreakTheCycle.Actions
{
    public class DialogueActions : MonoBehaviour
    {
        /// <summary>
        /// Вызывается из Action Node.
        /// Получает ID текущего NPC, ставит в очередь диалог с узла 37 для него.
        /// Если игра находится в "Сознании", инициирует переход в "Реальность".
        /// Завершение диалога будет зависеть от того, есть ли узлы после этого Action Node.
        /// </summary>
        public void ReturnToRealityAndQueueDialogue()
        {
            Debug.Log("[DialogueActions] Вызван ReturnToRealityAndQueueDialogue");

            if (!VIDE_Data.VIDE_Data.isActive || VIDE_Data.VIDE_Data.assigned == null)
            {
                Debug.LogError("[DialogueActions] Невозможно выполнить: диалог не активен или VIDE_Assign не назначен.");
                return;
            }

            // Получаем компонент NPCDialogueInteract с объекта, с которым идет диалог
            var npcInteract = VIDE_Data.VIDE_Data.assigned.GetComponent<NPCDialogueInteract>();
            if (npcInteract == null)
            {
                Debug.LogError($"[DialogueActions] На объекте {VIDE_Data.VIDE_Data.assigned.gameObject.name} не найден компонент NPCDialogueInteract.");
                // Завершаем диалог в любом случае, чтобы не зависнуть при ошибке
                VIDE_Data.VIDE_Data.EndDialogue();
                Debug.LogWarning("[DialogueActions] Диалог завершен принудительно из-за ошибки поиска NPCDialogueInteract.");
                return;
            }

            string npcId = npcInteract.npcId;
            if (string.IsNullOrEmpty(npcId))
            {
                 Debug.LogError($"[DialogueActions] У NPCDialogueInteract на объекте {VIDE_Data.VIDE_Data.assigned.gameObject.name} не задан npcId.");
                 // Завершаем диалог при ошибке
                 VIDE_Data.VIDE_Data.EndDialogue();
                 Debug.LogWarning("[DialogueActions] Диалог завершен принудительно из-за отсутствия npcId.");
                 return;
            }

            if (DialogueStartNodeQueue.Instance == null)
            {
                 Debug.LogError("[DialogueActions] Экземпляр DialogueStartNodeQueue не найден. Невозможно поставить узел в очередь.");
                 // Завершаем диалог при ошибке
                 VIDE_Data.VIDE_Data.EndDialogue();
                 Debug.LogWarning("[DialogueActions] Диалог завершен принудительно из-за отсутствия DialogueStartNodeQueue.");
                 return;
            }
            
            // --- ДОБАВЛЕНО: Проверка GameManager --- 
            if (GameManager.Instance == null)
            {
                Debug.LogError("[DialogueActions] Экземпляр GameManager не найден. Невозможно переключить мир.");
                // Завершаем диалог при ошибке
                VIDE_Data.VIDE_Data.EndDialogue();
                Debug.LogWarning("[DialogueActions] Диалог завершен принудительно из-за отсутствия GameManager.");
                return;
            }
            // --- КОНЕЦ ДОБАВЛЕНИЯ --- 

            // Ставим узел 37 в очередь для этого NPC
            int nodeToQueue = 37;
            DialogueStartNodeQueue.Instance.SetStartNode(npcId, nodeToQueue);
            Debug.Log($"[DialogueActions] Для NPC '{npcId}' поставлен в очередь стартовый узел {nodeToQueue}.");

            // --- ДОБАВЛЕНО: Переключение мира, если мы не в реальности ---
            if (!GameManager.Instance.IsInReality())
            {
                Debug.Log("[DialogueActions] Инициируем переход в Реальность через GameManager.SwitchWorld().");
                GameManager.Instance.SwitchWorld();
                // Важно: НЕ вызываем EndDialogue() здесь, так как SwitchWorld инициирует
                // асинхронный процесс смены сцены, и нам не нужно прерывать его.
                // Диалог завершится либо сам (если Action Node последний), либо переходом
                // к следующему узлу (если он есть), либо будет прерван сменой сцены.
            }
            else
            {
                 Debug.Log("[DialogueActions] Уже находимся в Реальности, переключение мира не требуется.");
            }
            // --- КОНЕЦ ДОБАВЛЕНИЯ ---

            Debug.Log("[DialogueActions] ReturnToRealityAndQueueDialogue завершен.");
            // Если после этого Action Node есть другие узлы в VIDE, они будут вызваны,
            // но переход сцены, инициированный SwitchWorld(), скорее всего, прервет их выполнение.
        }

        /// Вызывается из Action Node.
        /// Находит все комнаты в подземелье, уничтожает в них всех врагов
        /// и помечает комнаты как очищенные.
        /// Не завершает текущий диалог сам по себе.
        public void ClearAllRoomsAndDisableSpawns()
        {
            Debug.Log("[DialogueActions] Вызван ClearAllRoomsAndDisableSpawns");

            // Находим генератор подземелий
            DungeonGenerator dungeonGenerator = FindObjectOfType<DungeonGenerator>();
            if (dungeonGenerator == null)
            {
                Debug.LogError("[DialogueActions] DungeonGenerator не найден на сцене!");
                return;
            }

            var roomManagers = GetRoomManagers(dungeonGenerator);

            if (roomManagers == null || roomManagers.Count == 0)
            {
                Debug.LogWarning("[DialogueActions] Не найдено активных RoomManager для очистки.");
                return;
            }

            Debug.Log($"[DialogueActions] Найдено {roomManagers.Count} комнат для очистки.");

            // Проходим по всем найденным RoomManager и вызываем принудительную очистку
            foreach (var roomManager in roomManagers)
            {
                if (roomManager != null)
                {
                    roomManager.ForceClearRoomAndDestroyEnemies();
                }
                else
                {
                    Debug.LogWarning("[DialogueActions] Обнаружен null RoomManager в списке.");
                }
            }

            Debug.Log("[DialogueActions] Все активные комнаты принудительно очищены.");
        }
        
        // Вспомогательный метод для получения RoomManager. Может потребовать адаптации,
        // если DungeonGenerator хранит ссылки иначе.
        private List<RoomManager> GetRoomManagers(DungeonGenerator generator)
        {
            if (generator.transform != null) // Используем transform генератора как родителя
            {
                return generator.transform.GetComponentsInChildren<RoomManager>().ToList();
            }
            
            // Запасной вариант: найти все RoomManager на сцене
             Debug.LogWarning("[DialogueActions] Не удалось найти контейнер подземелья, ищем все RoomManager на сцене.");
             return FindObjectsOfType<RoomManager>().ToList();
        }
    }
} 