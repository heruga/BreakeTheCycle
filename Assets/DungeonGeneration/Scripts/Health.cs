using UnityEngine;
using System;
using UnityEngine.SceneManagement;

namespace DungeonGeneration
{
    public class Health : MonoBehaviour
    {
        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth;

        public event Action<GameObject> OnDeath;
        public event Action<float> OnHealthChanged;

        private void Start()
        {
            currentHealth = maxHealth;
        }

        public void TakeDamage(float damage)
        {
            currentHealth = Mathf.Max(0, currentHealth - damage);
            OnHealthChanged?.Invoke(currentHealth);

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        public void Heal(float amount)
        {
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            OnHealthChanged?.Invoke(currentHealth);
        }

        private void Die()
        {
            OnDeath?.Invoke(gameObject);
            
            // Если это игрок, загружаем сцену Reality
            if (gameObject.CompareTag("Player"))
            {
                Debug.Log("[Health] Игрок умер, загружаем сцену Reality");
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.SwitchWorld();
                }
                else
                {
                    Debug.LogError("[Health] GameManager не найден! Невозможно загрузить сцену Reality");
                    // Загружаем сцену напрямую, если GameManager недоступен
                    SceneManager.LoadScene("Reality");
                }
            }
            else
            {
                // Для врагов просто уничтожаем объект
                Destroy(gameObject);
            }
        }

        public float GetHealthPercentage()
        {
            return currentHealth / maxHealth;
        }

        public float GetCurrentHealth()
        {
            return currentHealth;
        }

        public float GetMaxHealth()
        {
            return maxHealth;
        }
    }
} 