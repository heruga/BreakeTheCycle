using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Компонент управления кнопкой эмоции в интерфейсе
/// </summary>
public class EmotionButtonUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Image levelIndicator;
    
    [Header("Colors")]
    [SerializeField] private Color inactiveColor = new Color(0.3f, 0.3f, 0.3f);
    [SerializeField] private Color activeColor = new Color(0.3f, 0.7f, 0.3f);
    [SerializeField] private Color selectedColor = new Color(0.7f, 0.7f, 0.3f);
    [SerializeField] private Color maxLevelColor = new Color(0.7f, 0.3f, 0.7f);
    
    private bool isSelected = false;
    private bool isActive = false;
    
    public Button Button => button;
    public EmotionSystem.EmotionType EmotionType { get; private set; }
    
    /// <summary>
    /// Инициализация кнопки эмоции
    /// </summary>
    public void Initialize(EmotionSystem.EmotionType emotionType)
    {
        EmotionType = emotionType;
        
        // Устанавливаем название эмоции
        nameText.text = EmotionSystem.GetEmotionName(emotionType);
        
        // Задаем начальное состояние
        SetActive(false);
        SetSelected(false);
        UpdateLevel(0);
    }
    
    /// <summary>
    /// Устанавливает активное состояние кнопки
    /// </summary>
    public void SetActive(bool active)
    {
        isActive = active;
        UpdateVisual();
    }
    
    /// <summary>
    /// Устанавливает выбранное состояние кнопки
    /// </summary>
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        UpdateVisual();
    }
    
    /// <summary>
    /// Обновляет уровень эмоции на кнопке
    /// </summary>
    public void UpdateLevel(int level)
    {
        // Обновляем индикатор уровня (заполнение от 0 до 1)
        float fillAmount = level / (float)EmotionSystem.MAX_EMOTION_LEVEL;
        levelIndicator.fillAmount = fillAmount;
        
        // Если максимальный уровень, выделяем индикатор
        if (level >= EmotionSystem.MAX_EMOTION_LEVEL)
        {
            levelIndicator.color = maxLevelColor;
        }
        else
        {
            levelIndicator.color = Color.white;
        }
        
        UpdateVisual();
    }
    
    /// <summary>
    /// Обновляет визуальное отображение кнопки
    /// </summary>
    private void UpdateVisual()
    {
        // Устанавливаем цвет в зависимости от состояния
        if (isSelected)
        {
            backgroundImage.color = selectedColor;
        }
        else if (isActive)
        {
            backgroundImage.color = activeColor;
        }
        else
        {
            backgroundImage.color = inactiveColor;
        }
    }
} 