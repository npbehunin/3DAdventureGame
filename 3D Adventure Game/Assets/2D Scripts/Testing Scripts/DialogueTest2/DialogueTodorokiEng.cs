using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using JetBrains.Annotations;
using UnityEngine;

[Serializable]
public class DialogueTodorokiEng
{
	public Dictionary<String, String> dict;
	
	public DialogueTodorokiEng()
	{
		dict = new Dictionary<String, String>
		{
			{"TodorokiForest1", "What the friggidy dip are you doing?"},
			{"TodorokiForest2", "Holy HECK dude."},
			{"TodorokiForest3", "I can't take you anywhere anymore, can I?"},
			
			{"TodorokiDesert1", "I hate sand. It's rough, course, and irratating, and it gets everywhere."},
			
			{"TodorokiOrangeCliff1", "The people here really suck."}
		};

		//Forest Temple
		//todorokiForest[0] = new DialogueThingy("TodorokiYell1", "What thE FRICK are you DOING?");
		//todorokiForest[1] = new DialogueThingy("TodorokiYell2", "NOOOOOOOOOOO");
		//todorokiForest[2] = new DialogueThingy("TodorokiCalm1", "Ehh, I don't care");
		//
		////Desert Temple
		//todorokiDesert[0] = new DialogueThingy("TodorokiDesert1", "I hate sand.");
	}
}

//OKAY SO APPARENTLY JSON DOESN'T LIKE TAKING IN DICTIONARIES, OF COOOOUUURSE

//SO WE HAVE TO FIGURE OUT HOW TO SERIALIZE IT OR CONVERT IT, OF COOOUUUUURSE
//PASS IN A DUMB OBJECT

//DICTIONARIES! THEY'RE PERFECT

//Dictionaries contain a "Key" and then the information about the key. We can search the dictionary for all OF IT AAAAUGH

//TO DO EEEE
//We still need a place to load the information from the JSON file. Either we replace this whole class when loading, just
//the dictionary, or we have to type every single dictionary reference into an individual string AND THAT'S THE WORST.
//So the plan is........

//1: Type all the dialogue, and JUST the dialogue (for todoroki) in this script.
//2: Save this dictionary (or class?) into a JSON file.
//3: Load the JSON file and overwrite this dictionary with the one inside the JSON. JUST FORGET TYPING A BILLION STRINGS.
//4: When we need to SayDialogue, make the SayDialogue function pass through a key string, then just check it IN THE
//DICTIONARY AAAAUGH
//DON'T OVERTHINK IT OKAY, JUST DO THIS ^^^

//WE DON'T NEED ARRAYS OR LISTS, SHUT UP