using UnityEngine;
using System;

/// <summary>
/// Менеджер валюты, которая падает с врагов в режиме Consciousness
/// </summary>
public class CurrencyManager : MonoBehaviour
{
    [SerializeField] private int currentCurrency = 0;
    
    // Событие изменения количества валюты
    public event Action<int> OnCurrencyChanged;
    
    // Синглтон
    public static CurrencyManager Instance { get; private set; }
    
    private const string PlayerPrefsCurrencyKey = "PlayerCurrency";

    private void Awake()
    {
        // Паттерн синглтон
        if (Instance == null)
        {
            Instance = this;
            // Загружаем валюту из PlayerPrefs
            currentCurrency = PlayerPrefs.GetInt(PlayerPrefsCurrencyKey, 0);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// Добавляет валюту игроку
    /// </summary>
    public void AddCurrency(int amount)
    {
        if (amount < 0)
        {
            Debug.LogWarning("[CurrencyManager] Попытка добавить отрицательное количество валюты");
            return;
        }
        
        currentCurrency += amount;
        PlayerPrefs.SetInt(PlayerPrefsCurrencyKey, currentCurrency);
        PlayerPrefs.Save();
        OnCurrencyChanged?.Invoke(currentCurrency);
        Debug.Log($"[CurrencyManager] Добавлено {amount} валюты. Текущее количество: {currentCurrency}");
    }
    
    /// <summary>
    /// Расходует валюту, если достаточно
    /// </summary>
    /// <returns>true, если валюты достаточно и она была потрачена</returns>
    public bool SpendCurrency(int amount)
    {
        if (amount <= 0) return false;
        
        if (currentCurrency >= amount)
        {
            currentCurrency -= amount;
            PlayerPrefs.SetInt(PlayerPrefsCurrencyKey, currentCurrency);
            PlayerPrefs.Save();
            OnCurrencyChanged?.Invoke(currentCurrency);
            Debug.Log($"[CurrencyManager] Потрачено {amount} валюты. Текущее количество: {currentCurrency}");
            return true;
        }
        
        Debug.Log($"[CurrencyManager] Недостаточно валюты. Требуется {amount}, имеется {currentCurrency}");
        return false;
    }
    
    /// <summary>
    /// Получает текущее количество валюты
    /// </summary>
    public int GetCurrentCurrency()
    {
        return currentCurrency;
    }
} 