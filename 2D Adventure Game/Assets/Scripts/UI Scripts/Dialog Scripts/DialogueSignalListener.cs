using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class DialogueSignalListener : MonoBehaviour
{
	public DialogueSignal signal;
	public DialogueEvent signalEvent;
		
	public void OnSignalRaised(Dialogue[] dialogue) //Will do something when we raise a signal
	{
		signalEvent.Invoke(dialogue);
	}

	private void OnEnable()
	{
		signal.RegisterListener(this);
	}

	private void OnDisable()
	{
		signal.DeRegisterListener(this);
	}
}
