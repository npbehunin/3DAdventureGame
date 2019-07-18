using System.Collections;
using System.Collections.Generic;
using System.Net.Mime;
using UnityEngine;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
	public DialogueInfo currentDialogue;
	public Signal dialogueSignal;
	public Sprite npcIcon;
	
	public Text nameText;
	public Text dialogueText;
	
	private Queue<string> sentences;
	
	// Use this for initialization
	void Start ()
	{
		sentences = new Queue<string>();
	}

	public void StartDialogue(Dialogue dialogue) //This somehow needs to be called
	{
		npcIcon = dialogue.npcIcon.npcSprite;
		nameText.text = dialogue.npcIcon.npcName.ToString();
		//npcIcon = currentDialogue.dialogue.npcIcon.npcSprite;
		//nameText.text = currentDialogue.dialogue.npcIcon.npcName.ToString();
		
		sentences.Clear();

		foreach (string sentence in currentDialogue.dialogue.sentences)
		{
			sentences.Enqueue(sentence);
		}
		
		ShowNextSentence();
	}

	public void ShowNextSentence() //THIS SHOULD BE CALLED WHEN THE PLAYER CLICKS FOR THE NEXT SENTENCE
	{
		if (sentences.Count == 0) //If we've reached the end of the queue...
		{
			EndDialogue();
			return;
		}

		//If we still have sentences left to say...
		string sentence = sentences.Dequeue();
		dialogueText.text = sentence;
	}

	void EndDialogue()
	{
		Debug.Log("End of convo");
	}
}

//TO DO
//Call the StartDialogue method somehow in our test NPC (Todoroki).
//The issues is that we can't use a signal because we want to pass through the parameters.
//The only way I can see right now is to store the dialogue info in a dialogue scriptable object, AND call a signal
//seperately for the startdialogue method.

//NOTES
//Right now the emotion enum from DialogueNPCIcon isn't being used UNTIL we want to add different backgrounds behind
//the character icon according to the associated emotion!

//Eventually we want to pass in a bool whether or not it's an NPC convo. If it isn't, then we're going to use a normal
//wide box without the npc name and icon.
