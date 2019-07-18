using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]

public class Dialogue
{
    public DialogueNPCIcon npcIcon;
    
    [TextArea(3, 10)]
    public string[] sentences;
}

//The instantiated class of dialogue so we can type new text and establish which character's icon to use.
