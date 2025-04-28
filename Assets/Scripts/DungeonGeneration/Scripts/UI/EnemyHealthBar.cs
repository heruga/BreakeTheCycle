using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    [SerializeField] private Image fillImage; // Сюда перетащи Image полоски (Fill)
    [SerializeField] private Vector3 offset = new Vector3(0, 2f, 0); // Стандартное смещение, если не найден коллайдер

    private Transform target;

    /// Установить цель (врага), над которым будет healthbar
    public void SetTarget(Transform targetTransform)
    {
        target = targetTransform;

        // Автоматически вычисляем высоту по коллайдеру
        Collider col = target.GetComponentInChildren<Collider>();
        if (col != null)
        {
            offset = new Vector3(0, col.bounds.size.y + 0.3f, 0); // 0.3f — небольшой запас над головой
        }
    }

    /// Установить процент здоровья (0..1)
    public void SetHealth(float percent)
    {
        UnityEngine.Debug.Log($"[EnemyHealthBar] SetHealth called with percent: {percent}");
        if (fillImage == null)
        {
            UnityEngine.Debug.LogError("[EnemyHealthBar] fillImage is null!");
            return;
        }
        fillImage.fillAmount = Mathf.Clamp01(percent);
        UnityEngine.Debug.Log($"[EnemyHealthBar] fillImage.fillAmount is now: {fillImage.fillAmount}");
    }

    private void LateUpdate()
    {
        if (target != null)
        {
            // Следуем за целью и всегда смотрим на камеру
            transform.position = target.position + offset;
            if (Camera.main != null)
                transform.forward = Camera.main.transform.forward;
        }
    }
}
