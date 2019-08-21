using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogueLoad : MonoBehaviour
{
	private const string dialogeSaveSeparator = "\n"; //The symbol for recognizing a new string array.

	//[SerializeField] private GameObject unitGameObject;

	private void Awake()
	{
		
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.S))
		{
			Save();
		}

		if (Input.GetKeyDown(KeyCode.L))
		{
			Load();
		}
	}

	private void Save()
	{
		//Receive the signal with the Dialogue object and fileName.
		//Write it into the JSON file.
	}

	private void Load()
	{
		//Receive the signal with the Dialogue object and fileName.
		//Access the JSON file for the associated object and fileName.
		//Send a signal back with the data from the JSON.
	}
}

//This is the "SaveSystem"
//This script is used to load, and or save the dialogue to and from the JSON file.

//NOTES:
//Remember to use "using" right before reading and writing a filestream. Ex: using (FileStream fs = File.Create(path)){...stuff}
//Strings in a text file can be split using .split or .join right after a string. Use a string to tell them where
//to split or join strings to and from their array.
//Strings can also be split using Environment.NewLine instead of checking for a string symbol.
