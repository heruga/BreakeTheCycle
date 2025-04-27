using UnityEngine;
using UnityEngine.SceneManagement;

namespace DungeonGeneration.Scripts.Health
{
    public class PlayerHealth : Health
    {
        private EmotionSystem emotionSystem;
        private bool isDead = false;

        private void Start()
        {
            // Пробуем найти EmotionSystem
            emotionSystem = FindObjectOfType<EmotionSystem>();
            if (emotionSystem == null)
            {
                Debug.LogWarning("[PlayerHealth] EmotionSystem не найден при старте. Будет попытка найти его позже.");
            }
        }

        private void Update()
        {
            // Если EmotionSystem не найден, пробуем найти его снова
            if (emotionSystem == null)
            {
                emotionSystem = FindObjectOfType<EmotionSystem>();
            }
        }

        public override void TakeDamage(float damage)
        {
            if (damage <= 0)
            {
                Heal(-damage);
                return;
            }

            // Если EmotionSystem не найден, пробуем найти его
            if (emotionSystem == null)
            {
                emotionSystem = FindObjectOfType<EmotionSystem>();
            }

            if (emotionSystem != null)
            {
                // Проверяем, можно ли игнорировать урон (Отрицание)
                if (emotionSystem.ShouldIgnoreDamage())
                {
                    return;
                }

                // Модифицируем входящий урон с учетом эффектов эмоций
                damage = emotionSystem.ModifyIncomingDamage(damage);
            }

            base.TakeDamage(damage);
        }

        public void Heal(float amount)
        {
            if (amount <= 0) return;
            
            float prevHealth = currentHealth;
            currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
            
            if (currentHealth > prevHealth)
            {
                Debug.Log($"[PlayerHealth] Восстановлено {currentHealth - prevHealth:F1} HP");
            }
        }

        /// <summary>
        /// Устанавливает текущее здоровье напрямую
        /// </summary>
        /// <param name="health">Новое значение здоровья</param>
        public void SetHealth(float health)
        {
            float prevHealth = currentHealth;
            currentHealth = Mathf.Clamp(health, 0, maxHealth);
            Debug.Log($"[PlayerHealth] Установлено здоровье: {currentHealth:F1} (было: {prevHealth:F1})");
        }

        protected override void Die()
        {
            if (isDead) return;
            isDead = true;
            // Если EmotionSystem не найден, пробуем найти его
            if (emotionSystem == null)
            {
                emotionSystem = FindObjectOfType<EmotionSystem>();
            }

            if (emotionSystem != null && emotionSystem.TryRevive())
            {
                Debug.Log("[PlayerHealth] Игрок воскрешен через систему эмоций");
                isDead = false;
                return;
            }

            base.Die();
            
            Debug.Log("[PlayerHealth] Игрок умер, загружаем сцену Reality");
            if (GameManager.Instance != null)
            {
                // Явно переходим в реальность, а не просто инвертируем состояние
                GameManager.Instance.StartCoroutine(GameManager.Instance.LoadSceneAsync(GameManager.Instance.realitySceneName));
            }
            else
            {
                Debug.LogError("[PlayerHealth] GameManager не найден! Переход в сцену Reality невозможен. Исправьте архитектуру переходов!");
            }
        }
    }
} 