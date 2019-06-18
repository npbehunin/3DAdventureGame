using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;

public enum PlayerState
{
	Idle, Walk, Run, Attack, Paused, Dead, Hitstun
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
	public Vector3Value direction;
	public static Vector3 inputDirection;
	
	void Start()
	{
		currentState = PlayerState.Idle;
		rb = GetComponent<Rigidbody2D>();
		SwordMomentumSmooth = 4f;
		SwordMomentumPower = 1f;
		CanSetState = true;
		direction.initialPos = new Vector3(1, 0, 0); //Set to the dir the player spawns in
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
		GetDirection();
		if (currentState == PlayerState.Idle || currentState == PlayerState.Walk || currentState == PlayerState.Run)
		{
			position = new Vector3(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"), 0).normalized;
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
		if (currentState != PlayerState.Attack && currentState != PlayerState.Paused && currentState != PlayerState.Hitstun)
		{
			if (position == Vector3.zero)
			{
				playerAnim.SetAnimState(AnimationState.Idle);
				currentState = PlayerState.Idle;
				inputDirection = Vector3.zero;
			}
		}

		//Attack
		if (currentState == PlayerState.Attack)
		{
			playerAnim.SetAnimState(AnimationState.SwordAttack);
			SwordMomentumScale.initialValue += SwordMomentumSmooth * Time.deltaTime;
			SwordMomentum = Mathf.Lerp(SwordMomentumPower, 0, SwordMomentumScale.initialValue);
			
			//Check if there's an input, otherwise move in the anim's direction.
			if (inputDirection != Vector3.zero)
			{
				position = (SwordMomentum * inputDirection);
			}
			else
			{
				position = (SwordMomentum * direction.initialPos);
			}
		}
	}

	//Check if game is paused or if player is hitstunned
	public void CheckForPause()
	{
		if (PauseGame.IsPaused)
		{
			if (CanSetState)
			{
				CanSetState = false;
				laststate = currentState;
			}
			currentState = PlayerState.Paused;
		}
		else if (Hitstun.HitStunEnabled)
		{
			if (CanSetState)
			{
				CanSetState = false;
				laststate = currentState;
			}
			currentState = PlayerState.Hitstun;
		}
		else
		{
			if (!CanSetState)
			{
				CanSetState = true;
				currentState = laststate; //*if last state was attack, player COULD end up being stuck.
			}
		}

		switch (currentState)
		{
			case PlayerState.Paused:
				rb.bodyType = RigidbodyType2D.Static;
				playerAnim.AnimPause(true);
				break;
			case PlayerState.Hitstun:
				position = Vector3.zero;
				playerAnim.AnimPause(true);
				break;
			default:
				rb.bodyType = RigidbodyType2D.Dynamic;
				playerAnim.AnimPause(false);
				break;
		}
	}

	//Returns the direction the player's animation is facing.
	public void GetDirection()
	{
		if (targetMode.CanTarget)
		{
			switch (targetMode.direction)
			{
				case AnimatorDirection.Up:
					direction.initialPos = new Vector3(0, 1, 0);
					break;
				case AnimatorDirection.Down:
					direction.initialPos = new Vector3(0, -1, 0);
					break;
				case AnimatorDirection.Left:
					direction.initialPos = new Vector3(-1, 0, 0);
					break;
				case AnimatorDirection.Right:
					direction.initialPos = new Vector3(1, 0, 0);
					break;
				default:
					direction.initialPos = Vector3.zero;
					break;
			}
		}
		else
		{
			if (position != Vector3.zero)
			{
				if (Mathf.Abs(position.x) >= Mathf.Abs(position.y))
				{
					if (Mathf.Round(position.x) != 0)
					{
						direction.initialPos = new Vector3(Mathf.Round(position.x), 0, 0); //x dir
					}
				}
				else
				{
					if (Mathf.Round(position.y) != 0)
					{
						direction.initialPos = new Vector3(0, Mathf.Round(position.y), 0); //y dir
					}
				}
			}
		}
	}

	//Get sword swing direction, not called through update.
	public void GetSwordSwingDirection()
	{
		inputDirection = new Vector3(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"), 0).normalized;
	}
}

//To do
//1: Switch statement instead of normal state checks.