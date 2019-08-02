using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]

public class Dialogue// : MonoBehaviour
{
    public DialogueNPCInfo npcInfo;
    
    [TextArea(3, 10)]
    public string sentence;
}
