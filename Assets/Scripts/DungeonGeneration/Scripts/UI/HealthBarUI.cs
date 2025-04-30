using UnityEngine;
using UnityEngine.UI;
using DungeonGeneration.Scripts.Health;
using TMPro;

namespace DungeonGeneration.Scripts.UI
{
    public class HealthBarUI : MonoBehaviour
    {
        [SerializeField] private RectTransform healthBarFill;
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private TextMeshProUGUI healthText;
        
        private Vector3 originalScale;

        private void Start()
        {
            if (healthBarFill == null)
            {
                Debug.LogError("HealthBarFill не назначен!");
            }
            else
            {
                // Запоминаем исходный масштаб полоски здоровья
                originalScale = healthBarFill.localScale;
            }

            if (healthText == null)
            {
                Debug.LogError("HealthText не назначен в инспекторе!");
            }

            if (playerHealth == null)
            {
                playerHealth = FindObjectOfType<PlayerHealth>();
            }

            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged += UpdateHealthBar;
                UpdateHealthBar(playerHealth.GetCurrentHealth());
            }
            else
            {
                Debug.LogError("PlayerHealth не найден на сцене!");
            }
        }

        private void UpdateHealthBar(float currentHealth)
        {
            if (healthBarFill != null)
            {
                float healthPercentage = playerHealth.GetHealthPercentage();
                // Изменяем только масштаб по X, сохраняя Y и Z
                healthBarFill.localScale = new Vector3(
                    originalScale.x * healthPercentage,
                    originalScale.y,
                    originalScale.z
                );
            }

            if (healthText != null && playerHealth != null)
            {
                // Обновляем текстовое поле
                healthText.text = $"{Mathf.CeilToInt(currentHealth)}/{Mathf.CeilToInt(playerHealth.GetMaxHealth())}";
            }
        }

        private void OnDestroy()
        {
            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged -= UpdateHealthBar;
            }
        }
    }
} 