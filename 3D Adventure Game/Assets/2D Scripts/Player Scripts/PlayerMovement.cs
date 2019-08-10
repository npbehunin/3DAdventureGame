﻿using System.Collections;
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
	public bool CanSetState, Invincible;
	public IntValue Health;
	public int CurrentHealth;
	public BoolValue EnemyCollision;

	private float horizontalspeed, verticalspeed;
	private float SwordMomentum, SwordMomentumSmooth, SwordMomentumPower;
	public FloatValue SwordMomentumScale, MoveSpeed;

	public Vector3 position;
	public Vector3Value direction, PlayerTransform, TargetTransform;
	public static Vector3 inputDirection;
	
	void Start()
	{
		currentState = PlayerState.Idle;
		rb = GetComponent<Rigidbody2D>();
		SwordMomentumSmooth = 4f;
		SwordMomentumPower = 1f;
		CanSetState = true;
		direction.initialPos = new Vector3(1, 0, 0); //Set to the dir the player spawns in
		PlayerTransform.initialPos = transform.position;
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
		PlayerTransform.initialPos = transform.position;
		CheckLineOfSight();
		CheckForPause();
		CheckStates();
		GetDirection();
		CheckHealth();
		
		if (currentState == PlayerState.Idle || currentState == PlayerState.Walk || currentState == PlayerState.Run)
		{
			position = new Vector3(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"), 0).normalized;
		}

		//If there's input...
		if (position != Vector3.zero)
		{
			horizontalspeed = position.x;
			verticalspeed = position.y;
			playerAnim.AnimSpeed(horizontalspeed, verticalspeed); //Anim speed

			//Run
			if (currentState == PlayerState.Idle)
			{
				currentState = PlayerState.Run;
			}
		}
	}
	
	//On collision with enemy attack hitbox...
	void OnTriggerStay2D(Collider2D col)
	{
		if (!Invincible && col.gameObject.CompareTag("EnemyAttackHitbox"))
		{
			TakeDamage();
		}
	}

	//Take damage!
	void TakeDamage()
	{
		CurrentHealth -= 1; //1 damage for now
		StartCoroutine(Invincibility());
	}

	//Health
	void CheckHealth()
	{
		Health.initialValue = CurrentHealth;
		if (CurrentHealth <= 0)
		{
			currentState = PlayerState.Dead;
			Debug.Log("Player has died");
		}
	}

	//Check the currentState of the player.
	void CheckStates()
	{
		switch (currentState)
		{
			case PlayerState.Idle:
				playerAnim.SetAnimState(AnimationState.Idle);
				inputDirection = Vector3.zero;
				break;
			case PlayerState.Walk:
				playerAnim.SetAnimState(AnimationState.Walk);
				MoveSpeed.initialValue = 2;
				break;
			case PlayerState.Run:
				playerAnim.SetAnimState(AnimationState.Run);
				break;
			case PlayerState.Attack:
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

				break;
			case PlayerState.Paused:
				rb.bodyType = RigidbodyType2D.Static;
				playerAnim.AnimPause(true);
				Invincible = true; //Invincible while paused
				break;
			case PlayerState.Hitstun:
				position = Vector3.zero;
				playerAnim.AnimPause(true);
				break;
			default:
				currentState = PlayerState.Idle;
				break;
		}
		
		//CurrentState special checks
		//IF not paused
		if (currentState != PlayerState.Paused)
		{
			rb.bodyType = RigidbodyType2D.Dynamic;
			playerAnim.AnimPause(false);
		}

		//If not walk
		if (currentState != PlayerState.Walk)
		{
			MoveSpeed.initialValue = 4;
		}

		//Set Idle
		if (currentState != PlayerState.Attack && currentState != PlayerState.Paused && currentState != PlayerState.Hitstun)
		{
			if (position == Vector3.zero)
			{
				currentState = PlayerState.Idle;
			}
		}
	}

	//*Change to make sure this doesn't run forever!
	void CheckLineOfSight()
	{
		//Handled in the pet for now
	}

	//Check if game is paused or if player is hitstunned
	public void CheckForPause() //Change this to a signal too!
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

	//Hitstun signal
	public void StartHitstun()
	{
		StartCoroutine(HitstunCo());
	}
	
	//Invincibility coroutine
	private IEnumerator Invincibility()
	{
		Invincible = true;
		yield return CustomTimer.Timer(2f);
		Invincible = false;
	}

	//Hitstun coroutine
	public IEnumerator HitstunCo()
	{
		PlayerState lastState = currentState;
		currentState = PlayerState.Hitstun;
		yield return CustomTimer.Timer(.075f);
		currentState = lastState;
		Debug.Log("Hitstun ended");
	}

	//Get sword swing direction, not called through update.
	public void GetSwordSwingDirection()
	{
		inputDirection = new Vector3(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"), 0).normalized;
	}
}

//To do
//1: Switch statement instead of normal state checks.