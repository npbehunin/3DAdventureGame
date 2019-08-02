using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

public class DialogueThingy
{
	public String stringName;
	public String dialogue;

	public DialogueThingy(String name, String sentence)
	{
		stringName = name;
		dialogue = sentence;
	}
}
