using UnityEngine;
using DungeonGeneration.Scripts.Health;

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

        protected override void Die()
        {
            base.Die();
            
            // Добавляем валюту при смерти врага
            if (GameManager.Instance != null && 
                GameManager.Instance.GetCurrentState() == GameManager.GameState.Consciousness)
            {
                DropCurrency();
            }
            
            Destroy(gameObject);
        }
        
        /// <summary>
        /// Выпадение валюты при смерти врага
        /// </summary>
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
                Debug.Log($"[EnemyHealth] Враг {gameObject.name} выронил {amount} валюты");
            }
        }
    }
} 