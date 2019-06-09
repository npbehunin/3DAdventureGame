using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractableCollision : MonoBehaviour {

	public Signal EnterTriggerSignal;
	public Signal ExitTriggerSignal;
	
	void OnTriggerStay2D(Collider2D col)
	{
		if (col.gameObject.CompareTag("InteractableObject"))
		{
			EnterTriggerSignal.Raise();
		}
	}

	void OnTriggerExit2D(Collider2D col)
	{
		if (col.gameObject.CompareTag("InteractableObject"))
		{
			ExitTriggerSignal.Raise();
		}
	}
}
