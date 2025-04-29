using UnityEngine;
using UnityEngine.SceneManagement; // Необходимо для работы со сценами

public class EndScreenHandler : MonoBehaviour
{
    // Публичный метод, который будет вызываться кнопкой
    public void ReturnToMainMenu()
    {
        // Выводим сообщение в консоль для отладки
        Debug.Log("[EndScreenHandler] Нажата кнопка на финальном экране. Загрузка главного меню...");

        // Загружаем сцену главного меню
        // Убедись, что имя сцены "MainMenu" указано верно!
        SceneManager.LoadScene("MainMenu");
    }

}
