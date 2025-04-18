using UnityEngine;
using UnityEngine.UI;
using VIDE_Data;
using System.Collections;

public class MonologueManager : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject monologuePanel;    // Панель с текстом монолога
    public Text monologueText;           // Компонент Text для отображения монолога
    
    [Header("Settings")]
    public float displayTime = 3f;       // Время показа монолога в секундах
    
    private VIDE_Assign monologueAssign;
    private Coroutine hideCoroutine; // Добавляем переменную для отслеживания корутины

    void Awake()
    {
        Debug.Log("MonologueManager: Awake");
        monologueAssign = gameObject.GetComponent<VIDE_Assign>();
        if (monologueAssign == null)
        {
            monologueAssign = gameObject.AddComponent<VIDE_Assign>();
            Debug.Log("VIDE_Assign добавлен");
        }

        monologueAssign.AssignNew("PlayerMonologues");
        Debug.Log("Диалог назначен: " + monologueAssign.assignedDialogue);
        
        if (monologuePanel != null)
            monologuePanel.SetActive(false);
    }

    public void PlayMonologue(int monologueID)
    {
        Debug.Log("Попытка воспроизвести монолог с ID: " + monologueID);

        if (VD.isActive)
        {
            Debug.Log("Завершаем предыдущий диалог");
            VD.EndDialogue();
        }

        if (monologuePanel == null || monologueText == null)
        {
            Debug.LogError("Отсутствуют ссылки на UI компоненты!");
            return;
        }

        monologueAssign.overrideStartNode = monologueID;
        VD.BeginDialogue(monologueAssign);
        
        if (VD.nodeData != null)
        {
            Debug.Log("Данные узла получены, текст: " + VD.nodeData.comments[0]);
            monologuePanel.SetActive(true);
            monologueText.text = VD.nodeData.comments[0];
            hideCoroutine = StartCoroutine(HideMonologueAfterDelay());
        }
        else
        {
            Debug.LogError("Не удалось получить данные узла!");
        }
    }

    private IEnumerator HideMonologueAfterDelay()
    {
        yield return new WaitForSeconds(displayTime);
        monologuePanel.SetActive(false);
        VD.EndDialogue();
        hideCoroutine = null; // Очищаем ссылку на корутину
    }

    // Добавляем метод для очистки при отключении объекта
    void OnDisable()
    {
        if (VD.isActive)
        {
            VD.EndDialogue();
        }
        
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }
    }
} 