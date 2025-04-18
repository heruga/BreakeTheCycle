using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Вспомогательный класс для создания UI валюты через код
/// </summary>
public class CurrencyPrefab : MonoBehaviour
{
    [SerializeField] private GameObject currencyCanvas;
    [SerializeField] private TextMeshProUGUI currencyText;
    [SerializeField] private Image currencyIcon;
    [SerializeField] private Sprite defaultCurrencySprite;
    
    /// <summary>
    /// Создает и настраивает UI валюты
    /// </summary>
    public static void CreateCurrencyUI()
    {
        if (CurrencyManager.Instance == null)
        {
            Debug.LogError("[CurrencyPrefab] CurrencyManager не найден!");
            return;
        }
        
        // Проверяем, есть ли уже UI валюты
        var existingUI = FindObjectOfType<CurrencyUI>();
        if (existingUI != null)
        {
            Debug.Log("[CurrencyPrefab] UI валюты уже существует");
            return;
        }
        
        // Создаем канву, если ее нет
        Canvas mainCanvas = FindObjectOfType<Canvas>();
        if (mainCanvas == null)
        {
            GameObject canvasGO = new GameObject("GameCanvas");
            mainCanvas = canvasGO.AddComponent<Canvas>();
            mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }
        
        // Создаем панель валюты
        GameObject currencyPanel = new GameObject("CurrencyPanel");
        currencyPanel.transform.SetParent(mainCanvas.transform, false);
        
        RectTransform panelRect = currencyPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1, 1);
        panelRect.anchorMax = new Vector2(1, 1);
        panelRect.pivot = new Vector2(1, 1);
        panelRect.anchoredPosition = new Vector2(-20, -20);
        panelRect.sizeDelta = new Vector2(200, 60);
        
        // Добавляем фон
        Image panelBg = currencyPanel.AddComponent<Image>();
        panelBg.color = new Color(0, 0, 0, 0.6f);
        
        // Создаем иконку валюты
        GameObject iconGO = new GameObject("CurrencyIcon");
        iconGO.transform.SetParent(currencyPanel.transform, false);
        
        RectTransform iconRect = iconGO.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0, 0.5f);
        iconRect.anchorMax = new Vector2(0, 0.5f);
        iconRect.pivot = new Vector2(0, 0.5f);
        iconRect.anchoredPosition = new Vector2(10, 0);
        iconRect.sizeDelta = new Vector2(40, 40);
        
        Image iconImage = iconGO.AddComponent<Image>();
        // Загружаем спрайт из ресурсов или используем дефолтный
        Sprite currencySprite = Resources.Load<Sprite>("UI/CurrencyIcon");
        iconImage.sprite = currencySprite ? currencySprite : CreateDefaultSprite();
        
        // Создаем текст валюты
        GameObject textGO = new GameObject("CurrencyText");
        textGO.transform.SetParent(currencyPanel.transform, false);
        
        RectTransform textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0, 0.5f);
        textRect.anchorMax = new Vector2(1, 0.5f);
        textRect.pivot = new Vector2(0, 0.5f);
        textRect.anchoredPosition = new Vector2(60, 0);
        textRect.sizeDelta = new Vector2(-70, 40);
        
        TextMeshProUGUI tmpText = textGO.AddComponent<TextMeshProUGUI>();
        tmpText.text = "0";
        tmpText.fontSize = 28;
        tmpText.color = Color.white;
        tmpText.alignment = TextAlignmentOptions.Left;
        
        // Добавляем компонент для анимации
        Animator animator = currencyPanel.AddComponent<Animator>();
        // Здесь можно создать и назначить RuntimeAnimatorController
        
        // Добавляем компонент CurrencyUI
        CurrencyUI currencyUI = currencyPanel.AddComponent<CurrencyUI>();
        currencyUI.GetComponent<CurrencyUI>().enabled = true;
        
        // Задаем ссылки
        var fields = currencyUI.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        foreach (var field in fields)
        {
            if (field.Name == "currencyText")
                field.SetValue(currencyUI, tmpText);
            else if (field.Name == "currencyIcon")
                field.SetValue(currencyUI, iconImage);
            else if (field.Name == "currencyAnimator")
                field.SetValue(currencyUI, animator);
        }
    }
    
    /// <summary>
    /// Создает простой спрайт для иконки валюты
    /// </summary>
    private static Sprite CreateDefaultSprite()
    {
        Texture2D texture = new Texture2D(64, 64);
        Color[] colors = new Color[64 * 64];
        
        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(32, 32));
                if (distance < 30)
                {
                    colors[y * 64 + x] = new Color(1f, 0.8f, 0.2f, 1); // Золотистый цвет
                }
                else
                {
                    colors[y * 64 + x] = new Color(0, 0, 0, 0);
                }
            }
        }
        
        texture.SetPixels(colors);
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
    }
} 