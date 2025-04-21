using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Компонент для отображения валюты на экране
/// </summary>
public class CurrencyUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TMP_Text currencyText;
    [SerializeField] private Image currencyIcon;
    
    private void Start()
    {
        TryInitializeCurrencyManager();
    }

    private void TryInitializeCurrencyManager()
    {
        if (CurrencyManager.Instance != null)
        {
            // Подписываемся на изменение валюты
            CurrencyManager.Instance.OnCurrencyChanged += UpdateCurrencyDisplay;
            // Обновляем начальное значение
            UpdateCurrencyDisplay(CurrencyManager.Instance.GetCurrentCurrency());
        }
        else
        {
            Debug.LogWarning("[CurrencyUI] CurrencyManager не найден! Попытка инициализации через GameManager...");
            
            // Проверяем GameManager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.InitializeCurrencyManager();
                // Повторяем попытку через небольшой промежуток времени
                Invoke(nameof(TryInitializeCurrencyManager), 0.1f);
            }
            else
            {
                Debug.LogError("[CurrencyUI] GameManager не найден! Убедитесь, что GameManager присутствует в сцене.");
            }
        }
    }

    private void OnDestroy()
    {
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.OnCurrencyChanged -= UpdateCurrencyDisplay;
        }
    }

    private void UpdateCurrencyDisplay(int newAmount)
    {
        if (currencyText != null)
        {
            currencyText.text = newAmount.ToString();
        }
    }
} 