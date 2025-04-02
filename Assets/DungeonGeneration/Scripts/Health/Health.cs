using UnityEngine;
using System;

namespace DungeonGeneration.Scripts.Health
{
    public abstract class Health : MonoBehaviour
    {
        [Header("Health Settings")]
        [SerializeField] protected float maxHealth = 100f;
        [SerializeField] protected float currentHealth;
        [SerializeField] protected GameObject deathEffectPrefab;

        public event Action<GameObject> OnDeath;
        public event Action<float> OnHealthChanged;

        protected virtual void Start()
        {
            currentHealth = maxHealth;
        }

        public virtual void TakeDamage(float damage)
        {
            if (damage <= 0) return;

            currentHealth = Mathf.Max(0, currentHealth - damage);
            OnHealthChanged?.Invoke(currentHealth);

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        protected virtual void Die()
        {
            if (deathEffectPrefab != null)
            {
                Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
            }

            OnDeath?.Invoke(gameObject);
        }

        public virtual float GetHealthPercentage()
        {
            if (maxHealth <= 0)
            {
                Debug.LogError($"Invalid maxHealth value: {maxHealth}");
                return 0;
            }
            return currentHealth / maxHealth;
        }

        public virtual float GetCurrentHealth()
        {
            return currentHealth;
        }

        public virtual float GetMaxHealth()
        {
            return maxHealth;
        }

        protected virtual void OnDestroy()
        {
            OnDeath = null;
            OnHealthChanged = null;
        }
    }
} 