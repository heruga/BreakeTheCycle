using UnityEngine;
using UnityEngine.UI;
using VIDE_Data;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement; // Добавляем для доступа к SceneManager
using UnityEngine.EventSystems; // <--- ДОБАВЛЕНО

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
        
        // Публичное свойство для проверки активности монолога
        public bool IsMonologueActive => isMonologueActive;

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
            // НОВАЯ ПРОВЕРКА: Если игра на паузе, ничего не делаем
            if (PauseMenuUI.IsPaused) // Убедитесь, что PauseMenuUI доступен (может потребоваться using)
            {
                return;
            }

            if (isMonologueActive && Input.GetMouseButtonDown(0))
            {
                if (EventSystem.current.IsPointerOverGameObject())
                {
                    // Если клик был над UI элементом (не диалоговым/монологовым),
                    // то не продвигаем монолог. Это должно помочь с кликами на другие UI.
                    return;
                }

                if (VIDE_Data.VIDE_Data.nodeData != null && VIDE_Data.VIDE_Data.nodeData.comments != null && currentCommentIndex < VIDE_Data.VIDE_Data.nodeData.comments.Length - 1)
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
            
            // УПРАВЛЕНИЕ ИГРОКОМ НЕ БЛОКИРУЕТСЯ ДЛЯ МОНОЛОГОВ
            if (PlayerControlManager.Instance != null)
            {
                PlayerControlManager.Instance.SetUIControlsBlock(false); 
                Debug.Log("[MonologueManager] SetUIControlsBlock(false) called for monologue start.");
            }

            // УПРАВЛЕНИЕ КУРСОРОМ: Скрыть курсор
            if (CursorManager.Instance != null)
            {
                CursorManager.Instance.SetGameplayState();
                Debug.Log("[MonologueManager] Cursor set to Gameplay state at monologue start.");
            }
            else
            {
                Debug.LogWarning("[MonologueManager] CursorManager.Instance is null at monologue start. Using fallback.");
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            // Завершаем предыдущий активный диалог VIDE (если есть)
            if (VIDE_Data.VIDE_Data.isActive)
            {
                Debug.Log("Завершаем предыдущий активный VIDE диалог/монолог");
                VIDE_Data.VIDE_Data.EndDialogue();
            }

            if (monologuePanel == null || monologueText == null)
            {
                Debug.LogError("[MonologueManager] Отсутствуют ссылки на UI компоненты!");
                if (PlayerControlManager.Instance != null) 
                {
                    PlayerControlManager.Instance.SetUIControlsBlock(false); 
                    Debug.Log("[MonologueManager] SetUIControlsBlock(false) called due to missing UI components.");
                }
                // Восстанавливаем курсор в игровое состояние при ошибке
                if (CursorManager.Instance != null)
                {
                    CursorManager.Instance.SetGameplayState();
                }
                else
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
                return;
            }

            monologueAssign.overrideStartNode = monologueID;
            VIDE_Data.VIDE_Data.BeginDialogue(monologueAssign); // Начинаем новый монолог
            currentMonologueID = monologueID;
            currentCommentIndex = 0;

            bool playOnce = false;
            if (VIDE_Data.VIDE_Data.nodeData != null && VIDE_Data.VIDE_Data.nodeData.extraVars != null && VIDE_Data.VIDE_Data.nodeData.extraVars.ContainsKey("playOnce"))
            {
                object val = VIDE_Data.VIDE_Data.nodeData.extraVars["playOnce"];
                if (val is bool) playOnce = (bool)val;
                else if (val is string) playOnce = (val as string).ToLower() == "true";
            }

            string playedKey = $"Monologue_{monologueID}_Played";
            if (playOnce && PlayerPrefs.GetInt(playedKey, 0) == 1)
            {
                Debug.Log($"[MonologueManager] Монолог {monologueID} уже был проигран ранее, не показываем повторно.");
                VIDE_Data.VIDE_Data.EndDialogue(); 
                if (PlayerControlManager.Instance != null) 
                {
                    PlayerControlManager.Instance.SetUIControlsBlock(false); 
                    Debug.Log("[MonologueManager] SetUIControlsBlock(false) called because monologue was already played.");
                }
                // Восстанавливаем курсор в игровое состояние
                if (CursorManager.Instance != null)
                {
                    CursorManager.Instance.SetGameplayState();
                }
                else
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
                return;
            }

            if (VIDE_Data.VIDE_Data.nodeData != null && VIDE_Data.VIDE_Data.nodeData.comments != null && VIDE_Data.VIDE_Data.nodeData.comments.Length > 0)
            {
                Debug.Log("[MonologueManager] Данные узла получены, текст: " + VIDE_Data.VIDE_Data.nodeData.comments[0]);
                monologuePanel.SetActive(true);
                ShowCurrentComment();
                isMonologueActive = true; 

                if (playOnce)
                {
                    PlayerPrefs.SetInt(playedKey, 1);
                    PlayerPrefs.Save();
                }
            }
            else
            {
                Debug.LogError("[MonologueManager] Не удалось получить данные узла или комментарии отсутствуют!");
                VIDE_Data.VIDE_Data.EndDialogue(); 
                if (PlayerControlManager.Instance != null) 
                {
                    PlayerControlManager.Instance.SetUIControlsBlock(false); 
                     Debug.Log("[MonologueManager] SetUIControlsBlock(false) called due to missing node data/comments.");
                }
                // Восстанавливаем курсор в игровое состояние при ошибке
                if (CursorManager.Instance != null)
                {
                    CursorManager.Instance.SetGameplayState();
                }
                else
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
                if(monologuePanel != null) monologuePanel.SetActive(false);
                isMonologueActive = false; 
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
            int completedID = currentMonologueID; 

            monologuePanel.SetActive(false);
            if (VIDE_Data.VIDE_Data.isActive) 
            {
                VIDE_Data.VIDE_Data.EndDialogue();
            }
            isMonologueActive = false;
            currentMonologueID = -1;
            currentCommentIndex = 0;

            if (PlayerControlManager.Instance != null)
            {
                PlayerControlManager.Instance.SetUIControlsBlock(false); 
                Debug.Log("[MonologueManager] SetUIControlsBlock(false) called on monologue hide.");
            }

            // УПРАВЛЕНИЕ КУРСОРОМ: Скрыть курсор (на всякий случай)
            if (CursorManager.Instance != null)
            {
                CursorManager.Instance.SetGameplayState();
                Debug.Log("[MonologueManager] Cursor set to Gameplay state at monologue hide.");
            }
            else
            {
                Debug.LogWarning("[MonologueManager] CursorManager.Instance is null at monologue hide. Using fallback.");
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            // НОВОЕ: Добавляем ID завершенного монолога в очередь с специальным "системным" NPC ID
            if (completedID >= 0 && DialogueStartNodeQueue.Instance != null)
            {
                const string SYSTEM_NPC_ID = "SYSTEM_MONOLOGUE";
                DialogueStartNodeQueue.Instance.EnqueueStartNode(SYSTEM_NPC_ID, completedID);
                Debug.Log($"[MonologueManager] Добавлен в очередь ID {completedID} для системных монологов после завершения");
            }

            OnMonologueComplete?.Invoke(completedID);
            Debug.Log($"[MonologueManager] Monologue {completedID} completed and event invoked.");
        }

        // Добавляем метод для очистки при отключении объекта
        void OnDisable()
        {
            if(isMonologueActive) 
            {
                 Debug.LogWarning("[MonologueManager] Monologue was active during OnDisable.");
                if (PlayerControlManager.Instance != null)
                {
                    PlayerControlManager.Instance.SetUIControlsBlock(false);
                    Debug.LogWarning("[MonologueManager] SetUIControlsBlock(false) called in OnDisable as a fallback.");
                }
                // Также восстанавливаем курсор в игровое состояние
                if (CursorManager.Instance != null)
                {
                    CursorManager.Instance.SetGameplayState();
                    Debug.LogWarning("[MonologueManager] Cursor set to Gameplay state in OnDisable as a fallback.");
                }
                else
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
                isMonologueActive = false; 
            }

            // Завершаем диалог VIDE, если он все еще активен и связан с этим менеджером
            // Это более сложный момент, так как VIDE_Data.isActive глобальный.
            // Лучше убедиться, что EndDialogue вызывается в HideMonologue.
            // if (VIDE_Data.VIDE_Data.isActive && VIDE_Data.VIDE_Data.assigned == monologueAssign)
            // {
            //     VIDE_Data.VIDE_Data.EndDialogue();
            // }
            currentMonologueID = -1; // Сбрасываем ID
        }

        // Добавляем OnDestroy для очистки подписчиков события
        void OnDestroy()
        {
            OnMonologueComplete = null;
            Debug.Log("[MonologueManager] OnMonologueComplete event cleared in OnDestroy.");
        }
    }
} 