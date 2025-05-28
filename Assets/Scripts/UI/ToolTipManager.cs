using UnityEngine;
using TMPro;
using System.Collections;

public class ToolTipManager : MonoBehaviour
{
    public static ToolTipManager Instance { get; private set; }
    
    [SerializeField] private TextMeshProUGUI tipText;
    [SerializeField] private RectTransform tipWindow;
    [SerializeField] private Canvas parentCanvas; // Канвас, на котором находится тултип
    
    private Coroutine updateTooltipCoroutine;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        
        // Если канвас не назначен, попробуем найти его автоматически
        if (parentCanvas == null)
        {
            parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas == null)
            {
                Debug.LogWarning("[ToolTipManager] Не удалось найти родительский Canvas. Позиционирование тултипа может работать некорректно.");
            }
        }
        
        HideTooltip();
    }
    
    public void ShowTooltip(string tip, Vector2 mousePos)
    {
        // Останавливаем предыдущую корутину, если она запущена
        if (updateTooltipCoroutine != null)
        {
            StopCoroutine(updateTooltipCoroutine);
        }
        
        // Запускаем новую корутину для обновления тултипа
        updateTooltipCoroutine = StartCoroutine(UpdateTooltipCoroutine(tip, mousePos));
    }
    
    private IEnumerator UpdateTooltipCoroutine(string tip, Vector2 mousePos)
    {
        // Сначала задаем текст
        tipText.text = tip;
        
        // Активируем окно для правильного расчета размеров
        tipWindow.gameObject.SetActive(true);
        
        // Ждем один кадр для корректного расчета размеров текста
        yield return null;
        Canvas.ForceUpdateCanvases();
        
        // Устанавливаем размер окна на основе размера текста
        float width = tipText.preferredWidth + 20; // Добавляем отступы
        float height = tipText.preferredHeight + 10; // Добавляем отступы
        tipWindow.sizeDelta = new Vector2(width > 200 ? 200 : width, height);
        
        // Еще раз обновляем канвас
        Canvas.ForceUpdateCanvases();
        yield return null;
        
        // Позиционируем тултип так, чтобы курсор мыши был в верхнем левом углу
        if (parentCanvas != null)
        {
            // Преобразуем позицию мыши в локальные координаты канваса
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentCanvas.transform as RectTransform,
                mousePos,
                parentCanvas.worldCamera,
                out Vector2 localPoint
            );
            
            // Устанавливаем позицию тултипа так, чтобы курсор был в верхнем левом углу
            // Учитываем, что pivot тултипа по умолчанию в центре (0.5, 0.5)
            float offsetX = tipWindow.sizeDelta.x * 0.5f; // Половина ширины
            float offsetY = tipWindow.sizeDelta.y * 0.5f; // Половина высоты
            
            // Смещаем тултип вправо и вниз от курсора
            tipWindow.localPosition = new Vector2(
                localPoint.x + offsetX, 
                localPoint.y - offsetY
            );
        }
        else
        {
            // Запасной вариант, если канвас не найден
            tipWindow.position = new Vector2(
                mousePos.x + tipWindow.sizeDelta.x * 0.5f,
                mousePos.y - tipWindow.sizeDelta.y * 0.5f
            );
        }
    }
    
    public void HideTooltip()
    {
        // Останавливаем корутину, если она запущена
        if (updateTooltipCoroutine != null)
        {
            StopCoroutine(updateTooltipCoroutine);
            updateTooltipCoroutine = null;
        }
        
        tipText.text = "";
        tipWindow.gameObject.SetActive(false);
    }
    
    private void OnDestroy()
    {
        // Останавливаем все корутины при уничтожении объекта
        StopAllCoroutines();
    }
}
