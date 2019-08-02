using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum JellyAnimState
{
	Idle, Attack
}
public class JellyAnimation : MonoBehaviour 
{
	public Animator animator;
	public JellyAnimState AnimState;
	public bool Attacking;
	
	void Start ()
	{
		AnimState = JellyAnimState.Idle;
	}
	
	//Very simple anim right now. Fix later. States currently do nothing.
	void Update () 
	{
		if (Attacking)
		{
			AnimState = JellyAnimState.Attack;
			animator.SetBool("Attacking", true);
		}
		else
		{
			AnimState = JellyAnimState.Idle;
			animator.SetBool("Attacking", false);
		}
	}
}
