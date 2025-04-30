using UnityEngine;
using UnityEngine.UI;
using VIDE_Data;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement; // Добавляем для доступа к SceneManager

namespace BreakTheCycle.Dialogue
{
    public class MonologueManager : MonoBehaviour
    {
        [Header("UI Elements")]
        public GameObject monologuePanel;    // Панель с текстом монолога
        public TextMeshProUGUI monologueText;           // Компонент TextMeshProUGUI для отображения монолога
        
        // Добавляем событие
        public event System.Action<int> OnMonologueComplete;

        private VIDE_Assign monologueAssign;
        private bool isMonologueActive = false; // Флаг активности монолога
        private int currentMonologueID = -1; // ID текущего монолога
        private int currentCommentIndex = 0;

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

        void Start()
        {
            // Проверяем, что текущая сцена - "Reality" перед автоматическим запуском монолога с ID 0
            string currentScene = SceneManager.GetActiveScene().name;
            Debug.Log($"[MonologueManager] Start в сцене: {currentScene}");

            // Проверяем, находимся ли мы в сцене "Reality" и начало новой игры
            if (currentScene == "Reality" && PlayerPrefs.GetInt("IsNewGame", 0) == 1)
            {
                Debug.Log("[MonologueManager] Автоматический запуск начального монолога (ID 0) в сцене Reality");
                PlayMonologue(0);
                
                // Сбрасываем флаг новой игры после показа монолога
                PlayerPrefs.SetInt("IsNewGame", 0);
                PlayerPrefs.Save();
            }
            else
            {
                Debug.Log($"[MonologueManager] Автоматический монолог не запущен. Сцена: {currentScene}, IsNewGame: {PlayerPrefs.GetInt("IsNewGame", 0)}");
            }
        }

        void Update()
        {
            if (isMonologueActive && Input.GetMouseButtonDown(0))
            {
                if (VIDE_Data.VIDE_Data.nodeData != null && currentCommentIndex < VIDE_Data.VIDE_Data.nodeData.comments.Length - 1)
                {
                    currentCommentIndex++;
                    ShowCurrentComment();
                }
                else
                {
                    HideMonologue();
                }
            }
        }

        public void PlayMonologue(int monologueID)
        {
            Debug.Log("Попытка воспроизвести монолог с ID: " + monologueID);

            if (VIDE_Data.VIDE_Data.isActive)
            {
                Debug.Log("Завершаем предыдущий диалог");
                VIDE_Data.VIDE_Data.EndDialogue();
            }

            if (monologuePanel == null || monologueText == null)
            {
                Debug.LogError("Отсутствуют ссылки на UI компоненты!");
                return;
            }

            monologueAssign.overrideStartNode = monologueID;
            VIDE_Data.VIDE_Data.BeginDialogue(monologueAssign);
            currentMonologueID = monologueID;
            currentCommentIndex = 0;

            // Проверяем Extra Variable playOnce
            bool playOnce = false;
            if (VIDE_Data.VIDE_Data.nodeData != null && VIDE_Data.VIDE_Data.nodeData.extraVars != null && VIDE_Data.VIDE_Data.nodeData.extraVars.ContainsKey("playOnce"))
            {
                object val = VIDE_Data.VIDE_Data.nodeData.extraVars["playOnce"];
                if (val is bool)
                    playOnce = (bool)val;
                else if (val is string)
                    playOnce = (val as string).ToLower() == "true";
            }

            string playedKey = $"Monologue_{monologueID}_Played";
            if (playOnce && PlayerPrefs.GetInt(playedKey, 0) == 1)
            {
                Debug.Log($"Монолог {monologueID} уже был проигран ранее, не показываем повторно.");
                return;
            }

            if (VIDE_Data.VIDE_Data.nodeData != null)
            {
                Debug.Log("Данные узла получены, текст: " + VIDE_Data.VIDE_Data.nodeData.comments[0]);
                monologuePanel.SetActive(true);
                ShowCurrentComment();
                isMonologueActive = true;

                // Сохраняем флаг, что монолог был проигран
                if (playOnce)
                {
                    PlayerPrefs.SetInt(playedKey, 1);
                    PlayerPrefs.Save();
                }
            }
            else
            {
                Debug.LogError("Не удалось получить данные узла!");
            }
        }

        private void ShowCurrentComment()
        {
            if (VIDE_Data.VIDE_Data.nodeData != null && currentCommentIndex < VIDE_Data.VIDE_Data.nodeData.comments.Length)
            {
                monologueText.text = VIDE_Data.VIDE_Data.nodeData.comments[currentCommentIndex];
            }
        }

        private void HideMonologue()
        {
            int completedID = currentMonologueID; // Запоминаем ID перед сбросом

            monologuePanel.SetActive(false);
            VIDE_Data.VIDE_Data.EndDialogue();
            isMonologueActive = false;
            currentMonologueID = -1;
            currentCommentIndex = 0;

            // Вызываем событие после всех действий
            OnMonologueComplete?.Invoke(completedID);
            Debug.Log($"Monologue {completedID} completed and event invoked.");
        }

        // Добавляем метод для очистки при отключении объекта
        void OnDisable()
        {
            if (VIDE_Data.VIDE_Data.isActive)
            {
                VIDE_Data.VIDE_Data.EndDialogue();
            }
            isMonologueActive = false;
            currentMonologueID = -1;
        }
    }
} 