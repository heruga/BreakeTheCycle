using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Компонент для применения эффектов эмоций к игроку
/// </summary>
public class PlayerEmotionHandler : MonoBehaviour
{
    // Информация о персонаже
    private float maxHealth = 100f;
    private float currentHealth;
    
    // Данные для эффектов эмоций
    private Dictionary<EmotionSystem.EmotionType, object> emotionData = new Dictionary<EmotionSystem.EmotionType, object>();
    
    // Ссылка на EmotionSystem
    private EmotionSystem emotionSystem;
    
    // События для перехвата различных действий
    public delegate float DamageModifierDelegate(float damage);
    public DamageModifierDelegate OnDamageModifier;
    
    public delegate bool DodgeCheckDelegate();
    public DodgeCheckDelegate OnDodgeCheck;
    
    public delegate float HealthModifierDelegate(float health);
    public HealthModifierDelegate OnHealthModifier;
    
    public delegate float SpeedModifierDelegate(float speed);
    public SpeedModifierDelegate OnSpeedModifier;
    
    private void Awake()
    {
        // Инициализация эмоций
        InitializeEmotionData();
        
        // Установка здоровья
        currentHealth = maxHealth;
    }
    
    private void Start()
    {
        // Получаем ссылку на EmotionSystem
        emotionSystem = EmotionSystem.Instance;
        
        if (emotionSystem == null)
        {
            Debug.LogError("[PlayerEmotionHandler] EmotionSystem не найден");
            return;
        }
        
        // Подписываемся на события
        emotionSystem.OnActiveEmotionsChanged += OnEmotionsChanged;
    }
    
    private void OnDestroy()
    {
        // Отписываемся от событий
        if (emotionSystem != null)
        {
            emotionSystem.OnActiveEmotionsChanged -= OnEmotionsChanged;
        }
    }
    
    /// <summary>
    /// Инициализирует данные для эффектов эмоций
    /// </summary>
    private void InitializeEmotionData()
    {
        // Отрицание (1 удар в комнате игнорируется)
        emotionData[EmotionSystem.EmotionType.Denial] = new DenialData();
        
        // Принятие (в случае смерти 1 раз за комнату восстанавливает 20% HP)
        emotionData[EmotionSystem.EmotionType.Acceptance] = new AcceptanceData();
    }
    
    /// <summary>
    /// Вызывается при изменении активных эмоций
    /// </summary>
    private void OnEmotionsChanged(List<Emotion> activeEmotions)
    {
        // Сброс состояния при смене эмоций
        ResetEmotionDataForNewRoom();
    }
    
    /// <summary>
    /// Вызывается при входе в новую комнату
    /// </summary>
    public void OnEnterNewRoom()
    {
        // Сброс состояния эмоций для новой комнаты
        ResetEmotionDataForNewRoom();
    }
    
    /// <summary>
    /// Сбрасывает состояние эмоций для новой комнаты
    /// </summary>
    private void ResetEmotionDataForNewRoom()
    {
        // Сброс счетчика игнорирования урона для Отрицания
        if (emotionData.TryGetValue(EmotionSystem.EmotionType.Denial, out object denialObj) && denialObj is DenialData denial)
        {
            int maxIgnores = (int)emotionSystem.GetEmotionEffectValue(EmotionSystem.EmotionType.Denial, EmotionEffectType.IgnoredHitsCount);
            denial.remainingIgnoredHits = maxIgnores;
        }
        
        // Сброс флага воскрешения для Принятия
        if (emotionData.TryGetValue(EmotionSystem.EmotionType.Acceptance, out object acceptanceObj) && acceptanceObj is AcceptanceData acceptance)
        {
            acceptance.hasRevived = false;
        }
    }
    
    /// <summary>
    /// Обрабатывает получение урона с учетом эмоций
    /// </summary>
    /// <param name="damage">Входящий урон</param>
    /// <returns>Модифицированный урон</returns>
    public float ModifyDamage(float damage)
    {
        if (emotionSystem == null) return damage;
        
        float modifiedDamage = damage;
        
        // Проверяем шанс уклонения (Страх)
        if (emotionSystem.IsEmotionActive(EmotionSystem.EmotionType.Fear))
        {
            float dodgeChance = emotionSystem.GetEmotionEffectValue(EmotionSystem.EmotionType.Fear, EmotionEffectType.DodgeChance);
            if (Random.value < dodgeChance)
            {
                Debug.Log("[PlayerEmotionHandler] Уклонение от удара благодаря эмоции Страх");
                return 0f; // Удар полностью игнорируется
            }
        }
        
        // Проверяем игнорирование урона (Отрицание)
        if (emotionSystem.IsEmotionActive(EmotionSystem.EmotionType.Denial))
        {
            if (emotionData.TryGetValue(EmotionSystem.EmotionType.Denial, out object denialObj) && denialObj is DenialData denial)
            {
                if (denial.remainingIgnoredHits > 0)
                {
                    denial.remainingIgnoredHits--;
                    Debug.Log($"[PlayerEmotionHandler] Удар игнорирован благодаря эмоции Отрицание. Осталось игнорирований: {denial.remainingIgnoredHits}");
                    return 0f; // Удар полностью игнорируется
                }
            }
        }
        
        // Модификатор урона от Страха (снижение урона)
        if (emotionSystem.IsEmotionActive(EmotionSystem.EmotionType.Fear))
        {
            float damageMod = emotionSystem.GetEmotionEffectValue(EmotionSystem.EmotionType.Fear, EmotionEffectType.DamageReduction);
            modifiedDamage *= (1f + damageMod); // damageMod отрицательный, поэтому используем сложение
        }
        
        // Применяем внешние модификаторы урона
        if (OnDamageModifier != null)
        {
            modifiedDamage = OnDamageModifier(modifiedDamage);
        }
        
        return modifiedDamage;
    }
    
    /// <summary>
    /// Проверяет, должен ли игрок уклониться от атаки
    /// </summary>
    public bool ShouldDodgeAttack()
    {
        if (emotionSystem == null) return false;
        
        // Проверяем уклонение от Страха
        if (emotionSystem.IsEmotionActive(EmotionSystem.EmotionType.Fear))
        {
            float dodgeChance = emotionSystem.GetEmotionEffectValue(EmotionSystem.EmotionType.Fear, EmotionEffectType.DodgeChance);
            bool shouldDodge = Random.value < dodgeChance;
            
            if (shouldDodge)
            {
                Debug.Log("[PlayerEmotionHandler] Уклонение от удара благодаря эмоции Страх");
            }
            
            return shouldDodge;
        }
        
        // Применяем внешние проверки уклонения
        if (OnDodgeCheck != null)
        {
            return OnDodgeCheck();
        }
        
        return false;
    }
    
    /// <summary>
    /// Модифицирует урон, наносимый игроком, с учетом эмоций
    /// </summary>
    /// <param name="damage">Базовый урон</param>
    /// <returns>Модифицированный урон</returns>
    public float ModifyPlayerDamage(float damage, bool isElite = false)
    {
        if (emotionSystem == null) return damage;
        
        float modifiedDamage = damage;
        
        // Модификатор от Решимости (если HP < 50%)
        if (emotionSystem.IsEmotionActive(EmotionSystem.EmotionType.Resolve))
        {
            float healthPercent = currentHealth / maxHealth;
            if (healthPercent < 0.5f)
            {
                float damageBoost = emotionSystem.GetEmotionEffectValue(EmotionSystem.EmotionType.Resolve, EmotionEffectType.DamageBoost);
                modifiedDamage *= (1f + damageBoost);
                Debug.Log($"[PlayerEmotionHandler] Урон увеличен на {damageBoost*100}% благодаря Решимости");
            }
        }
        
        // Модификатор от Гнева
        if (emotionSystem.IsEmotionActive(EmotionSystem.EmotionType.Anger))
        {
            float damageBoost = emotionSystem.GetEmotionEffectValue(EmotionSystem.EmotionType.Anger, EmotionEffectType.DamageBoost);
            modifiedDamage *= (1f + damageBoost);
            Debug.Log($"[PlayerEmotionHandler] Урон увеличен на {damageBoost*100}% благодаря Гневу");
        }
        
        return modifiedDamage;
    }
    
    /// <summary>
    /// Модифицирует скорость игрока с учетом эмоций
    /// </summary>
    /// <param name="speed">Базовая скорость</param>
    /// <returns>Модифицированная скорость</returns>
    public float ModifyPlayerSpeed(float speed, bool fightingElite = false)
    {
        if (emotionSystem == null) return speed;
        
        float modifiedSpeed = speed;
        
        // Бонус скорости от Решимости (в бою с элитами)
        if (emotionSystem.IsEmotionActive(EmotionSystem.EmotionType.Resolve) && fightingElite)
        {
            float speedBoost = emotionSystem.GetEmotionEffectValue(EmotionSystem.EmotionType.Resolve, EmotionEffectType.SpeedBoost);
            modifiedSpeed *= (1f + speedBoost);
            Debug.Log($"[PlayerEmotionHandler] Скорость увеличена на {speedBoost*100}% благодаря Решимости");
        }
        
        // Применяем внешние модификаторы скорости
        if (OnSpeedModifier != null)
        {
            modifiedSpeed = OnSpeedModifier(modifiedSpeed);
        }
        
        return modifiedSpeed;
    }
    
    /// <summary>
    /// Обрабатывает восстановление здоровья после победы над врагом
    /// </summary>
    public void OnEnemyDefeated()
    {
        if (emotionSystem == null) return;
        
        // Эффект Сострадания - восстановление HP после победы
        if (emotionSystem.IsEmotionActive(EmotionSystem.EmotionType.Compassion))
        {
            float healPercent = emotionSystem.GetEmotionEffectValue(EmotionSystem.EmotionType.Compassion, EmotionEffectType.HealPercentage);
            float healAmount = maxHealth * healPercent;
            
            // Восстанавливаем здоровье
            currentHealth = Mathf.Min(currentHealth + healAmount, maxHealth);
            
            Debug.Log($"[PlayerEmotionHandler] Восстановлено {healAmount} HP благодаря Состраданию");
        }
    }
    
    /// <summary>
    /// Проверяет, должен ли игрок воскреснуть после смерти
    /// </summary>
    /// <returns>Процент восстановленного здоровья или 0, если воскрешение не произошло</returns>
    public float TryRevive()
    {
        if (emotionSystem == null) return 0f;
        
        // Проверяем эффект Принятия
        if (emotionSystem.IsEmotionActive(EmotionSystem.EmotionType.Acceptance))
        {
            if (emotionData.TryGetValue(EmotionSystem.EmotionType.Acceptance, out object acceptanceObj) && acceptanceObj is AcceptanceData acceptance)
            {
                // Если воскрешение в этой комнате уже было использовано
                if (acceptance.hasRevived)
                {
                    Debug.Log("[PlayerEmotionHandler] Воскрешение от Принятия уже было использовано в этой комнате");
                    return 0f;
                }
                
                // Применяем эффект воскрешения
                float revivePercent = emotionSystem.GetEmotionEffectValue(EmotionSystem.EmotionType.Acceptance, EmotionEffectType.ReviveHealthPercentage);
                acceptance.hasRevived = true;
                
                Debug.Log($"[PlayerEmotionHandler] Воскрешение с {revivePercent*100}% HP благодаря Принятию");
                
                // Восстанавливаем здоровье
                currentHealth = maxHealth * revivePercent;
                
                return revivePercent;
            }
        }
        
        return 0f; // Воскрешение не произошло
    }
    
    /// <summary>
    /// Устанавливает максимальное здоровье с учетом эмоций
    /// </summary>
    public void SetMaxHealth(float newMaxHealth)
    {
        maxHealth = newMaxHealth;
        
        // Применяем модификаторы максимального здоровья
        if (emotionSystem != null && emotionSystem.IsEmotionActive(EmotionSystem.EmotionType.Anger))
        {
            float healthMod = emotionSystem.GetEmotionEffectValue(EmotionSystem.EmotionType.Anger, EmotionEffectType.HealthReduction);
            maxHealth *= (1f + healthMod); // healthMod отрицательный, поэтому используем сложение
            Debug.Log($"[PlayerEmotionHandler] Максимальное здоровье уменьшено на {-healthMod*100}% из-за Гнева");
        }
        
        // Применяем внешние модификаторы здоровья
        if (OnHealthModifier != null)
        {
            maxHealth = OnHealthModifier(maxHealth);
        }
        
        // Обновляем текущее здоровье пропорционально
        float healthPercent = currentHealth / (newMaxHealth / maxHealth);
        currentHealth = Mathf.Min(healthPercent * maxHealth, maxHealth);
    }
    
    /// <summary>
    /// Устанавливает текущее здоровье
    /// </summary>
    public void SetCurrentHealth(float health)
    {
        currentHealth = Mathf.Clamp(health, 0f, maxHealth);
    }
    
    /// <summary>
    /// Получает текущее здоровье
    /// </summary>
    public float GetCurrentHealth()
    {
        return currentHealth;
    }
    
    /// <summary>
    /// Получает максимальное здоровье с учетом модификаторов
    /// </summary>
    public float GetMaxHealth()
    {
        return maxHealth;
    }
    
    /// <summary>
    /// Получает процент здоровья
    /// </summary>
    public float GetHealthPercentage()
    {
        return currentHealth / maxHealth;
    }
}

/// <summary>
/// Данные для эмоции Отрицание
/// </summary>
internal class DenialData
{
    public int remainingIgnoredHits = 1;
}

/// <summary>
/// Данные для эмоции Принятие
/// </summary>
internal class AcceptanceData
{
    public bool hasRevived = false;
} 