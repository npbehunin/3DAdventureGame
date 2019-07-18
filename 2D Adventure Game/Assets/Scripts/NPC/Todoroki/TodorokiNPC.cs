using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TodorokiNPC : Interactable
{
	public UnityEvent TriggerDialogue;
	public Dialogue dialogue1;
	
	void Start () 
	{
		
	}
	
	void Update () 
	{
		
	}

	protected override void Interact()
	{
		//Trigger the dialog here!
		//Finish with InteractionFinished();
	}
}
