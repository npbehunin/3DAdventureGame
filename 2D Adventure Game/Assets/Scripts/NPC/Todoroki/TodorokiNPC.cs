using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TodorokiNPC : Interactable
{
	public Dialogue[] dialogue1;
	public DialogueSignal dialogueSignal;
	
	protected override void Start () 
	{
		base.Start();
	}
	
	protected override void Update () 
	{
		base.Update();
	}

	protected override void Interact()
	{
		dialogueSignal.Raise(dialogue1);
		Debug.Log("Interacted with Todoroki!");
		//Remember to set CanInteract back to true once the conversation finishes!
	}
}
