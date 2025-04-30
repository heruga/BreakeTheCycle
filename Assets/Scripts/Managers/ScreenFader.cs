using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ScreenFader : MonoBehaviour
{
    private static ScreenFader _instance;
    public static ScreenFader Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("ScreenFader");
                _instance = go.AddComponent<ScreenFader>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private Canvas canvas;
    private Image fadeImage;
    private Coroutine currentFade;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        CreateFadeCanvas();
    }

    private void CreateFadeCanvas()
    {
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        fadeImage = new GameObject("FadeImage").AddComponent<Image>();
        fadeImage.transform.SetParent(canvas.transform, false);
        fadeImage.rectTransform.anchorMin = Vector2.zero;
        fadeImage.rectTransform.anchorMax = Vector2.one;
        fadeImage.rectTransform.offsetMin = Vector2.zero;
        fadeImage.rectTransform.offsetMax = Vector2.zero;
        fadeImage.raycastTarget = false;
        fadeImage.color = Color.clear;
    }

    public void SetColor(Color color)
    {
        if (fadeImage != null)
            fadeImage.color = color;
    }

    public Coroutine StartFadeIn(float duration, Color color)
    {
        // if (currentFade != null) StopCoroutine(currentFade);
        currentFade = StartCoroutine(FadeRoutine(1, 0, duration, color));
        return currentFade;
    }

    public Coroutine FadeOut(float duration, Color color)
    {
        // if (currentFade != null) StopCoroutine(currentFade);
        currentFade = StartCoroutine(FadeRoutine(0, 1, duration, color));
        return currentFade;
    }

    private IEnumerator FadeRoutine(float from, float to, float duration, Color color)
    {
        fadeImage.color = new Color(color.r, color.g, color.b, from);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float alpha = Mathf.Lerp(from, to, t);
            fadeImage.color = new Color(color.r, color.g, color.b, alpha);
            yield return null;
        }
        fadeImage.color = new Color(color.r, color.g, color.b, to);
    }
} 