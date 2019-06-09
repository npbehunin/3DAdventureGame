using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Interaction : MonoBehaviour
{
	public Signal InteractionSignal;

	
	void OnTriggerStay2D(Collider2D col)
	{
		if (col.gameObject.CompareTag("Player"))
		{
			if (Input.GetKeyDown(KeyCode.E))
			{
				InteractionSignal.Raise();
			}
		}
	}
}
