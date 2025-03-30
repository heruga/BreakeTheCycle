using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class InteractionPromptUI : MonoBehaviour
{
    [Header("UI Elements")]
    public Image backgroundImage;
    public Image keyIcon;
    
    [Header("Animation Settings")]
    [SerializeField] private float fadeDuration = 0.3f;
    [SerializeField] private float scaleDuration = 0.2f;
    
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    
    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
        
        // Начальное состояние
        canvasGroup.alpha = 0f;
        rectTransform.localScale = Vector3.zero;
    }
    
    public void ShowPrompt(string text)
    {
        // Анимация появления
        canvasGroup.DOFade(1f, fadeDuration);
        rectTransform.DOScale(Vector3.one, scaleDuration).SetEase(Ease.OutBack);
    }
    
    public void HidePrompt()
    {
        // Анимация исчезновения
        canvasGroup.DOFade(0f, fadeDuration);
        rectTransform.DOScale(Vector3.zero, scaleDuration).SetEase(Ease.InBack);
    }

    private void OnDestroy()
    {
        // Очищаем все анимации DOTween
        DOTween.Kill(canvasGroup);
        DOTween.Kill(rectTransform);
    }
} 