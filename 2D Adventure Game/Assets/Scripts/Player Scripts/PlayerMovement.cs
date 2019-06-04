using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;

public enum PlayerState
{
	Idle, Walk, Run, Attack, Paused, Dead
}

public class PlayerMovement : MonoBehaviour
{
	public PlayerState currentState, laststate;
	public Rigidbody2D rb;
	public PlayerAnimation playerAnim;
	public LookTowardsTarget targetMode;
	private bool CanSetState;

	private float horizontalspeed, verticalspeed;
	private float SwordMomentum, SwordMomentumSmooth, SwordMomentumPower;
	public FloatValue SwordMomentumScale, MoveSpeed;

	public Vector3 position;
	public static Vector3 test;
	
	void Start()
	{
		currentState = PlayerState.Idle;
		rb = GetComponent<Rigidbody2D>();
		SwordMomentumSmooth = 4f;
		SwordMomentumPower = 1f;
		CanSetState = true;
	}

	void FixedUpdate()
	{
		if (currentState == PlayerState.Run || currentState == PlayerState.Walk || currentState == PlayerState.Attack)
		{
			rb.MovePosition(transform.position + position * MoveSpeed.initialValue * Time.deltaTime);
		}
	}

	void Update()
	{
		CheckForPause();
		if (currentState != PlayerState.Attack && currentState != PlayerState.Paused)
		{
			position = new Vector3(Input.GetAxisRaw("Horizontal"), (Input.GetAxisRaw("Vertical")), 0).normalized;
		}

		if (position != Vector3.zero)
		{
			horizontalspeed = position.x;
			verticalspeed = position.y;
			playerAnim.AnimSpeed(horizontalspeed, verticalspeed); //Anim speed

			//Run
			if (currentState == PlayerState.Idle || currentState == PlayerState.Run)
			{
				playerAnim.SetAnimState(AnimationState.Run);
				currentState = PlayerState.Run;
				MoveSpeed.initialValue = 4;
			}
			
			//Walk
			if (currentState == PlayerState.Walk)
			{
				playerAnim.SetAnimState(AnimationState.Walk);
				MoveSpeed.initialValue = 2;
			}
			else
			{
				MoveSpeed.initialValue = 4;
			}
		}

		//Idle
		if (currentState != PlayerState.Attack && currentState != PlayerState.Paused)
		{
			if (position == Vector3.zero)
			{
				playerAnim.SetAnimState(AnimationState.Idle);
				currentState = PlayerState.Idle;
				test = Vector3.zero;
			}
		}

		//Attack
		if (currentState == PlayerState.Attack)
		{
			playerAnim.SetAnimState(AnimationState.SwordAttack);
			SwordMomentumScale.initialValue += SwordMomentumSmooth * Time.deltaTime;
			SwordMomentum = Mathf.Lerp(SwordMomentumPower, 0, SwordMomentumScale.initialValue);
			position = (SwordMomentum * test);
		}
	}

	//Check if game is paused
	public void CheckForPause()
	{
		if (PauseGame.IsPaused || Hitstun.HitStunEnabled)
		{
			if (CanSetState)
			{
				CanSetState = false;
				laststate = currentState;
			}
			currentState = PlayerState.Paused;
		}
		else
		{
			if (!CanSetState)
			{
				CanSetState = true;
				currentState = laststate;
			}
		}
		
		if (currentState == PlayerState.Paused)
		{
			rb.bodyType = RigidbodyType2D.Static;
			playerAnim.AnimPause(true);
		}
		else
		{
			rb.bodyType = RigidbodyType2D.Dynamic;
			playerAnim.AnimPause(false);
		}
	}
	
	//Get sword swing direction
	public void GetSwordSwingDirection()
	{
		test = new Vector3(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"), 0).normalized;
	}
}