using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewDialogueSignal", menuName = "ScriptableObject/DialogueSignal")]
public class DialogueSignal : ScriptableObject
{
	public List<DialogueSignalListener> listeners = new List<DialogueSignalListener>();

	public void Raise(Dialogue[] dialogue)
	{
		for (int i = listeners.Count - 1; i >= 0; i--) //Go through our list of listeners backwards (to prevent out of range exceptions)
		{
			listeners[i].OnSignalRaised(dialogue); //Whatever it needs to do for the signal, it will do it.
		}
	}

	public void RegisterListener(DialogueSignalListener listener)
	{
		listeners.Add(listener);
	}
    
	public void DeRegisterListener(DialogueSignalListener listener)
	{
		listeners.Remove(listener);
	}
}
