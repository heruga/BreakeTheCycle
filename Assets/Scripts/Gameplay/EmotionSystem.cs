using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.UI;
using TMPro;
using DungeonGeneration.Scripts.Health;

public class EmotionSystem : MonoBehaviour
{
    [Header("Цвета состояний")]
    public Color activeColor = new Color(0.2f, 0.8f, 0.2f); // Зеленый для активной эмоции
    public Color inactiveColor = Color.white; // Белый для неактивной эмоции

    [Header("Настройки улучшения")]
    public int baseLevelUpCost = 100; // Базовая стоимость улучшения
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
                "+10% к урону, если HP меньше 50%, +10% к скорости в бою с элитами",
                "+15% к урону, если HP меньше 50%, +15% к скорости в бою с элитами",
                "+20% к урону, если HP меньше 50%, +20% к скорости в бою с элитами"
            }
        },
        {
            EmotionType.Compassion,
            new string[]
            {
                "+5% HP после победы над врагом",
                "+10% HP после победы над врагом",
                "+15% HP после победы над врагом"
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
                "1 раз за комнату при смерти восстанавливает 20% HP",
                "1 раз за комнату при смерти восстанавливает 35% HP",
                "1 раз за комнату при смерти восстанавливает 50% HP"
            }
        },
        {
            EmotionType.Anger,
            new string[]
            {
                "+15% урона, -10% HP",
                "+20% урона, -8% HP",
                "+25% урона, -5% HP"
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

    private void Awake()
    {
        // Инициализируем список эмоций
        foreach (EmotionType type in Enum.GetValues(typeof(EmotionType)))
        {
            emotions.Add(new Emotion { type = type });
            roomIgnoreCounters[type] = 0;
        }
    }

    private void Start()
    {
        // Находим компоненты
        playerHealth = FindObjectOfType<PlayerHealth>();
        playerController = FindObjectOfType<ConsciousnessController>();
        
        if (playerHealth == null)
        {
            Debug.LogWarning("[EmotionSystem] PlayerHealth не найден при старте. Будет попытка найти его позже.");
        }
        
        FindUIElements();
        UpdateUITexts();
    }

    private void Update()
    {
        // Если PlayerHealth не найден, пробуем найти его снова
        if (playerHealth == null)
        {
            playerHealth = FindObjectOfType<PlayerHealth>();
        }
    }

    private void FindUIElements()
    {
        GameObject levelObj = GameObject.Find("Level");
        if (levelObj != null)
        {
            levelText = levelObj.GetComponent<TextMeshProUGUI>();
        }

        GameObject levelUpObj = GameObject.Find("LevelUp");
        if (levelUpObj != null)
        {
            levelUpButton = levelUpObj.GetComponent<Button>();
            // Ищем текст внутри кнопки
            levelUpText = levelUpObj.GetComponentInChildren<TextMeshProUGUI>();
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

    private void ToggleEmotion(EmotionType emotionType)
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
    }

    private void DeactivateEmotion(Emotion emotion)
    {
        emotion.isActive = false;
        currentActiveEmotions--;
        Debug.Log($"Деактивирована эмоция: {emotion.type}");
        UpdateEmotionVisual(emotion.type, false);
    }

    private void UpdateEmotionVisual(EmotionType emotionType, bool isActive)
    {
        string objectName = emotionType.ToString();
        GameObject emotionObject = GameObject.Find(objectName);
        
        if (emotionObject != null)
        {
            // Пробуем найти Image компонент
            Image imageComponent = emotionObject.GetComponent<Image>();
            if (imageComponent != null)
            {
                imageComponent.color = isActive ? activeColor : inactiveColor;
            }

            // Также проверяем, есть ли Image в дочерних объектах
            Image[] childImages = emotionObject.GetComponentsInChildren<Image>();
            foreach (Image img in childImages)
            {
                img.color = isActive ? activeColor : inactiveColor;
            }
        }
        else
        {
            Debug.LogWarning($"Не найден объект с именем {objectName}");
        }
    }

    public void UpgradeEmotion(EmotionType emotionType)
    {
        Emotion emotion = emotions.Find(e => e.type == emotionType);
        if (emotion == null) return;

        if (emotion.level < 3)
        {
            emotion.level++;
            Debug.Log($"Улучшена эмоция {emotion.type} до уровня {emotion.level}");
        }
        else
        {
            Debug.Log($"Эмоция {emotion.type} уже максимального уровня!");
        }
    }

    public void UpgradeMaxActiveEmotions()
    {
        int upgradeCost = GetUpgradeCost();
        // Здесь должна быть проверка наличия достаточного количества ресурсов у игрока
        // if (playerResources >= upgradeCost) 
        
        if (currentMaxEmotionsLevel < 6) // Максимум 6 уровней
        {
            maxActiveEmotions++;
            currentMaxEmotionsLevel++;
            Debug.Log($"Увеличено максимальное количество активных эмоций до {maxActiveEmotions}");
            // Здесь должно быть списание ресурсов
            // playerResources -= upgradeCost;
            UpdateUITexts();
        }
        else
        {
            Debug.Log("Достигнут максимальный уровень!");
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
            levelText.text = $"Уровень: {currentMaxEmotionsLevel}/6";
        }

        if (levelUpText != null)
        {
            if (currentMaxEmotionsLevel < 6)
            {
                levelUpText.text = $"Повысить уровень ({GetUpgradeCost()})";
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
        FindUIElements(); // На случай, если панель создаётся заново
        UpdateUITexts();
        // Обновляем цвета всех эмоций
        foreach (Emotion emotion in emotions)
        {
            UpdateEmotionVisual(emotion.type, emotion.isActive);
        }
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
        return $"{GetEmotionName(type)}\nУровень: {emotion.level}/3\nСтатус: {status}\n\nЭффект:\n{effectDescription}";
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
                float healPercent = 0.05f + (emotion.level - 1) * 0.05f; // 5/10/15%
                float healAmount = playerHealth.GetMaxHealth() * healPercent;
                playerHealth.Heal(healAmount);
                Debug.Log($"[EmotionSystem] Сострадание: восстановлено {healPercent:P0} HP ({healAmount} единиц)");
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
}
