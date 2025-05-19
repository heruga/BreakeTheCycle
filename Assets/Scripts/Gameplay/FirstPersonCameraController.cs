using UnityEngine;
using BreakTheCycle;

/// <summary>
/// Управление камерой игрока в режиме от первого лица
/// </summary>
public class FirstPersonCameraController : MonoBehaviour
{
    [Header("Настройки камеры")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float verticalRotationLimit = 80f;
    [SerializeField] private float rotationSmoothSpeed = 15f;
    [SerializeField] private Transform playerBody; // Ссылка на объект игрока

    private float targetHorizontalRotation = 0f;
    private float targetVerticalRotation = 0f;
    private Vector3 currentEulerAngles;
    private float currentVerticalRotation;

    private void Awake()
    {
        if (playerBody == null && transform.parent != null)
        {
            playerBody = transform.parent;
        }
        currentEulerAngles = playerBody != null ? playerBody.eulerAngles : Vector3.zero;
        targetHorizontalRotation = currentEulerAngles.y;
    }

    private void Update()
    {
        // Проверяем, включено ли управление игроком
        if (PlayerControlManager.Instance != null && !PlayerControlManager.Instance.ControlsEnabled)
        {
            // Если управление отключено (например, во время осмотра или в меню),
            // не обрабатываем ввод мыши для вращения.
            return; 
        }

        if (PlayerControlManager.Instance != null && PlayerControlManager.Instance.JustBecameEnabledThisFrame)
        {
            // Пропускаем обработку мыши в кадре, когда управление только что включилось,
            // чтобы избежать рывка от последнего положения курсора на UI.
            // Синхронизируем углы, чтобы сбросить любые дельты, которые могли накопиться.
            SyncRotationWithTransform(); 
            return; 
        }

        // Получаем ввод мыши и масштабируем его
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Обновляем целевые углы поворота
        targetHorizontalRotation += mouseX;
        targetVerticalRotation = Mathf.Clamp(targetVerticalRotation - mouseY, -verticalRotationLimit, verticalRotationLimit);

        // Плавно интерполируем текущие углы к целевым
        if (playerBody != null)
        {
            currentEulerAngles.y = Mathf.LerpAngle(currentEulerAngles.y, targetHorizontalRotation, Time.deltaTime * rotationSmoothSpeed);
            currentVerticalRotation = Mathf.Lerp(currentVerticalRotation, targetVerticalRotation, Time.deltaTime * rotationSmoothSpeed);

            playerBody.eulerAngles = currentEulerAngles;
            transform.localRotation = Quaternion.Euler(currentVerticalRotation, 0f, 0f);
        }
    }

    public void SyncRotationWithTransform()
    {
        if (playerBody != null)
        {
            targetHorizontalRotation = playerBody.eulerAngles.y;
        }
        else
        {
            // Резервный вариант, если playerBody отсутствует (в идеале не должно происходить)
            targetHorizontalRotation = transform.eulerAngles.y;
        }

        float currentLocalX = transform.localEulerAngles.x;
        // Преобразуем угол X из диапазона [0, 360) в диапазон, близкий к [-180, 180)
        if (currentLocalX > 180f)
        {
            currentLocalX -= 360f;
        }
        targetVerticalRotation = Mathf.Clamp(currentLocalX, -verticalRotationLimit, verticalRotationLimit);

        // Обновляем также 'current' значения, чтобы сделать синхронизацию немедленной
        // и избежать интерполяции от старых 'current' к новым 'target' значениям.
        if (playerBody != null) // currentEulerAngles относится к playerBody
        {
            currentEulerAngles.y = targetHorizontalRotation;
        }
        currentVerticalRotation = targetVerticalRotation; // currentVerticalRotation относится к камере
    }

    // --- Публичные методы для GameManager ---
    public float GetVerticalRotation()
    {
        return targetVerticalRotation;
    }

    public void SetVerticalRotation(float angle)
    {
        // Устанавливаем целевой угол, учитывая ограничения
        targetVerticalRotation = Mathf.Clamp(angle, -verticalRotationLimit, verticalRotationLimit);
        // Сразу применяем к текущему углу, чтобы избежать скачка при следующем Update
        currentVerticalRotation = targetVerticalRotation;
        // Применяем к transform камеры немедленно
        transform.localEulerAngles = new Vector3(currentVerticalRotation, 0, 0);
        Debug.Log($"[FirstPersonCameraController] SetVerticalRotation: Установлен угол {angle}, результат targetVerticalRotation: {targetVerticalRotation}");
    }
    // --- Конец публичных методов ---
} 