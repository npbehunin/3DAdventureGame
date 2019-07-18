using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public enum npcEnum
{
    Todoroki, Eliza, Jenn
}

public enum emotionEnum
{
    neutral, happy, annoyed, sad
}

[CreateAssetMenu(fileName = "NewNPCIcon", menuName = "Custom Stuff/NPC/DialogueIcon")]
public class DialogueNPCIcon : ScriptableObject
{
    public npcEnum npcName;
    public emotionEnum npcEmotion;
    public Sprite npcSprite;
}

//The name, emotion, and sprite of the dialoge npc icon.