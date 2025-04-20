using UnityEngine;
using UnityEngine.UI;
using BreakTheCycle;

/// <summary>
/// Класс управления интерфейсом системы эмоций
/// </summary>
public class EmotionUI : MonoBehaviour
{
    [SerializeField] private Button[] emotionButtons;
    [SerializeField] private EmotionSystem.EmotionType[] emotionTypes;
    [SerializeField] private GameObject emotionPanel;

    private void Awake()
    {
        if (emotionPanel != null)
            emotionPanel.SetActive(false);
    }

    private void Start()
    {
        for (int i = 0; i < emotionButtons.Length; i++)
        {
            int index = i;
            emotionButtons[i].onClick.AddListener(() => OnEmotionButtonClicked(index));
        }
        UpdateUI();
    }

    private void OnEmotionButtonClicked(int index)
    {
        var type = emotionTypes[index];
        if (EmotionSystem.Instance.IsEmotionActive(type))
            EmotionSystem.Instance.DeactivateEmotion(type);
        else
            EmotionSystem.Instance.ActivateEmotion(type);

        UpdateUI();
    }

    private void UpdateUI()
    {
        for (int i = 0; i < emotionButtons.Length; i++)
        {
            var type = emotionTypes[i];
            bool isActive = EmotionSystem.Instance.IsEmotionActive(type);

            // Пример: меняем цвет кнопки
            var colors = emotionButtons[i].colors;
            colors.normalColor = isActive ? Color.green : Color.white;
            emotionButtons[i].colors = colors;
        }
    }

    public void Open()
    {
        if (emotionPanel != null)
            emotionPanel.SetActive(true);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void Close()
    {
        if (emotionPanel != null)
            emotionPanel.SetActive(false);
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public bool IsOpen()
    {
        return emotionPanel != null && emotionPanel.activeSelf;
    }

    private void Update()
    {
        if (IsOpen())
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.F))
            {
                Close();
                if (PlayerControlManager.Instance != null)
                    PlayerControlManager.Instance.SetControlsEnabled(true);
            }
        }
    }
} 