using UnityEngine;
using DG.Tweening;

namespace BreakTheCycle.Gameplay
{
    /// <summary>
    /// Компонент для создания анимации парения объекта.
    /// Объект будет плавно подниматься и опускаться в бесконечном цикле.
    /// </summary>
    public class FloatingAnimation : MonoBehaviour
    {
        [Header("Настройки парения")]
        [SerializeField] private float floatHeight = 0.5f; // Высота парения
        [SerializeField] private float floatDuration = 2f; // Длительность одного цикла (вверх-вниз)
        [SerializeField] private Ease floatEase = Ease.InOutSine; // Тип анимации
        [SerializeField] private bool startOnAwake = true; // Начинать анимацию сразу
        
        [Header("Дополнительные настройки")]
        [SerializeField] private bool randomStartOffset = true; // Случайное смещение начала анимации
        [SerializeField] private Vector3 floatDirection = Vector3.up; // Направление парения
        
        private Vector3 startPosition;
        private Tween floatTween;
        
        private void Awake()
        {
            // Сохраняем начальную позицию
            startPosition = transform.position;
            
            if (startOnAwake)
            {
                StartFloating();
            }
        }
        
        /// <summary>
        /// Запускает анимацию парения
        /// </summary>
        public void StartFloating()
        {
            // Останавливаем предыдущую анимацию, если она есть
            StopFloating();
            
            // Нормализуем направление парения
            Vector3 normalizedDirection = floatDirection.normalized;
            
            // Вычисляем конечную позицию
            Vector3 targetPosition = startPosition + normalizedDirection * floatHeight;
            
            // Создаем последовательность анимации
            Sequence floatSequence = DOTween.Sequence();
            
            // Если нужно случайное смещение, начинаем с рандомной позиции в цикле
            if (randomStartOffset)
            {
                float randomOffset = Random.Range(0f, 1f);
                floatSequence.AppendInterval(randomOffset * floatDuration);
            }
            
            // Движение вверх
            floatSequence.Append(transform.DOMove(targetPosition, floatDuration / 2f).SetEase(floatEase));
            
            // Движение вниз
            floatSequence.Append(transform.DOMove(startPosition, floatDuration / 2f).SetEase(floatEase));
            
            // Зацикливаем анимацию
            floatSequence.SetLoops(-1, LoopType.Restart);
            
            // Сохраняем ссылку на анимацию
            floatTween = floatSequence;
        }
        
        /// <summary>
        /// Останавливает анимацию парения
        /// </summary>
        public void StopFloating()
        {
            if (floatTween != null && floatTween.IsActive())
            {
                floatTween.Kill();
                floatTween = null;
            }
        }
        
        /// <summary>
        /// Приостанавливает анимацию парения
        /// </summary>
        public void PauseFloating()
        {
            if (floatTween != null && floatTween.IsActive())
            {
                floatTween.Pause();
            }
        }
        
        /// <summary>
        /// Возобновляет анимацию парения
        /// </summary>
        public void ResumeFloating()
        {
            if (floatTween != null && floatTween.IsActive())
            {
                floatTween.Play();
            }
        }
        
        /// <summary>
        /// Возвращает объект в исходную позицию
        /// </summary>
        public void ResetToStartPosition()
        {
            StopFloating();
            transform.position = startPosition;
        }
        
        /// <summary>
        /// Обновляет начальную позицию (полезно, если объект был перемещен)
        /// </summary>
        public void UpdateStartPosition()
        {
            startPosition = transform.position;
        }
        
        private void OnDestroy()
        {
            // Убираем анимацию при уничтожении объекта
            StopFloating();
        }
        
        private void OnDisable()
        {
            // Приостанавливаем анимацию при отключении объекта
            PauseFloating();
        }
        
        private void OnEnable()
        {
            // Возобновляем анимацию при включении объекта
            if (floatTween != null)
            {
                ResumeFloating();
            }
            else if (startOnAwake)
            {
                StartFloating();
            }
        }
        
        // Для отладки в редакторе
        private void OnDrawGizmosSelected()
        {
            Vector3 currentStart = Application.isPlaying ? startPosition : transform.position;
            Vector3 normalizedDirection = floatDirection.normalized;
            Vector3 targetPos = currentStart + normalizedDirection * floatHeight;
            
            // Рисуем линию парения
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(currentStart, targetPos);
            
            // Рисуем точки начала и конца
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(currentStart, 0.1f);
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(targetPos, 0.1f);
        }
    }
} 