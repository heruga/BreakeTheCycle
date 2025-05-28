using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.UI;
using TMPro;
using DungeonGeneration.Scripts.Health;
using System.Collections;
using UI;

public class EmotionSystem : MonoBehaviour
{
    [Header("Цвета состояний")]
    public Color activeColor = new Color(0.2f, 0.8f, 0.2f); // Зеленый для активной эмоции
    public Color inactiveColor = Color.white; // Белый для неактивной эмоции

    [Header("Настройки улучшения")]
    public int baseLevelUpCost = 60; // Базовая стоимость улучшения
    private int currentMaxEmotionsLevel = 1; // Текущий уровень максимума эмоций

    private TextMeshProUGUI levelText;
    private TextMeshProUGUI levelUpText;
    private Button levelUpButton;

    public enum EmotionType
    {
        Determination,  // Решимость
        Compassion,     // Сострадание
        Fear,          // Страх
        Acceptance,    // Принятие
        Anger,         // Гнев
        Denial         // Отрицание
    }

    [System.Serializable]
    public class Emotion
    {
        public EmotionType type;
        public int level = 1;
        public bool isActive = false;
    }

    private List<Emotion> emotions = new List<Emotion>();
    private int maxActiveEmotions = 1;
    private int currentActiveEmotions = 0;

    private PlayerHealth playerHealth;
    private ConsciousnessController playerController;

    // Словарь с описаниями эффектов для каждого уровня
    private readonly Dictionary<EmotionType, string[]> emotionEffects = new Dictionary<EmotionType, string[]>
    {
        {
            EmotionType.Determination,
            new string[]
            {
                "+10% к скорости, +10% к урону, если HP меньше 50%",
                "+15% к скорости, +15% к урону, если HP меньше 50%",
                "+20% к скорости, +20% к урону, если HP меньше 50%"
            }
        },
        {
            EmotionType.Compassion,
            new string[]
            {
                "+3% текущего HP после победы над врагом",
                "+6% текущего HP после победы над врагом",
                "+9% текущего HP после победы над врагом"
            }
        },
        {
            EmotionType.Fear,
            new string[]
            {
                "+20% к шансу избежать удара, -10% урона",
                "+25% к шансу избежать удара, -8% урона",
                "+30% к шансу избежать удара, -5% урона"
            }
        },
        {
            EmotionType.Acceptance,
            new string[]
            {
                "1 раз при смерти восстанавливает 20% HP",
                "1 раз при смерти восстанавливает 35% HP",
                "1 раз при смерти восстанавливает 50% HP"
            }
        },
        {
            EmotionType.Anger,
            new string[]
            {
                "+15% урона, -10% макс. HP",
                "+20% урона, -8% макс. HP",
                "+25% урона, -5% макс. HP"
            }
        },
        {
            EmotionType.Denial,
            new string[]
            {
                "1 удар в комнате игнорируется",
                "2 удара в комнате игнорируются",
                "3 удара в комнате игнорируются"
            }
        }
    };

    private Dictionary<EmotionType, int> roomIgnoreCounters = new Dictionary<EmotionType, int>();
    private bool hasUsedReviveInCurrentRoom = false;

    // Dictionary to store progress images for each emotion
    private Dictionary<EmotionType, Image> upgradeProgressImages = new Dictionary<EmotionType, Image>();

    private Dictionary<EmotionType, EmotionButton> emotionButtons = new Dictionary<EmotionType, EmotionButton>();

    private void Awake()
    {
        // Инициализируем список эмоций
        foreach (EmotionType type in Enum.GetValues(typeof(EmotionType)))
        {
            int savedLevel = PlayerPrefs.GetInt($"EmotionLevel_{type}", 1);
            emotions.Add(new Emotion { type = type, level = savedLevel });
            roomIgnoreCounters[type] = 0;
        }
        currentMaxEmotionsLevel = PlayerPrefs.GetInt("MaxActiveEmotionsLevel", 1);
        maxActiveEmotions = currentMaxEmotionsLevel;
    }

    private void Start()
    {
        // Находим компоненты
        Debug.Log("Начинаем Start");
        playerHealth = FindObjectOfType<PlayerHealth>();
        playerController = FindObjectOfType<ConsciousnessController>();
        
        if (playerHealth == null)
        {
            Debug.LogWarning("[EmotionSystem] PlayerHealth не найден при старте. Будет попытка найти его позже.");
        }
        
        FindUIElements();
        UpdateUITexts();
        
        // Отложенная инициализация UI
        StartCoroutine(InitializeUIWithDelay());
        
        Debug.Log("End Start");
    }

    private IEnumerator InitializeUIWithDelay()
    {
        // Ждем один кадр, чтобы дать время на создание всех объектов
        yield return null;
        
        // Пробуем инициализировать UI
        InitializeUI();
    }

    public void InitializeUI()
    {
        Debug.Log("[EmotionSystem] Начинаем инициализацию UI");
        SetupEmotionTooltips();
        
        // Обновляем все UI элементы
        foreach (Emotion emotion in emotions)
        {
            UpdateEmotionVisual(emotion.type, emotion.isActive);
        }
    }

    private void Update()
    {
        // Если PlayerHealth не найден, пробуем найти его снова
        if (playerHealth == null)
        {
            playerHealth = FindObjectOfType<PlayerHealth>();
        }
        
        // Update progress for emotions being upgraded
        foreach (var button in emotionButtons.Values)
        {
            if (button == null || button.gameObject == null) continue;

            EmotionType emotionType = button.GetEmotionType();
            LongHoldHandler longHoldHandler = button.gameObject.GetComponent<LongHoldHandler>();
            if (longHoldHandler != null)
            {
                if (longHoldHandler.IsHolding)
                {
                    float progress = (Time.time - longHoldHandler.HoldStartTime) / longHoldHandler.holdTime;
                    button.UpdateProgress(progress);
                }
            }
        }
    }

    private void FindUIElements()
    {
        Debug.Log("[EmotionSystem] Начинаю поиск UI элементов");
        
        GameObject levelObj = GameObject.Find("Level");
        if (levelObj != null)
        {
            levelText = levelObj.GetComponent<TextMeshProUGUI>();
            Debug.Log("[EmotionSystem] Найден текст уровня");
        }
        else
        {
            Debug.LogError("[EmotionSystem] Не найден объект Level!");
        }

        GameObject levelUpObj = GameObject.Find("LevelUp");
        if (levelUpObj != null)
        {
            levelUpButton = levelUpObj.GetComponent<Button>();
            if (levelUpButton != null)
            {
                Debug.Log("[EmotionSystem] Найдена кнопка LevelUp");
                // Ищем текст внутри кнопки
                levelUpText = levelUpObj.GetComponentInChildren<TextMeshProUGUI>();
                if (levelUpText != null)
                {
                    Debug.Log("[EmotionSystem] Найден текст кнопки LevelUp");
                }
                else
                {
                    Debug.LogError("[EmotionSystem] Не найден текст внутри кнопки LevelUp!");
                }
            }
            else
            {
                Debug.LogError("[EmotionSystem] На объекте LevelUp нет компонента Button!");
            }
        }
        else
        {
            Debug.LogError("[EmotionSystem] Не найден объект LevelUp!");
        }
    }

    private void SetupEmotionTooltips()
    {
        Debug.Log("[EmotionSystem] Начинаем установку тултипов");
        emotionButtons.Clear(); // Очищаем словарь перед новым заполнением
        
        // Находим все кнопки эмоций
        EmotionButton[] buttons = FindObjectsOfType<EmotionButton>();
        foreach (var button in buttons)
        {
            EmotionType type = button.GetEmotionType();
            emotionButtons[type] = button;
            button.Initialize(this);
            UpdateEmotionTooltip(type, button);
        }
        
        if (emotionButtons.Count == 0)
        {
            Debug.LogWarning("[EmotionSystem] Не найдено ни одной кнопки эмоций!");
        }
    }

    private void UpdateEmotionTooltip(EmotionType emotionType, EmotionButton button)
    {
        Debug.Log($"Обновляем тултип для {emotionType}");
        if (button != null)
        {
            button.UpdateTooltip(GetEmotionDescription(emotionType));
        }
    }

    private void UpdateEmotionVisual(EmotionType emotionType, bool isActive)
    {
        if (emotionButtons.TryGetValue(emotionType, out EmotionButton button))
        {
            button.UpdateVisual(isActive, activeColor, inactiveColor);
            UpdateEmotionTooltip(emotionType, button);
        }
        else
        {
            Debug.LogWarning($"Не найдена кнопка для эмоции {emotionType}");
        }
    }

    // Методы для привязки к кнопкам в Unity
    public void ToggleDetermination()
    {
        ToggleEmotion(EmotionType.Determination);
    }

    public void ToggleCompassion()
    {
        ToggleEmotion(EmotionType.Compassion);
    }

    public void ToggleFear()
    {
        ToggleEmotion(EmotionType.Fear);
    }

    public void ToggleAcceptance()
    {
        ToggleEmotion(EmotionType.Acceptance);
    }

    public void ToggleAnger()
    {
        ToggleEmotion(EmotionType.Anger);
    }

    public void ToggleDenial()
    {
        ToggleEmotion(EmotionType.Denial);
    }

    public void ToggleEmotion(EmotionType emotionType)
    {
        Emotion emotion = emotions.Find(e => e.type == emotionType);
        if (emotion == null) return;

        if (emotion.isActive)
        {
            DeactivateEmotion(emotion);
        }
        else
        {
            if (currentActiveEmotions < maxActiveEmotions)
            {
                ActivateEmotion(emotion);
            }
            else
            {
                Debug.Log("Достигнут лимит активных эмоций!");
            }
        }
    }

    private void ActivateEmotion(Emotion emotion)
    {
        emotion.isActive = true;
        currentActiveEmotions++;
        Debug.Log($"Активирована эмоция: {emotion.type} (Уровень {emotion.level})");
        UpdateEmotionVisual(emotion.type, true);

        // Применяем эффекты эмоции сразу при активации
        if (playerHealth != null)
        {
            switch (emotion.type)
            {
                case EmotionType.Anger:
                    // Уменьшаем максимальное HP при активации гнева
                    float healthPenalty = 0.10f - (emotion.level - 1) * 0.025f; // 10/8/5%
                    float originalMaxHealth = playerHealth.GetMaxHealth();
                    float newMaxHealth = originalMaxHealth * (1 - healthPenalty);
                    
                    // Получаем текущий процент здоровья
                    float healthPercentage = playerHealth.GetCurrentHealth() / originalMaxHealth;
                    
                    // Устанавливаем новое максимальное здоровье
                    playerHealth.SetMaxHealth(newMaxHealth);
                    
                    // Устанавливаем текущее здоровье в том же процентном соотношении
                    float newCurrentHealth = newMaxHealth * healthPercentage;
                    playerHealth.SetHealth(newCurrentHealth);
                    
                    Debug.Log($"[EmotionSystem] Гнев активирован: макс. HP уменьшено на {healthPenalty:P0} (с {originalMaxHealth} до {newMaxHealth})");
                    break;
            }
        }
    }

    private void DeactivateEmotion(Emotion emotion)
    {
        emotion.isActive = false;
        currentActiveEmotions--;
        Debug.Log($"Деактивирована эмоция: {emotion.type}");
        UpdateEmotionVisual(emotion.type, false);

        // Отменяем эффекты эмоции при деактивации
        if (playerHealth != null)
        {
            switch (emotion.type)
            {
                case EmotionType.Anger:
                    // Восстанавливаем максимальное HP при деактивации гнева
                    float healthPenalty = 0.10f - (emotion.level - 1) * 0.025f; // 10/8/5%
                    float currentMaxHealth = playerHealth.GetMaxHealth();
                    float originalMaxHealth = currentMaxHealth / (1 - healthPenalty);
                    
                    // Получаем текущий процент здоровья
                    float healthPercentage = playerHealth.GetCurrentHealth() / currentMaxHealth;
                    
                    // Устанавливаем исходное максимальное здоровье
                    playerHealth.SetMaxHealth(originalMaxHealth);
                    
                    // Устанавливаем текущее здоровье в том же процентном соотношении
                    float newCurrentHealth = originalMaxHealth * healthPercentage;
                    playerHealth.SetHealth(newCurrentHealth);
                    
                    Debug.Log($"[EmotionSystem] Гнев деактивирован: макс. HP восстановлено до {originalMaxHealth}");
                    break;
            }
        }
    }

    private int CalculateEmotionUpgradeCost(Emotion emotion)
    {
        if (emotion == null) return 0; // Or handle error appropriately
        // Текущая логика расчета стоимости
        return 30 * emotion.level;
    }

    public void UpgradeEmotion(EmotionType emotionType)
    {
        Emotion emotion = emotions.Find(e => e.type == emotionType);
        if (emotion == null) return;

        int upgradeCost = CalculateEmotionUpgradeCost(emotion); // Используем новый метод
        if (!CurrencyManager.Instance.SpendCurrency(upgradeCost))
        {
            // Недостаточно валюты, не улучшаем
            return;
        }

        if (emotion.level < 3)
        {
            emotion.level++;
            PlayerPrefs.SetInt($"EmotionLevel_{emotion.type}", emotion.level);
            PlayerPrefs.Save();
            Debug.Log($"Улучшена эмоция {emotion.type} до уровня {emotion.level}");
            
            // Обновляем тултип после улучшения
            if (emotionButtons.TryGetValue(emotionType, out EmotionButton button))
            {
                UpdateEmotionTooltip(emotionType, button);
            }
            
            // Добавляем визуальный эффект успешного улучшения
            if (upgradeProgressImages.TryGetValue(emotionType, out Image progressImage))
            {
                // Создаем эффект вспышки
                progressImage.color = Color.white;
                progressImage.fillAmount = 1;
                
                // Анимируем исчезновение через анимацию
                StartCoroutine(FadeOutProgressBar(progressImage));
            }
        }
        else
        {
            Debug.Log($"Эмоция {emotion.type} уже максимального уровня!");
        }
    }

    private System.Collections.IEnumerator FadeOutProgressBar(Image progressImage)
    {
        if (progressImage == null) yield break; // Проверка на null в начале

        float duration = 0.5f;
        float elapsed = 0;
        Color startColor = progressImage.color;
        Color endColor = new Color(1f, 0.8f, 0.2f, 0);
        
        while (elapsed < duration)
        {
            if (progressImage == null) yield break; // Проверка на null в цикле
            progressImage.color = Color.Lerp(startColor, endColor, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        if (progressImage != null) // Проверка на null перед финальным присвоением
        {
            progressImage.color = new Color(1f, 0.8f, 0.2f, 0.9f);
            progressImage.fillAmount = 0;
        }
    }

    public void UpgradeMaxActiveEmotions()
    {
        Debug.Log("[EmotionSystem] Попытка улучшить максимальное количество эмоций");
        int upgradeCost = GetUpgradeCost();
        Debug.Log($"[EmotionSystem] Стоимость улучшения: {upgradeCost}");
        
        if (!CurrencyManager.Instance.SpendCurrency(upgradeCost))
        {
            Debug.Log("[EmotionSystem] Недостаточно валюты для улучшения");
            return;
        }
        
        if (currentMaxEmotionsLevel < 6) // Максимум 6 уровней
        {
            maxActiveEmotions++;
            currentMaxEmotionsLevel++;
            PlayerPrefs.SetInt("MaxActiveEmotionsLevel", currentMaxEmotionsLevel);
            PlayerPrefs.Save();
            Debug.Log($"[EmotionSystem] Увеличено максимальное количество активных эмоций до {maxActiveEmotions}");
            UpdateUITexts();
        }
        else
        {
            Debug.Log("[EmotionSystem] Достигнут максимальный уровень!");
        }
    }

    private int GetUpgradeCost()
    {
        // Каждый следующий уровень стоит дороже
        return baseLevelUpCost * currentMaxEmotionsLevel;
    }

    private void UpdateUITexts()
    {
        if (levelText != null)
        {
            levelText.text = $"{currentMaxEmotionsLevel}/6";
        }

        if (levelUpText != null)
        {
            if (currentMaxEmotionsLevel < 6)
            {
                levelUpText.text = $"Просветиться\n({GetUpgradeCost()})";
                if (levelUpButton != null) levelUpButton.interactable = true;
            }
            else
            {
                levelUpText.text = "МАКС. УРОВЕНЬ";
                if (levelUpButton != null) levelUpButton.interactable = false;
            }
        }
    }

    public void OnPanelOpened()
    {
        Debug.Log("[EmotionSystem] Панель эмоций открыта, начинаем инициализацию UI");
        FindUIElements(); // На случай, если панель создаётся заново
        UpdateUITexts();
        InitializeUI(); // Инициализируем UI элементы
    }

    public bool IsEmotionActive(EmotionType emotionType)
    {
        Emotion emotion = emotions.Find(e => e.type == emotionType);
        return emotion?.isActive ?? false;
    }

    public int GetEmotionLevel(EmotionType emotionType)
    {
        Emotion emotion = emotions.Find(e => e.type == emotionType);
        return emotion?.level ?? 0;
    }

    // Получить описание эффекта для конкретной эмоции
    public string GetEmotionDescription(EmotionType type)
    {
        Emotion emotion = emotions.Find(e => e.type == type);
        if (emotion == null) return string.Empty;

        string effectDescription = emotionEffects[type][emotion.level - 1];
        string status = emotion.isActive ? "Активна" : "Неактивна";
        string result = $"{GetEmotionName(type)}\nУровень: {emotion.level}/3\nСтатус: {status}\n\nЭффект:\n{effectDescription}";

        // Добавляем стоимость улучшения и инструкцию, если не максимальный уровень
        if (emotion.level < 3)
        {
            int upgradeCost = CalculateEmotionUpgradeCost(emotion); // Используем новый метод
            result += $"\n\nСтоимость улучшения: {upgradeCost}";
            result += "\nУдерживайте кнопку для улучшения";
        }
        else
        {
            result += "\n\nМаксимальный уровень";
        }
        return result;
    }

    // Получить локализованное название эмоции
    private string GetEmotionName(EmotionType type)
    {
        switch (type)
        {
            case EmotionType.Determination: return "Решимость";
            case EmotionType.Compassion: return "Сострадание";
            case EmotionType.Fear: return "Страх";
            case EmotionType.Acceptance: return "Принятие";
            case EmotionType.Anger: return "Гнев";
            case EmotionType.Denial: return "Отрицание";
            default: return type.ToString();
        }
    }

    // Методы обработки боевых эффектов
    
    /// <summary>
    /// Модифицирует входящий урон с учетом активных эмоций
    /// </summary>
    public float ModifyIncomingDamage(float damage)
    {
        float modifiedDamage = damage;
        bool damageWasModified = false;

        foreach (var emotion in emotions)
        {
            if (!emotion.isActive) continue;

            switch (emotion.type)
            {
                case EmotionType.Fear:
                    // Шанс уклонения
                    float dodgeChance = 0.2f + (emotion.level - 1) * 0.05f; // 20/25/30%
                    if (UnityEngine.Random.value < dodgeChance)
                    {
                        Debug.Log($"[EmotionSystem] Страх сработал: уклонение от атаки (шанс {dodgeChance:P0})");
                        return 0;
                    }
                    break;

                case EmotionType.Anger:
                    // Увеличенный входящий урон
                    float healthPenalty = 0.10f - (emotion.level - 1) * 0.025f; // 10/8/5%
                    modifiedDamage *= (1 + healthPenalty);
                    Debug.Log($"[EmotionSystem] Гнев активен: входящий урон увеличен на {healthPenalty:P0}");
                    damageWasModified = true;
                    break;
            }
        }

        if (damageWasModified)
        {
            Debug.Log($"[EmotionSystem] Итоговый модифицированный урон: {modifiedDamage} (изначальный: {damage})");
        }

        return modifiedDamage;
    }

    /// <summary>
    /// Модифицирует исходящий урон с учетом активных эмоций
    /// </summary>
    public float ModifyOutgoingDamage(float damage)
    {
        float modifiedDamage = damage;
        bool damageWasModified = false;

        foreach (var emotion in emotions)
        {
            if (!emotion.isActive) continue;

            switch (emotion.type)
            {
                case EmotionType.Determination:
                    if (playerHealth != null && playerHealth.GetCurrentHealth() < playerHealth.GetMaxHealth() * 0.5f)
                    {
                        float determinationBonus = 0.10f + (emotion.level - 1) * 0.05f; // 10/15/20%
                        modifiedDamage *= (1 + determinationBonus);
                        Debug.Log($"[EmotionSystem] Решимость активна: урон увеличен на {determinationBonus:P0} (HP < 50%)");
                        damageWasModified = true;
                    }
                    break;

                case EmotionType.Fear:
                    float damagePenalty = 0.10f - (emotion.level - 1) * 0.025f; // 10/8/5%
                    modifiedDamage *= (1 - damagePenalty);
                    Debug.Log($"[EmotionSystem] Страх активен: урон уменьшен на {damagePenalty:P0}");
                    damageWasModified = true;
                    break;

                case EmotionType.Anger:
                    float angerBonus = 0.15f + (emotion.level - 1) * 0.05f; // 15/20/25%
                    modifiedDamage *= (1 + angerBonus);
                    Debug.Log($"[EmotionSystem] Гнев активен: урон увеличен на {angerBonus:P0}");
                    damageWasModified = true;
                    break;
            }
        }

        if (damageWasModified)
        {
            Debug.Log($"[EmotionSystem] Итоговый модифицированный урон: {modifiedDamage} (изначальный: {damage})");
        }

        return modifiedDamage;
    }

    /// <summary>
    /// Обрабатывает победу над врагом
    /// </summary>
    public void OnEnemyDefeated()
    {
        foreach (var emotion in emotions)
        {
            if (!emotion.isActive) continue;

            if (emotion.type == EmotionType.Compassion && playerHealth != null)
            {
                float healPercent = 0.03f + (emotion.level - 1) * 0.03f; // 3/6/9%
                float currentHealth = playerHealth.GetCurrentHealth();
                float healAmount = currentHealth * healPercent;
                
                // Округляем до ближайшего целого и гарантируем минимум 1 единицу восстановления здоровья
                healAmount = Mathf.Max(1f, Mathf.Round(healAmount));
                
                playerHealth.Heal(healAmount);
                Debug.Log($"[EmotionSystem] Сострадание: восстановлено {healAmount:F0} HP (округлено, минимум 1, {healPercent:P0} от текущего HP)");
            }
        }
    }

    /// <summary>
    /// Проверяет возможность игнорирования урона (Отрицание)
    /// </summary>
    public bool ShouldIgnoreDamage()
    {
        var denialEmotion = emotions.Find(e => e.type == EmotionType.Denial && e.isActive);
        if (denialEmotion != null)
        {
            // Проверяем, не превышен ли лимит игнорирований для текущей комнаты
            int maxIgnores = denialEmotion.level; // 1/2/3 игнорирования
            int currentIgnores = roomIgnoreCounters[EmotionType.Denial];
            
            if (currentIgnores < maxIgnores)
            {
                roomIgnoreCounters[EmotionType.Denial]++;
                Debug.Log($"[EmotionSystem] Отрицание: игнорирован {currentIgnores + 1}-й удар из {maxIgnores} возможных в этой комнате");
                return true;
            }
            else
            {
                Debug.Log($"[EmotionSystem] Отрицание: достигнут лимит игнорирований в этой комнате ({maxIgnores})");
            }
        }
        return false;
    }

    /// <summary>
    /// Проверяет возможность воскрешения (Принятие)
    /// </summary>
    public bool TryRevive()
    {
        var acceptanceEmotion = emotions.Find(e => e.type == EmotionType.Acceptance && e.isActive);
        if (acceptanceEmotion != null && !hasUsedReviveInCurrentRoom)
        {
            // Если PlayerHealth не найден, пробуем найти его
            if (playerHealth == null)
            {
                playerHealth = FindObjectOfType<PlayerHealth>();
                if (playerHealth == null)
                {
                    Debug.LogError("[EmotionSystem] Не найден компонент PlayerHealth! Воскрешение невозможно.");
                    return false;
                }
            }

            float reviveHealthPercent = 0.20f + (acceptanceEmotion.level - 1) * 0.15f; // 20/35/50%
            float maxHealth = playerHealth.GetMaxHealth();
            float reviveAmount = maxHealth * reviveHealthPercent;
            
            // Сначала устанавливаем минимальное здоровье, чтобы избежать состояния смерти
            playerHealth.SetHealth(1);
            // Затем добавляем оставшееся здоровье через Heal
            playerHealth.Heal(reviveAmount - 1);
            
            hasUsedReviveInCurrentRoom = true;
            Debug.Log($"[EmotionSystem] Принятие: воскрешение с {reviveHealthPercent:P0} HP ({reviveAmount:F1} единиц)");
            return true;
        }
        else if (acceptanceEmotion != null)
        {
            Debug.Log("[EmotionSystem] Принятие: воскрешение уже было использовано в этой комнате");
        }
        return false;
    }

    /// <summary>
    /// Сбрасывает счетчики при входе в новую комнату
    /// </summary>
    public void OnEnterNewRoom()
    {
        roomIgnoreCounters[EmotionType.Denial] = 0;
        hasUsedReviveInCurrentRoom = false;
        Debug.Log("[EmotionSystem] Вход в новую комнату: счетчики сброшены");
        
        // Выводим информацию о текущих активных эмоциях и их состоянии
        foreach (var emotion in emotions)
        {
            if (emotion.isActive)
            {
                if (emotion.type == EmotionType.Denial)
                {
                    Debug.Log($"[EmotionSystem] Отрицание (активно): счетчик игнорирований сброшен, доступно {emotion.level} игнорирований в новой комнате");
                }
                else if (emotion.type == EmotionType.Acceptance && !hasUsedReviveInCurrentRoom)
                {
                    Debug.Log($"[EmotionSystem] Принятие (активно): возможность воскрешения восстановлена");
                }
            }
        }
    }

    /// <summary>
    /// Возвращает суммарный модификатор скорости от активных эмоций.
    /// </summary>
    /// <returns>Модификатор скорости (например, 0.1f для +10%).</returns>
    public float GetEmotionSpeedModifier()
    {
        float modifier = 0f;
        // Логика расчета модификатора скорости
        if (IsEmotionActive(EmotionType.Determination))
        {
            Emotion determination = emotions.Find(e => e.type == EmotionType.Determination);
            if (determination != null)
            {
                modifier += (0.10f + (determination.level - 1) * 0.05f); // +10/15/20% скорости
            }
        }
        return modifier;
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
        // Тут можно добавить отписку от глобальных событий, если EmotionSystem на них подписывается
        // Например, если бы он подписывался на какое-то событие GameManager'а:
        // if (GameManager.Instance != null) 
        // { 
        //     GameManager.Instance.OnSomeGlobalEvent -= HandleGlobalEvent; 
        // }
    }
}
 