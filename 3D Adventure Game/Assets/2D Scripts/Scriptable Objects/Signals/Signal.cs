using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewSignal", menuName = "ScriptableObject/Signal")]
public class Signal : ScriptableObject
{
    public List<SignalListener> listeners = new List<SignalListener>();

    public void Raise()
    {
        for (int i = listeners.Count - 1; i >= 0; i--) //Go through our list of listeners backwards (to prevent out of range exceptions)
        {
            listeners[i].OnSignalRaised(); //Whatever it needs to do for the signal, it will do it.
        }
    }

    public void RegisterListener(SignalListener listener)
    {
        listeners.Add(listener);
    }
    
    public void DeRegisterListener(SignalListener listener)
    {
        listeners.Remove(listener);
    }
}
