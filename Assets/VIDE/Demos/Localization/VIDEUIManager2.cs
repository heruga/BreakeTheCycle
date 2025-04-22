using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using VIDE_Data;

/*
 * This is another example script that handles the data obtained from nodeData
 * It handles localization data.
 * Refer to Example3Dialogue dialogue in the VIDE Editor.
 * It is simpler and focused on showing both NPC and Player text at the same time
 * It doesn't require any VIDE_Data or VIDE_Assign component to be in the scene
 */

public class VIDEUIManager2 : MonoBehaviour
{
    public string dialogueNameToLoad;
    public Text[] playerChoices;
    public Text npcText;
    public Image flag;
    public AudioSource audioPlayer;

    void Start()
    {
        //Sets the temp VIDE_Assign's variables for the given dialogue.
        VIDE_Data.VIDE_Data.SetAssigned(dialogueNameToLoad, "LocalizationTest", -1, null, null);
    }

    //Called by UI button
    public void Begin()
    {
        if (!VIDE_Data.VIDE_Data.isActive)
        {
            transform.GetChild(1).gameObject.SetActive(true); //UI stuff
            transform.GetChild(0).gameObject.SetActive(false); //UI stuff
            VIDE_Data.VIDE_Data.OnNodeChange += NodeChangeAction; //Required events
            VIDE_Data.VIDE_Data.OnEnd += End; //Required events
            VIDE_Data.VIDE_Data.BeginDialogue(dialogueNameToLoad);
        }
    }

    //Called by UI buttons, every button sends a different choice index
    public void ButtonChoice(int choice)
    {
        VIDE_Data.VIDE_Data.nodeData.commentIndex = choice; //Set commentIndex as it acts as the picked choice
        if (VIDE_Data.VIDE_Data.nodeData.extraVars.ContainsKey("loadLang"))
        {
            if (choice < (int)VIDE_Data.VIDE_Data.nodeData.extraVars["loadLang"]) //Don't count index 3 as language
			{
				VIDE_Data.VIDE_Data.OnLanguageChange += UpdateWithNewLanguage;
				VIDE_Data.VIDE_Data.SetCurrentLanguage(VIDE_Data.VIDE_Data.GetLanguages()[choice]); 
			}
			else 
			{
				VIDE_Data.VIDE_Data.Next(); 
			}
        }
    }

    void OnDisable()
    {
        //If the script gets destroyed, let's make sure we force-end the dialogue to prevent errors
        End(null);
    }

    //This will trigger with the OnLanguageChange event
    //It will make sure the current text being displayed will be updated with the new localization
    void UpdateWithNewLanguage() {
        npcText.text = VIDE_Data.VIDE_Data.GetNodeData(0).comments[0];
        flag.sprite = VIDE_Data.VIDE_Data.GetNodeData(0).sprite;
        SetPlayerChoices();
        audioPlayer.clip = VIDE_Data.VIDE_Data.GetNodeData(0).audios[0];
        audioPlayer.Play();
        VIDE_Data.VIDE_Data.OnLanguageChange -= UpdateWithNewLanguage;
    }

    //Called by the OnNodeChange event
    void NodeChangeAction(VIDE_Data.VIDE_Data.NodeData data)
    {
        if (data.isPlayer)
        {
            SetPlayerChoices();
        }
        else
        {
            WipePlayerChoices();
            StartCoroutine(ShowNPCText());
        }
    }

    void WipePlayerChoices()
    {
        for (int i = 0; i < playerChoices.Length; i++)
        {
            playerChoices[i].transform.parent.gameObject.SetActive(false);
        }
    }

    void SetPlayerChoices()
    {
        for (int i = 0; i < playerChoices.Length; i++)
        {
            if (i < VIDE_Data.VIDE_Data.nodeData.comments.Length)
            {
                playerChoices[i].transform.parent.gameObject.SetActive(true);
                playerChoices[i].text = VIDE_Data.VIDE_Data.nodeData.comments[i];
            }
            else
            {
                playerChoices[i].transform.parent.gameObject.SetActive(false);
            }
        }
    }

    IEnumerator ShowNPCText()
    {
        if (VIDE_Data.VIDE_Data.GetExtraVariables(VIDE_Data.VIDE_Data.nodeData.nodeID).ContainsKey("flag"))
            flag.sprite = VIDE_Data.VIDE_Data.nodeData.sprite;

        string text = string.Empty;
        npcText.text = text;
        while (text.Length < VIDE_Data.VIDE_Data.nodeData.comments[VIDE_Data.VIDE_Data.nodeData.commentIndex].Length)
        {
            text += VIDE_Data.VIDE_Data.nodeData.comments[VIDE_Data.VIDE_Data.nodeData.commentIndex][text.Length];
            npcText.text = text;
            yield return new WaitForSeconds(0.01f);
        }

        //Automatically call next.
        yield return new WaitForSeconds(1f);
        VIDE_Data.VIDE_Data.Next();
    }

    void End(VIDE_Data.VIDE_Data.NodeData data)
    {
        WipePlayerChoices();
        npcText.text = string.Empty;
        transform.GetChild(1).gameObject.SetActive(false);
        transform.GetChild(0).gameObject.SetActive(true);
        VIDE_Data.VIDE_Data.OnNodeChange -= NodeChangeAction;
        VIDE_Data.VIDE_Data.OnEnd -= End;
        VIDE_Data.VIDE_Data.EndDialogue();
    }


}
