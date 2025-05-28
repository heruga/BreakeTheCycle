using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Animations;

namespace DungeonGeneration.Scripts.Health
{
    public class PlayerHealth : Health
    {
        private EmotionSystem emotionSystem;
        private bool isDead = false;
        private Animator animator;

        private void Start()
        {
            // Пробуем найти EmotionSystem
            emotionSystem = FindObjectOfType<EmotionSystem>();
            if (emotionSystem == null)
            {
                Debug.LogWarning("[PlayerHealth] EmotionSystem не найден при старте. Будет попытка найти его позже.");
            }
            animator = GetComponent<Animator>();
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
            Debug.Log("[PlayerHealth] TakeDamage вызван с damage: " + damage);
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
                Debug.Log("[PlayerHealth] EmotionSystem найден, проверяем, можно ли игнорировать урон (Отрицание)");
                // Проверяем, можно ли игнорировать урон (Отрицание)
                if (emotionSystem.ShouldIgnoreDamage())
                {
                    Debug.Log("[PlayerHealth] Урон игнорируется (Отрицание)");
                    return;
                }

                // Модифицируем входящий урон с учетом эффектов эмоций
                damage = emotionSystem.ModifyIncomingDamage(damage);
            }

            if (animator != null && !isDead)
            {
                animator.SetTrigger("isHit");
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
            InvokeHealthChanged();
        }

        /// <summary>
        /// Устанавливает максимальное здоровье напрямую
        /// </summary>
        /// <param name="newMaxHealth">Новое значение максимального здоровья</param>
        public void SetMaxHealth(float newMaxHealth)
        {
            float prevMaxHealth = maxHealth;
            maxHealth = Mathf.Max(1, newMaxHealth); // Минимум 1 HP
            
            // Ограничиваем текущее здоровье новым максимумом
            if (currentHealth > maxHealth)
            {
                currentHealth = maxHealth;
            }
            
            Debug.Log($"[PlayerHealth] Установлено макс. здоровье: {maxHealth:F1} (было: {prevMaxHealth:F1})");
            InvokeHealthChanged();
        }

        protected override void Die()
        {
            if (isDead) return;
            isDead = true;
            
            // Останавливаем время сразу после установки флага смерти
            Time.timeScale = 0;
            
            if (animator != null)
            {
                animator.SetTrigger("isDead");
            }
            // Если EmotionSystem не найден, пробуем найти его
            if (emotionSystem == null)
            {
                emotionSystem = FindObjectOfType<EmotionSystem>();
            }

            if (emotionSystem != null && emotionSystem.TryRevive())
            {
                Debug.Log("[PlayerHealth] Игрок воскрешен через систему эмоций");
                isDead = false;
                // Возвращаем нормальное течение времени, если игрок был воскрешен
                Time.timeScale = 1;
                return;
            }

            // base.Die(); // <-- ВРЕМЕННО ЗАКОММЕНТИРОВАНО ДЛЯ ТЕСТА
            
            Debug.Log("[PlayerHealth] Игрок умер. Обращаемся к GameManager для обработки смерти.");
            if (GameManager.Instance != null)
            {
                // Явно переходим в реальность, а не просто инвертируем состояние
                // GameManager.Instance.StartCoroutine(GameManager.Instance.LoadSceneAsync(GameManager.Instance.realitySceneName)); // <--- ЭТУ СТРОКУ УДАЛЯЕМ/КОММЕНТИРУЕМ
                GameManager.Instance.HandlePlayerDeath(); // <--- ВОТ ЭТА СТРОКА ДОБАВЛЕНА
            }
            else
            {
                Debug.LogError("[PlayerHealth] GameManager не найден! Не могу корректно обработать смерть и переход в сцену Reality. Исправьте архитектуру переходов!");
                // В качестве крайней меры, можно попробовать загрузить сцену напрямую, но это не идеальный вариант
                // SceneManager.LoadScene("Reality"); 
            }
        }
    }
} 