using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewDialogueInfo", menuName = "ScriptableObject/DialogueInfo")]
public class DialogueInfo : ScriptableObject
{
	public Dialogue dialogue;
}

//Scriptable object for storing the current dialogue info.
