using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractionSymbol : MonoBehaviour
{
	public GameObject exclamation;
	
	void Start()
	{
		exclamation.SetActive(false);
	}
	public void ExclamationEnable()
	{
		exclamation.SetActive(true);
	}

	public void ExclamationDisable()
	{
		exclamation.SetActive(false);
	}
}
