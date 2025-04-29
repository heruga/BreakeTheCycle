using UnityEngine;
using BreakTheCycle;
using DungeonGeneration.Scripts;

namespace DungeonGeneration.Scripts
{
    /// Специальный тип портала, который всегда инициирует возвращение в "Реальность" при взаимодействии, при условии, что комната зачищена.
    public class InteractablePortalReality : InteractablePortal
    {
        public override void OnInteract()
        {
            Debug.Log("[InteractablePortalReality] Начало взаимодействия с порталом реальности.");

            // Получаем RoomManager, если он еще не получен в Start() базового класса
            // (Start() базового класса должен вызваться автоматически)
             RoomManager currentRoom = GetComponentInParent<RoomManager>(); // Можно получить ссылку здесь или убедиться, что она установлена в Start

            // Проверяем, зачищена ли комната (как в базовом классе)
            if (currentRoom != null && !currentRoom.IsRoomCleared)
            {
                Debug.Log("[InteractablePortalReality] Комната не зачищена! Нужно победить всех врагов.");
                // Тут можно добавить UI сообщение для игрока
                return;
            }

            // Проверяем наличие GameManager
            if (GameManager.Instance != null)
            {
                Debug.Log("[InteractablePortalReality] Комната зачищена. Запускаем переход в реальность через GameManager.SwitchWorld().");
                // Вызываем метод GameManager, который управляет переходом между мирами
                // и сам использует корутины для загрузки сцены и затухания.
                GameManager.Instance.SwitchWorld();
            }
            else
            {
                Debug.LogError("[InteractablePortalReality] GameManager не найден! Не удалось вернуться в реальность.");
            }
        }
    }
}
