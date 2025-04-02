using UnityEngine;
using UnityEngine.SceneManagement;

namespace DungeonGeneration.Scripts.Health
{
    public class PlayerHealth : Health
    {
        public void Heal(float amount)
        {
            if (amount <= 0) return;
            TakeDamage(-amount); // Используем отрицательный урон для лечения
        }

        protected override void Die()
        {
            base.Die();
            
            Debug.Log("[PlayerHealth] Игрок умер, загружаем сцену Reality");
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SwitchWorld();
            }
            else
            {
                Debug.LogError("[PlayerHealth] GameManager не найден! Невозможно загрузить сцену Reality");
                SceneManager.LoadScene("Reality");
            }
        }
    }
} 