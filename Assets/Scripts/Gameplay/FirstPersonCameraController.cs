using UnityEngine;

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
} 