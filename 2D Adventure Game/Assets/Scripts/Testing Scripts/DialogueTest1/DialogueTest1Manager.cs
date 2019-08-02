using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogueTest1Manager : MonoBehaviour
{
	public DialogueTest1Dialogue[] languageList;
	private String currentDialogue;
	public int stringToGet;
	public language currentLanguage;

	public enum language
	{
		english, spanish, japanese
	}

	void Start()
	{
		GetDialogue(stringToGet);
	}

	void Update()
	{
		if (currentDialogue != null)
		{
			Debug.Log(currentDialogue);
		}
		else
		{
			Debug.Log("No string found");
		}
		
	}

	void GetDialogue(int id)
	{
		switch (currentLanguage)
		{
			case language.english:
				currentDialogue = languageList[0].Dialogue[id];
				break;
			case language.spanish:
				currentDialogue = languageList[1].Dialogue[id];
				break;
			case language.japanese:
				currentDialogue = languageList[2].Dialogue[id];
				break;
		}
	}
}

//One way of accessing dialogue.

//CONS:
//The dialogue can only be accessed via an int number, not a string id.
//Easy to get out of index error when requesting an id.
//Each CutsceneDialogue needs its own instance in game.

//PROS:
//Short scripts I guess?