using UnityEngine;
using DungeonGeneration.Scripts.Health;
using DungeonGeneration.Scripts.UI;

namespace DungeonGeneration.Scripts.Health
{
    public class EnemyHealth : Health
    {
        [SerializeField] private bool isBoss = false;
        
        [Header("Валюта")]
        [Tooltip("Количество валюты, выпадающей при смерти")]
        [SerializeField] private int currencyDropAmount = 10;
        [Tooltip("Множитель валюты для босса")]
        [SerializeField] private float bossMultiplier = 5f;

        [Header("UI")]
        [SerializeField] private GameObject healthBarPrefab;
        private EnemyHealthBar healthBar;
        private bool isHealthBarActive = false; // Флаг для отслеживания активации

        public bool IsBoss => isBoss;

        private void Start()
        {
            if (healthBarPrefab != null)
            {
                var barObj = Instantiate(healthBarPrefab);
                healthBar = barObj.GetComponent<EnemyHealthBar>();
                if (healthBar != null) // Доп. проверка, что компонент найден
                {
                    healthBar.SetTarget(transform);
                    UpdateHealthBar();
                    healthBar.gameObject.SetActive(false); // Деактивируем при старте
                }
                else
                {
                    Debug.LogError("[EnemyHealth] Не найден компонент EnemyHealthBar на префабе HealthBar!");
                }
            }
        }

        public override void TakeDamage(float damage)
        {
            // Активируем HealthBar при первом получении урона
            if (damage > 0 && healthBar != null && !isHealthBarActive)
            {
                healthBar.gameObject.SetActive(true);
                isHealthBarActive = true;
            }
            
            if (damage > 0 && currentHealth > 0)
            {
                var controller = GetComponent<Enemies.EnemyController>();
                if (controller != null)
                {
                    controller.PlayHitAnimation();
                }
            }
            base.TakeDamage(damage);
            UpdateHealthBar();
        }

        protected override void Die()
        {
            var controller = GetComponent<Enemies.EnemyController>();
            if (controller != null)
            {
                controller.PlayDeathAnimation();
            }
            if (healthBar != null)
            {
                Destroy(healthBar.gameObject);
            }
            base.Die();
            
            // Добавляем валюту при смерти врага
            if (GameManager.Instance != null && 
                GameManager.Instance.GetCurrentState() == GameManager.GameState.Consciousness)
            {
                DropCurrency();
            }
            
            Destroy(gameObject);
        }
        
        /// Выпадение валюты при смерти врага
        private void DropCurrency()
        {
            if (CurrencyManager.Instance != null)
            {
                int amount = currencyDropAmount;
                
                // Если это босс, увеличиваем количество валюты
                if (isBoss)
                {
                    amount = Mathf.RoundToInt(amount * bossMultiplier);
                }
                
                CurrencyManager.Instance.AddCurrency(amount);
                Debug.Log($"[EnemyHealth] После смерти {gameObject.name} выронил {amount} валюты");
            }
        }

        private void UpdateHealthBar()
        {
            if (healthBar != null)
            {
                float healthPercentage = GetHealthPercentage();
                UnityEngine.Debug.Log($"[EnemyHealth] Updating health bar. Percentage: {healthPercentage}");
                healthBar.SetHealth(healthPercentage);
            }
            else
            {
                UnityEngine.Debug.LogWarning("[EnemyHealth] UpdateHealthBar called, but healthBar is null!");
            }
        }

        private void OnDestroy()
        {
            if (healthBar != null)
            {
                Destroy(healthBar.gameObject);
            }
        }
    }
} 