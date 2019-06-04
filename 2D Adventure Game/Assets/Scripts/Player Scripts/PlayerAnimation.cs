using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum AnimationState
{
	Idle, Walk, Run, Interact, Dead, //0-4
	SwordAttack, BowAttack, BombAttack, //5-7
	SitChair, SitGround, Fishing //8-10
}

public class PlayerAnimation : MonoBehaviour
{
	public AnimationState animState;
	public Animator animator;
	public LookTowardsTarget targetMode;

	void Start()
	{
		animState = AnimationState.Idle;
		animator = gameObject.GetComponent<Animator>();
	}

	void Update()
	{
		animator.SetInteger("AnimState", (int) animState);
		animator.enabled = !PauseGame.IsPaused;
	}

	//Set the animation state
	public void SetAnimState(AnimationState anim)
	{
		animState = anim;
	}

	//SpeedX and SpeedY
	public void AnimSpeed(float x, float y)
	{
		animator.SetFloat("SpeedX", x);
		animator.SetFloat("SpeedY", y);
	}

	//Sword attack
	public void SetAnimSwordAttack(int attack)
	{
		animator.SetInteger("SwordAttackState", attack);
	}

	//Pause the animation
	public void AnimPause(bool isPaused)
	{
		animator.enabled = !isPaused;
	}

	//Targetmode
	public void AnimLookDirection()
	{
		switch (targetMode.direction)
		{
			case AnimatorDirection.Up:
				animator.SetFloat("DirectionY", 1);
				animator.SetFloat("DirectionX", 0);
				animator.SetFloat("SpeedX", 0);
				animator.SetFloat("SpeedY", 1);
				break;
			case AnimatorDirection.Down:
				animator.SetFloat("DirectionY", -1);
				animator.SetFloat("DirectionX", 0);
				animator.SetFloat("SpeedX", 0);
				animator.SetFloat("SpeedY", -1);
				break;
			case AnimatorDirection.Left:
				animator.SetFloat("DirectionY", 0);
				animator.SetFloat("DirectionX", -1);
				animator.SetFloat("SpeedX", -1);
				animator.SetFloat("SpeedY", 0);
				break;
			case AnimatorDirection.Right:
				animator.SetFloat("DirectionY", 0);
				animator.SetFloat("DirectionX", 1);
				animator.SetFloat("SpeedX", 1);
				animator.SetFloat("SpeedY", 0);
				break;
			default:
				animator.SetFloat("DirectionY", 0);
				animator.SetFloat("DirectionX", 0);
				animator.SetFloat("SpeedX", 0);
				animator.SetFloat("SpeedY", 0);
				break;
		}
	}
}
