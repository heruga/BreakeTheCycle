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


    bool dialoguePaused = false; //Custom variable to prevent the manager from calling VD.Next
    bool animatingText = false; //Will help us know when text is currently being animated
    int availableChoices = 0;

    IEnumerator TextAnimator;

    #endregion

    #region MAIN

    void Awake()
    {
        // Instead of loading all dialogues at once, we'll load them on demand
        // VD.LoadDialogues(); // Commented out to prevent the collection modification error
    }

    //Call this to begin the dialogue and advance through it
    public void Interact(VIDE_Assign dialogue)
    {
        //Удаляем проверку PreConditions
        //var doNotInteract = PreConditions(dialogue);
        //if (doNotInteract) return;

        if (!VD.isActive)
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
        //VD.OnActionNode += ActionHandler;
        VD.OnNodeChange += UpdateUI;
        //VD.OnEnd += EndDialogue;

        // Load the specific dialogue we need
        VD.Load(dialogue.assignedDialogue);

        VD.BeginDialogue(dialogue); //Begins dialogue, will call the first OnNodeChange

        dialogueContainer.SetActive(true); //Let's make our dialogue container visible
    }
    
    //Calls next node in the dialogue
    public void CallNext()
    {
        //Удаляем CutTextAnim
        //if (animatingText) { CutTextAnim(); return; }

        if (!dialoguePaused) //Only if
        {
            VD.Next(); //We call the next node and populate nodeData with new data. Will fire OnNodeChange.
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
        VD.nodeData.commentIndex = choice;

        if (Input.GetMouseButtonUp(0))
        {
            Interact(VD.assigned);
        }
    }

    //Input related stuff (scroll through player choices and update highlight)
    void Update()
    {
        //Lets just store the Node Data variable for the sake of fewer words
        var data = VD.nodeData;

        if (VD.isActive) //If there is a dialogue active
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
            }

            //Detect interact key
            if (Input.GetKeyDown(interactionKey))
            {
                Interact(VD.assigned);
            }
            if (Input.GetMouseButtonDown(0))
            {
                if (animatingText)
                {
                    Interact(VD.assigned);
                }
                else if (!data.isPlayer)
                {
                    Interact(VD.assigned);
                }
            }
        }
        //Note you could also use Unity's Navi system, in which case you would tick the useNavigation flag.
    }

    //When we call VD.Next, nodeData will change. When it changes, OnNodeChange event will fire
    //We subscribed our UpdateUI method to the event in the Begin method
    //Here's where we update our UI
    void UpdateUI(VD.NodeData data)
    {
        //Reset some variables
        NPC_Text.text = "";
        foreach (Button b in maxPlayerChoices) { b.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = ""; b.transform.GetChild(0).GetComponent<TextMeshProUGUI>().color = Color.white; }
        NPC_Container.SetActive(false);
        playerContainer.SetActive(false);
        playerSprite.sprite = null;
        NPCSprite.sprite = null;

        //Удаляем PostConditions
        //PostConditions(data);

        //If this new Node is a Player Node, set the player choices offered by the node
        if (data.isPlayer)
        {
            //Set node sprite if there's any, otherwise try to use default sprite

        }
    }

    #endregion
} 