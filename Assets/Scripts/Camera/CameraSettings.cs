using UnityEngine;

[CreateAssetMenu(fileName = "CameraSettings", menuName = "BreakTheCycle/Camera Settings")]
public class CameraSettings : ScriptableObject
{
    [Header("Основные настройки")]
    [Tooltip("Высота камеры над игроком")]
    public float cameraHeight = 15f;
    
    [Tooltip("Угол наклона камеры (X-ось)")]
    public float cameraAngleX = 45f;
    
    [Tooltip("Угол поворота камеры (Y-ось)")]
    public float cameraAngleY = 45f;
    
    [Tooltip("Скорость перемещения камеры")]
    public float cameraSmoothSpeed = 3.5f;
    
    [Tooltip("Стандартное смещение камеры относительно игрока")]
    public Vector3 cameraOffset = new Vector3(0f, 15f, -15f);

    [Header("Динамическое смещение")]
    [Tooltip("Включить смещение камеры в направлении движения")]
    public bool adjustCameraDirection = true;
    
    [Tooltip("Сила смещения камеры в направлении движения")]
    public float directionAdjustmentStrength = 5f;
    
    [Tooltip("Время возврата камеры в стандартное положение после остановки игрока")]
    public float cameraReturnTime = 2.2f;
    
    [Tooltip("Сила демпфирования для плавного торможения камеры")]
    public float cameraDampingStrength = 0.85f;
    
    [Tooltip("Скорость реакции на изменение направления движения")]
    public float directionChangeResponsiveness = 1.2f;
    
    [Tooltip("Максимальное расстояние опережения камеры")]
    public float maxLookAheadDistance = 7f;

    [Header("Эффекты")]
    [Tooltip("Применить эффект 'пружины' к движению камеры")]
    public bool useSpringEffect = true;
    
    [Tooltip("Порог для определения резкого изменения направления")]
    public float directionChangeDotThreshold = 0.4f;
    
    [Tooltip("Время сглаживания резкого изменения направления")]
    public float abruptChangeSmoothing = 0.8f;
} 