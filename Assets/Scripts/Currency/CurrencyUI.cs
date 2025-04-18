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
        if (CurrencyManager.Instance != null)
        {
            // Подписываемся на изменение валюты
            CurrencyManager.Instance.OnCurrencyChanged += UpdateCurrencyDisplay;
            // Обновляем начальное значение
            UpdateCurrencyDisplay(CurrencyManager.Instance.GetCurrentCurrency());
        }
        else
        {
            Debug.LogWarning("[CurrencyUI] CurrencyManager не найден!");
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