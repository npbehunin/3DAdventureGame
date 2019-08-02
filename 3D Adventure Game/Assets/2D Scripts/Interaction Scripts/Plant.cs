using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Plant : Interactable {

	public String plantString;

	protected override void Start()
	{
		InteractDir = InteractDirection.down;
		CanInteract = true;
		CheckDir();
	}
	
	protected override void Interact()
	{
		Debug.Log(plantString);
	}
}
