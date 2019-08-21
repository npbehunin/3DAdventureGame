using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogueTest2Manager : MonoBehaviour
{
	public String currentDialogue;
	
	void Start () 
	{
		
	}

	void GetDialogue(String npc, String id)
	{
		//Runs a for loop through each id in the associated npc script. If it finds the id, it returns the dialogue
		//with the associated id. Else, no text happens.
	}
	
	void Update () 
	{
		Debug.Log(currentDialogue);
	}
}

//WE DON'T NEED THIS SCRIPT UNTIL WE GET THE OTHER TWO SCRIPTS WORKING

//The next method we'll try for handling dialogue:

//(Optional for now)
//1: The DialogueLoad script will first check what language the game is in. Once established, sets an enum.
//2: Determine the path. Ex: (’string path = Path.Combine(Application.peristentDataPath, player.fun);’

//LOADING TYPES
//(If using FileStream...)
//3: Load with FileStream commands (Brackeys)
//(If using text reading...)
//3: Load using string savePath = File.ReadAllText(Application.dataPath, "") (CodeMonkey)

//FILE TYPES
//(If loaded from a JSON...)
//4: Use JSON Utility commands to convert back to data values. (CodeMonkey)
//(If loaded from a txt...) 
//4: Seperate the string into a string array using .split. (CodeMonkey)

//5: Store the data from the JSON file.
//6: Run a command that passes a dialogue id.
//7: Run a for loop or foreach loop in the data for the JSON file that checks each string and matches the ID.
//8: If matched, set the currentdialogue to be equal to that string.

//NOTES
//Dialogue can be seperated into as many files as we want as long as the languages are in separate folders. (We can keep
//all the dialogue in one big file, or split for specific NPCs or normal boxes)

//Keep in mind at some point we will add a conversation box so the player can respond to the NPC, although this will
//probably be handled through code and it just checks for the strings like normal.

//We might also implement things like bold, italic, underlined, etc text. (Depending how much work it would be for
//localization!)