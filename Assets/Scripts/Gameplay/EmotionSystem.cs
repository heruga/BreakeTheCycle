using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Система управления эмоциями игрока
/// </summary>
public class EmotionSystem : MonoBehaviour
{
    // Синглтон
    public static EmotionSystem Instance { get; private set; }
    
    // Событие изменения активных эмоций
    public event Action<List<Emotion>> OnActiveEmotionsChanged;
    
    // Событие изменения уровня эмоции
    public event Action<EmotionType, int> OnEmotionLevelChanged;
    
    // Событие изменения максимального количества активных эмоций
    public event Action OnMaxActiveEmotionsChanged;
    
    // Константы
    public const int MAX_EMOTIONS = 6;
    public const int MAX_EMOTION_LEVEL = 3;
    public const int BASE_UPGRADE_COST = 25;
    public const int BASE_MAX_EMOTION_INCREASE_COST = 50;
    public const float DEFAULT_COST_MULTIPLIER = 1.5f;
    
    // Максимальное количество эмоций, которые игрок может выбрать одновременно
    [SerializeField] private int maxActiveEmotions = 1;
    
    // Множитель стоимости для каждого следующего уровня увеличения
    [SerializeField] private float costMultiplier = DEFAULT_COST_MULTIPLIER;
    
    // Словарь всех эмоций
    private Dictionary<EmotionType, Emotion> emotions = new Dictionary<EmotionType, Emotion>();
    
    // Список активных эмоций
    private List<Emotion> activeEmotions = new List<Emotion>();
    
    // Словарь уровней эмоций (сохраняемые данные)
    private Dictionary<EmotionType, int> emotionLevels = new Dictionary<EmotionType, int>();
    
    // Кэш данных для UI
    private Dictionary<EmotionType, EmotionData> emotionDataCache = new Dictionary<EmotionType, EmotionData>();
    
    // Типы эмоций
    public enum EmotionType
    {
        Resolve,    // Решимость
        Compassion, // Сострадание
        Fear,       // Страх
        Acceptance, // Принятие
        Anger,      // Гнев
        Denial      // Отрицание
    }
    
    private void Awake()
    {
        // Синглтон
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeEmotions();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// Инициализация всех эмоций
    /// </summary>
    private void InitializeEmotions()
    {
        // Инициализация эмоций с их базовыми эффектами
        
        // Решимость (+10% к урону, если HP меньше 50%, +10% к скорости в бою с элитами)
        emotions[EmotionType.Resolve] = new Emotion(
            EmotionType.Resolve,
            "Решимость",
            "Придает силы в тяжелых ситуациях",
            new Dictionary<int, string>
            {
                { 1, "+10% к урону если HP < 50%, +10% к скорости в бою с элитами" },
                { 2, "+15% к урону если HP < 50%, +15% к скорости в бою с элитами" },
                { 3, "+20% к урону если HP < 50%, +20% к скорости в бою с элитами" }
            }
        );
        
        // Сострадание (небольшое восстановление HP после победы над врагом)
        emotions[EmotionType.Compassion] = new Emotion(
            EmotionType.Compassion,
            "Сострадание",
            "Позволяет восстанавливать силы",
            new Dictionary<int, string>
            {
                { 1, "+5% HP после победы над врагом" },
                { 2, "+10% HP после победы над врагом" },
                { 3, "+15% HP после победы над врагом" }
            }
        );
        
        // Страх (+20% к шансу избежать удара, -10% урона)
        emotions[EmotionType.Fear] = new Emotion(
            EmotionType.Fear,
            "Страх",
            "Обостряет чувства, но снижает силу атаки",
            new Dictionary<int, string>
            {
                { 1, "+20% к шансу избежать удара, -10% урона" },
                { 2, "+25% к шансу избежать удара, -8% урона" },
                { 3, "+30% к шансу избежать удара, -5% урона" }
            }
        );
        
        // Принятие (в случае смерти 1 раз за комнату восстанавливает 20% HP)
        emotions[EmotionType.Acceptance] = new Emotion(
            EmotionType.Acceptance,
            "Принятие",
            "Даёт второй шанс",
            new Dictionary<int, string>
            {
                { 1, "1 раз за комнату при смерти восстанавливает 20% HP" },
                { 2, "1 раз за комнату при смерти восстанавливает 35% HP" },
                { 3, "1 раз за комнату при смерти восстанавливает 50% HP" }
            }
        );
        
        // Гнев (+15% урона, -10% HP)
        emotions[EmotionType.Anger] = new Emotion(
            EmotionType.Anger,
            "Гнев",
            "Увеличивает урон ценой здоровья",
            new Dictionary<int, string>
            {
                { 1, "+15% урона, -10% HP" },
                { 2, "+20% урона, -8% HP" },
                { 3, "+25% урона, -5% HP" }
            }
        );
        
        // Отрицание (1 удар в комнате игнорируется)
        emotions[EmotionType.Denial] = new Emotion(
            EmotionType.Denial,
            "Отрицание",
            "Позволяет отрицать урон",
            new Dictionary<int, string>
            {
                { 1, "1 удар в комнате игнорируется" },
                { 2, "2 удара в комнате игнорируются" },
                { 3, "3 удара в комнате игнорируются" }
            }
        );
        
        // Инициализация уровней эмоций
        foreach (EmotionType type in Enum.GetValues(typeof(EmotionType)))
        {
            emotionLevels[type] = 1;
            
            // Создаем кэш данных для UI
            emotionDataCache[type] = new EmotionData
            {
                Type = type,
                Name = emotions[type].Name,
                Description = emotions[type].Description,
                Level = 1
            };
        }
    }
    
    /// <summary>
    /// Получить список всех эмоций
    /// </summary>
    public Dictionary<EmotionType, Emotion> GetAllEmotions()
    {
        return emotions;
    }
    
    /// <summary>
    /// Получить конкретную эмоцию по типу
    /// </summary>
    public Emotion GetEmotion(EmotionType type)
    {
        if (emotions.TryGetValue(type, out Emotion emotion))
        {
            return emotion;
        }
        
        Debug.LogWarning($"[EmotionSystem] Эмоция типа {type} не найдена");
        return null;
    }
    
    /// <summary>
    /// Получить данные эмоции по типу (для использования в UI)
    /// </summary>
    public EmotionData GetEmotionData(EmotionType type)
    {
        // Проверяем, существует ли кэш для этого типа
        if (!emotionDataCache.ContainsKey(type))
        {
            // Если эмоция существует, создаем для нее кэш
            if (emotions.TryGetValue(type, out Emotion emotion))
            {
                emotionDataCache[type] = new EmotionData
                {
                    Type = type,
                    Name = emotion.Name,
                    Description = emotion.Description,
                    Level = GetEmotionLevel(type)
                };
            }
            else
            {
                Debug.LogWarning($"[EmotionSystem] Попытка получить данные для несуществующей эмоции типа {type}");
                return null;
            }
        }
        
        // Обновляем уровень в кэше, если необходимо
        if (emotionDataCache[type].Level != emotionLevels[type])
        {
            emotionDataCache[type].Level = emotionLevels[type];
        }
        
        return emotionDataCache[type];
    }
    
    /// <summary>
    /// Получить название эмоции
    /// </summary>
    public static string GetEmotionName(EmotionType type)
    {
        if (Instance != null && Instance.emotions.TryGetValue(type, out Emotion emotion))
        {
            return emotion.Name;
        }
        
        // Если эмоции не найдены, возвращаем стандартное название
        switch (type)
        {
            case EmotionType.Resolve: return "Решимость";
            case EmotionType.Compassion: return "Сострадание";
            case EmotionType.Fear: return "Страх";
            case EmotionType.Acceptance: return "Принятие";
            case EmotionType.Anger: return "Гнев";
            case EmotionType.Denial: return "Отрицание";
            default: return "Неизвестная эмоция";
        }
    }
    
    /// <summary>
    /// Получить количество активных эмоций
    /// </summary>
    public int GetActiveEmotionsCount()
    {
        return activeEmotions.Count;
    }
    
    /// <summary>
    /// Получить список активных эмоций
    /// </summary>
    public List<Emotion> GetActiveEmotions()
    {
        return activeEmotions;
    }
    
    /// <summary>
    /// Активировать эмоцию
    /// </summary>
    public bool ActivateEmotion(EmotionType type)
    {
        Debug.Log($"[EmotionSystem] Попытка активировать эмоцию: {type}");
        // Проверяем, существует ли эмоция
        if (!emotions.ContainsKey(type))
        {
            Debug.LogWarning($"[EmotionSystem] Эмоция {type} не найдена!");
            return false;
        }
        
        // Проверка, не активна ли эмоция уже
        if (IsEmotionActive(type))
        {
            Debug.Log($"[EmotionSystem] Эмоция {type} уже активна.");
            return false;
        }
        
        // Проверка, не превышен ли лимит активных эмоций
        if (activeEmotions.Count >= maxActiveEmotions)
        {
            Debug.Log($"[EmotionSystem] Превышен лимит активных эмоций ({maxActiveEmotions}).");
            return false;
        }
        
        // Активация эмоции
        activeEmotions.Add(emotions[type]);
        Debug.Log($"[EmotionSystem] Эмоция {type} активирована. Сейчас активных: {activeEmotions.Count}");
        
        // Вызов события об изменении списка активных эмоций
        OnActiveEmotionsChanged?.Invoke(activeEmotions);
        
        return true;
    }
    
    /// <summary>
    /// Деактивировать эмоцию
    /// </summary>
    public bool DeactivateEmotion(EmotionType type)
    {
        Debug.Log($"[EmotionSystem] Попытка деактивировать эмоцию: {type}");
        // Проверяем, существует ли эмоция
        if (!emotions.ContainsKey(type))
        {
            Debug.LogWarning($"[EmotionSystem] Эмоция {type} не найдена!");
            return false;
        }
        
        // Проверка, активна ли эмоция
        if (!IsEmotionActive(type))
        {
            Debug.Log($"[EmotionSystem] Эмоция {type} не активна.");
            return false;
        }
        
        // Деактивация эмоции
        activeEmotions.Remove(emotions[type]);
        Debug.Log($"[EmotionSystem] Эмоция {type} деактивирована. Сейчас активных: {activeEmotions.Count}");
        
        // Вызов события об изменении списка активных эмоций
        OnActiveEmotionsChanged?.Invoke(activeEmotions);
        
        return true;
    }
    
    /// <summary>
    /// Проверка, активна ли эмоция
    /// </summary>
    public bool IsEmotionActive(EmotionType type)
    {
        return emotions.ContainsKey(type) && activeEmotions.Contains(emotions[type]);
    }
    
    /// <summary>
    /// Сброс всех активных эмоций (при смерти или выходе)
    /// </summary>
    public void ResetActiveEmotions()
    {
        Debug.Log("[EmotionSystem] Сброс всех активных эмоций.");
        activeEmotions.Clear();
        Debug.Log("[EmotionSystem] Сброшены все активные эмоции");
        
        // Вызов события об изменении списка активных эмоций
        OnActiveEmotionsChanged?.Invoke(activeEmotions);
    }
    
    /// <summary>
    /// Получить уровень эмоции
    /// </summary>
    public int GetEmotionLevel(EmotionType type)
    {
        if (emotionLevels.TryGetValue(type, out int level))
        {
            return level;
        }
        
        Debug.LogWarning($"[EmotionSystem] Уровень эмоции типа {type} не найден, возвращаем уровень 1");
        return 1; // По умолчанию возвращаем уровень 1
    }
    
    /// <summary>
    /// Улучшить уровень эмоции
    /// </summary>
    public bool UpgradeEmotion(EmotionType type)
    {
        Debug.Log($"[EmotionSystem] Попытка улучшить эмоцию: {type}");
        // Проверяем, существует ли эмоция
        if (!emotions.ContainsKey(type))
        {
            Debug.LogWarning($"[EmotionSystem] Попытка улучшить несуществующую эмоцию типа {type}");
            return false;
        }
        
        // Проверка, не достигнут ли максимальный уровень
        if (GetEmotionLevel(type) >= MAX_EMOTION_LEVEL)
        {
            Debug.Log($"[EmotionSystem] Эмоция {type} уже на максимальном уровне");
            return false;
        }
        
        // Расчет стоимости улучшения
        int cost = GetUpgradeCost(GetEmotionLevel(type));
        
        // Проверка, достаточно ли фрагментов
        if (CurrencyManager.Instance != null)
        {
            if (!CurrencyManager.Instance.SpendCurrency(cost))
            {
                Debug.Log($"[EmotionSystem] Недостаточно фрагментов для улучшения {type}");
                return false;
            }
        }
        else
        {
            Debug.LogError("[EmotionSystem] CurrencyManager не найден");
            return false;
        }
        
        // Улучшение эмоции
        emotionLevels[type]++;
        int newLevel = emotionLevels[type];
        Debug.Log($"[EmotionSystem] Эмоция {type} улучшена до уровня {newLevel}");
        
        // Обновляем кэш для UI
        if (emotionDataCache.ContainsKey(type))
        {
            emotionDataCache[type].Level = newLevel;
        }
        
        // Вызов события об изменении уровня эмоции
        OnEmotionLevelChanged?.Invoke(type, newLevel);
        
        return true;
    }
    
    /// <summary>
    /// Получить стоимость улучшения эмоции для указанного уровня
    /// </summary>
    public static int GetUpgradeCost(int currentLevel)
    {
        int cost = BASE_UPGRADE_COST;
        
        // Для каждого уровня стоимость увеличивается на множитель
        for (int i = 1; i < currentLevel; i++)
        {
            cost = Mathf.RoundToInt(cost * DEFAULT_COST_MULTIPLIER);
        }
        
        return cost;
    }
    
    /// <summary>
    /// Увеличить максимальное количество одновременно активных эмоций
    /// </summary>
    public bool IncreaseMaxActiveEmotions()
    {
        Debug.Log("[EmotionSystem] Попытка увеличить лимит активных эмоций.");
        // Проверка, не достигнут ли максимум (все 6 эмоций)
        if (maxActiveEmotions >= MAX_EMOTIONS)
        {
            Debug.Log("[EmotionSystem] Уже достигнуто максимальное количество активных эмоций");
            return false;
        }
        
        // Рассчитываем стоимость увеличения
        int cost = GetIncreaseMaxEmotionsCost(maxActiveEmotions);
        
        // Проверка, достаточно ли фрагментов
        if (CurrencyManager.Instance != null)
        {
            if (!CurrencyManager.Instance.SpendCurrency(cost))
            {
                Debug.Log($"[EmotionSystem] Недостаточно фрагментов для увеличения максимума эмоций. Требуется {cost}");
                return false;
            }
        }
        else
        {
            Debug.LogError("[EmotionSystem] CurrencyManager не найден");
            return false;
        }
        
        // Увеличиваем максимум
        maxActiveEmotions++;
        Debug.Log($"[EmotionSystem] Максимальное количество активных эмоций увеличено до {maxActiveEmotions}");
        
        // Вызываем событие об изменении максимального количества активных эмоций
        OnMaxActiveEmotionsChanged?.Invoke();
        
        return true;
    }
    
    /// <summary>
    /// Получить текущее максимальное количество активных эмоций
    /// </summary>
    public int GetMaxActiveEmotions()
    {
        return maxActiveEmotions;
    }
    
    /// <summary>
    /// Получить стоимость увеличения максимального количества активных эмоций для указанного значения
    /// </summary>
    public static int GetIncreaseMaxEmotionsCost(int currentMax)
    {
        int cost = BASE_MAX_EMOTION_INCREASE_COST;
        
        for (int i = 1; i < currentMax; i++)
        {
            cost = Mathf.RoundToInt(cost * DEFAULT_COST_MULTIPLIER);
        }
        
        return cost;
    }
    
    /// <summary>
    /// Получить значение эффекта эмоции в зависимости от ее уровня
    /// </summary>
    public float GetEmotionEffectValue(EmotionType type, EmotionEffectType effectType)
    {
        // Проверяем, существует ли эмоция
        if (!emotions.ContainsKey(type))
        {
            Debug.LogWarning($"[EmotionSystem] Попытка получить значение эффекта для несуществующей эмоции типа {type}");
            return 0f;
        }
        
        int level = GetEmotionLevel(type);
        
        switch (type)
        {
            case EmotionType.Resolve:
                switch (effectType)
                {
                    case EmotionEffectType.DamageBoost:
                        return level == 1 ? 0.1f : level == 2 ? 0.15f : 0.2f;
                    case EmotionEffectType.SpeedBoost:
                        return level == 1 ? 0.1f : level == 2 ? 0.15f : 0.2f;
                    default:
                        return 0f;
                }
            
            case EmotionType.Compassion:
                switch (effectType)
                {
                    case EmotionEffectType.HealPercentage:
                        return level == 1 ? 0.05f : level == 2 ? 0.1f : 0.15f;
                    default:
                        return 0f;
                }
            
            case EmotionType.Fear:
                switch (effectType)
                {
                    case EmotionEffectType.DodgeChance:
                        return level == 1 ? 0.2f : level == 2 ? 0.25f : 0.3f;
                    case EmotionEffectType.DamageReduction:
                        return level == 1 ? -0.1f : level == 2 ? -0.08f : -0.05f;
                    default:
                        return 0f;
                }
            
            case EmotionType.Acceptance:
                switch (effectType)
                {
                    case EmotionEffectType.ReviveHealthPercentage:
                        return level == 1 ? 0.2f : level == 2 ? 0.35f : 0.5f;
                    default:
                        return 0f;
                }
            
            case EmotionType.Anger:
                switch (effectType)
                {
                    case EmotionEffectType.DamageBoost:
                        return level == 1 ? 0.15f : level == 2 ? 0.2f : 0.25f;
                    case EmotionEffectType.HealthReduction:
                        return level == 1 ? -0.1f : level == 2 ? -0.08f : -0.05f;
                    default:
                        return 0f;
                }
            
            case EmotionType.Denial:
                switch (effectType)
                {
                    case EmotionEffectType.IgnoredHitsCount:
                        return level;
                    default:
                        return 0f;
                }
            
            default:
                return 0f;
        }
    }
    
    /// <summary>
    /// Проверить, нужно ли применять эффект при данных условиях
    /// </summary>
    public bool ShouldApplyEmotionEffect(EmotionType type, EmotionEffectType effectType, object context = null)
    {
        // Если эмоция не активна или не существует, не применяем эффект
        if (!IsEmotionActive(type)) return false;
        
        switch (type)
        {
            case EmotionType.Resolve:
                if (effectType == EmotionEffectType.DamageBoost)
                {
                    // Проверяем контекст (HP игрока < 50%)
                    if (context is float healthPercentage)
                    {
                        return healthPercentage < 0.5f;
                    }
                    return false;
                }
                else if (effectType == EmotionEffectType.SpeedBoost)
                {
                    // Проверяем контекст (бой с элитой)
                    if (context is bool fightingElite)
                    {
                        return fightingElite;
                    }
                    return false;
                }
                return false;
            
            // Для остальных эмоций простая проверка активности
            default:
                return true;
        }
    }
}

/// <summary>
/// Класс данных об эмоции
/// </summary>
[Serializable]
public class Emotion
{
    public EmotionSystem.EmotionType Type { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public Dictionary<int, string> LevelDescriptions { get; private set; }
    
    public Emotion(EmotionSystem.EmotionType type, string name, string description, Dictionary<int, string> levelDescriptions)
    {
        Type = type;
        Name = name;
        Description = description;
        LevelDescriptions = levelDescriptions;
    }
    
    public string GetLevelDescription(int level)
    {
        if (LevelDescriptions.ContainsKey(level))
        {
            return LevelDescriptions[level];
        }
        
        return "Описание недоступно";
    }
}

/// <summary>
/// Типы эффектов эмоций
/// </summary>
public enum EmotionEffectType
{
    DamageBoost,            // Бонус к урону
    SpeedBoost,             // Бонус к скорости
    HealPercentage,         // Процент восстановления HP
    DodgeChance,            // Шанс избежать удара
    DamageReduction,        // Снижение урона
    ReviveHealthPercentage, // Процент HP при возрождении
    HealthReduction,        // Снижение HP
    IgnoredHitsCount        // Количество игнорируемых ударов
}

/// <summary>
/// Класс данных об эмоции для использования в UI
/// </summary>
[Serializable]
public class EmotionData
{
    public EmotionSystem.EmotionType Type { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public int Level { get; set; }
} 