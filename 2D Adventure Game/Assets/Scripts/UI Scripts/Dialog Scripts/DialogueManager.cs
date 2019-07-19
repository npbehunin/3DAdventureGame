using System.Collections;
using System.Collections.Generic;
using System.Net.Mime;
using UnityEngine;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
	public Animator animator;
	public Image currentSprite;
	public Text nameText;
	public Text dialogueText;
	
	private Queue<string> sentences;
	private Queue<string> name;
	private Queue<Sprite> icon;

	public bool CanClick;
	
	// Use this for initialization
	void Start ()
	{
		sentences = new Queue<string>();
		name = new Queue<string>();
		icon = new Queue<Sprite>();
	}

	void Update() //REPLACE THIS LATER IN THE INPUT SCRIPT!!
	{
		if (CanClick && Input.GetKeyDown(KeyCode.E))
		{
			ShowNextSentence();
		}
	}

	public void StartDialogue(Dialogue[] dialogue) //This somehow needs to be called
	{
		animator.SetBool("IsOpen", true);
		CanClick = true;
		sentences.Clear();

		for (int i = 0; i < dialogue.Length; i++)
		{
			string sentence = dialogue[i].sentence;
			string nametext = dialogue[i].npcInfo.npcName.ToString();
			Sprite sprite = dialogue[i].npcInfo.npcSprite;
			sentences.Enqueue(sentence);
			name.Enqueue(nametext);
			icon.Enqueue(sprite);
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
		string nametext = name.Dequeue();
		Sprite sprite = icon.Dequeue();

		StopAllCoroutines();
		StartCoroutine(TypeSentenceAnim(sentence));
		nameText.text = nametext;
		currentSprite.sprite = sprite;
	}

	void EndDialogue()
	{
		animator.SetBool("IsOpen", false);
		CanClick = false;
		Debug.Log("End of convo");
	}

	IEnumerator TypeSentenceAnim(string sentence)
	{
		dialogueText.text = "";
		foreach (char letter in sentence.ToCharArray())
		{
			dialogueText.text += letter;
			yield return CustomTimer.Timer(.015f);
		}
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
