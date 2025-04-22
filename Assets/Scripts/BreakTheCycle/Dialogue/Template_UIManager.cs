/*
 *  This is a template verison of the VIDEUIManager1.cs script. Check that script out and the "Player Interaction" demo for more reference.
 *  This one doesn't include an item popup as that demo was mostly hard coded.
 *  Doesn't include reference to a player script or gameobject. How you handle that is up to you.
 *  Doesn't save dialogue and VA state.
 *  Player choices are not instantiated. You need to set the references manually.
    
 *  You are NOT limited to what this script can do. This script is only for convenience. You are completely free to write your own manager or build from this one.
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using VIDE_Data; //<--- Import to use VD class
using TMPro;
using BreakTheCycle;

public class Template_UIManager : MonoBehaviour
{
    #region VARS

    //These are the references to UI components and containers in the scene
    [Header("References")]
    public GameObject dialogueContainer;
    public GameObject NPC_Container;
    public GameObject playerContainer;

    public TextMeshProUGUI NPC_Text;
    public TextMeshProUGUI NPC_label;
    public Image NPCSprite;
    public Image playerSprite;
    public TextMeshProUGUI playerLabel;

    public List<Button> maxPlayerChoices = new List<Button>();

    [Tooltip("Attach an Audio Source and reference it if you want to play audios")]
    public AudioSource audioSource;

    [Header("Options")]
    public KeyCode interactionKey;
    public bool NPC_animateText;
    public bool player_animateText;
    public float NPC_secsPerLetter;
    public float player_secsPerLetter;
    public float choiceInterval;
    [Tooltip("Tick this if using Navigation. Will prevent mixed input.")]
    public bool useNavigation;


    bool dialoguePaused = false; //Custom variable to prevent the manager from calling VIDE_Data.Next
    bool animatingText = false; //Will help us know when text is currently being animated
    int availableChoices = 0;

    IEnumerator TextAnimator;

    #endregion

    #region MAIN

    void Awake()
    {
        // Instead of loading all dialogues at once, we'll load them on demand
        // VIDE_Data.LoadDialogues(); // Commented out to prevent the collection modification error
    }

    //Call this to begin the dialogue and advance through it
    public void Interact(VIDE_Assign dialogue)
    {
        Debug.Log($"[Interact] dialogue={dialogue}, isActive={VIDE_Data.VIDE_Data.isActive}");
        if (!VIDE_Data.VIDE_Data.isActive)
        {
            Begin(dialogue);
        }
        else
        {
            CallNext();
        }
    }

    //This begins the conversation. 
    void Begin(VIDE_Assign dialogue)
    {
        //Let's reset the NPC text variables
        NPC_Text.text = "";
        NPC_label.text = "";
        playerLabel.text = "";

        //Subscribe to events
        //VIDE_Data.OnActionNode += ActionHandler;
        VIDE_Data.VIDE_Data.OnNodeChange += UpdateUI;
        VIDE_Data.VIDE_Data.OnEnd += EndDialogue;

        // Удаляем ручную загрузку диалога, чтобы избежать ошибки
        // VIDE_Data.VIDE_Data.Load(dialogue.assignedDialogue);

        VIDE_Data.VIDE_Data.BeginDialogue(dialogue); //Begins dialogue, will call the first OnNodeChange

        Debug.Log("Открываем диалоговое окно!");
        dialogueContainer.SetActive(true);
        Debug.Log("dialogueContainer active: " + dialogueContainer.activeSelf);
        // Отключаем управление игроком
        if (PlayerControlManager.Instance != null)
            PlayerControlManager.Instance.SetControlsEnabled(false);
    }
    
    //Calls next node in the dialogue
    public void CallNext()
    {
        Debug.Log($"[CallNext] dialoguePaused={dialoguePaused}, isEnd={VIDE_Data.VIDE_Data.nodeData?.isEnd}");
        var data = VIDE_Data.VIDE_Data.nodeData;
        Debug.Log($"[CallNext] commentIndex={data.commentIndex}, comments.Length={data.comments?.Length ?? -1}");
        if (data.comments != null && data.commentIndex < data.comments.Length)
        {
            Debug.Log($"[CallNext] Выбранный комментарий: {data.comments[data.commentIndex]}");
        }
        // Попробуем получить следующий узел через GetNext
        var nextNode = VIDE_Data.VIDE_Data.GetNext(true, true);
        Debug.Log($"[CallNext] GetNext: nodeID={nextNode?.nodeID}, isPlayer={nextNode?.isPlayer}, isEnd={nextNode?.isEnd}");
        if (nextNode == null || nextNode.isEnd)
        {
            Debug.Log("[CallNext] Следующего узла нет или это конец — завершаем диалог вручную.");
            EndDialogue(VIDE_Data.VIDE_Data.nodeData);
            return;
        }
        if (!dialoguePaused) //Only if
        {
            var afterNext = VIDE_Data.VIDE_Data.Next();
            Debug.Log($"[CallNext] После Next: nodeID={afterNext?.nodeID}, isPlayer={afterNext?.isPlayer}, isEnd={afterNext?.isEnd}");
        }
        else
        {
            //Stuff we can do instead if dialogue is paused
        }
    }

    //If not using local input, then the UI buttons are going to call this method when you tap/click them!
    //They will send along the choice index
    public void SelectChoice(int choice)
    {
        Debug.Log($"[SelectChoice] Выбран индекс: {choice}");
        VIDE_Data.VIDE_Data.nodeData.commentIndex = choice;
        Interact(VIDE_Data.VIDE_Data.assigned);
    }

    //Input related stuff (scroll through player choices and update highlight)
    void Update()
    {
        //Lets just store the Node Data variable for the sake of fewer words
        var data = VIDE_Data.VIDE_Data.nodeData;

        if (VIDE_Data.VIDE_Data.isActive) //If there is a dialogue active
        {
            //Scroll through Player dialogue options if dialogue is not paused and we are on a player node
            //For player nodes, NodeData.commentIndex is the index of the picked choice
            if (!data.pausedAction && !animatingText && data.isPlayer && !useNavigation)
            {
                if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
                {
                    if (data.commentIndex < availableChoices - 1)
                        data.commentIndex++;
                }
                if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
                {
                    if (data.commentIndex > 0)
                        data.commentIndex--;
                }
                //Color the Player options. Blue for the selected one
                for (int i = 0; i < maxPlayerChoices.Count; i++)
                {
                    maxPlayerChoices[i].transform.GetChild(0).GetComponent<TextMeshProUGUI>().color = Color.white;
                    if (i == data.commentIndex) maxPlayerChoices[i].transform.GetChild(0).GetComponent<TextMeshProUGUI>().color = Color.yellow;
                }

                // --- ВЫБОР РЕПЛИКИ ПО ENTER или ЛКМ ---
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetMouseButtonDown(0))
                {
                    Debug.Log($"[UI] Выбрана реплика: {data.commentIndex} — {data.comments[data.commentIndex]}");
                    SelectChoice(data.commentIndex);
                }
            }

            // --- ЗАВЕРШЕНИЕ ДИАЛОГА ПО ENTER/ЛКМ НА NPC/END NODE ---
            if (!data.isPlayer && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetMouseButtonDown(0)))
            {
                Debug.Log("[UI] Завершаем диалог по Enter/ЛКМ на NPC/End Node");
                Interact(VIDE_Data.VIDE_Data.assigned);
            }

            //Detect interact key
            if (Input.GetKeyDown(interactionKey))
            {
                Interact(VIDE_Data.VIDE_Data.assigned);
            }
        }
        //Note you could also use Unity's Navi system, in which case you would tick the useNavigation flag.
    }

    //When we call VIDE_Data.Next, nodeData will change. When it changes, OnNodeChange event will fire
    //We subscribed our UpdateUI method to the event in the Begin method
    //Here's where we update our UI
    void UpdateUI(VIDE_Data.VIDE_Data.NodeData data)
    {
        Debug.Log($"[UpdateUI] Вызван для nodeID={data.nodeID}, isPlayer={data.isPlayer}, isEnd={data.isEnd}, commentIndex={data.commentIndex}");
        // Подробная отладка
        Debug.Log($"[UpdateUI] NodeData: isPlayer={data.isPlayer}, commentIndex={data.commentIndex}, comments.Length={data.comments?.Length ?? -1}");
        if (data.comments != null)
        {
            for (int i = 0; i < data.comments.Length; i++)
            {
                Debug.Log($"[UpdateUI] data.comments[{i}]: {data.comments[i]}");
            }
        }
        Debug.Log($"[UpdateUI] extraData: {data.extraData}");

        // Устанавливаем количество доступных вариантов для навигации
        availableChoices = data.comments != null ? data.comments.Length : 0;

        // Сброс UI
        NPC_Text.text = "";
        NPC_label.text = "";
        playerLabel.text = "";
        foreach (Button b in maxPlayerChoices) { b.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = ""; b.transform.GetChild(0).GetComponent<TextMeshProUGUI>().color = Color.white; }
        NPC_Container.SetActive(false);
        playerContainer.SetActive(false);

        // Выводим текст в зависимости от типа узла
        if (!data.isPlayer)
        {
            data.commentIndex = 0; // Сбросить индекс для NPC-узла
            NPC_Container.SetActive(true);
            if (data.comments != null && data.commentIndex < data.comments.Length)
            {
                NPC_Text.text = data.comments[data.commentIndex];
                Debug.Log($"[UpdateUI] NPC_Text.text = {NPC_Text.text}");
            }
            else
            {
                Debug.LogWarning("[UpdateUI] Нет комментариев для NPC!");
            }
            // Имя NPC из поля Tag
            if (!string.IsNullOrEmpty(data.tag))
                NPC_label.text = data.tag;
            else
                NPC_label.text = "NPC";
            Debug.Log($"[UpdateUI] NPC_label.text = {NPC_label.text}");
        }
        else
        {
            playerContainer.SetActive(true);
            if (data.comments != null)
            {
                for (int i = 0; i < data.comments.Length && i < maxPlayerChoices.Count; i++)
                {
                    maxPlayerChoices[i].transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = data.comments[i];
                    Debug.Log($"[UpdateUI] PlayerChoice[{i}] = {data.comments[i]}");
                    var btn = maxPlayerChoices[i];
                    int choiceIndex = i;
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => {
                        VIDE_Data.VIDE_Data.nodeData.commentIndex = choiceIndex;
                        Debug.Log($"[UI] Клик по варианту: {choiceIndex} — {data.comments[choiceIndex]}");
                        CallNext();
                    });
                }
            }
            // Имя игрока из поля Tag
            if (!string.IsNullOrEmpty(data.tag))
                playerLabel.text = data.tag;
            else
                playerLabel.text = "Игрок";
            Debug.Log($"[UpdateUI] playerLabel.text = {playerLabel.text}");
        }
    }

    void EndDialogue(VIDE_Data.VIDE_Data.NodeData data)
    {
        Debug.Log("[EndDialogue] Диалог завершён!");
        dialogueContainer.SetActive(false); // Скрываем окно диалога
        // Включаем управление игроком обратно
        if (PlayerControlManager.Instance != null)
            PlayerControlManager.Instance.SetControlsEnabled(true);
        VIDE_Data.VIDE_Data.EndDialogue(); // Корректно завершаем диалог в VIDE
    }

    void OnDestroy()
    {
        VIDE_Data.VIDE_Data.OnEnd -= EndDialogue;
    }

    #endregion
} 