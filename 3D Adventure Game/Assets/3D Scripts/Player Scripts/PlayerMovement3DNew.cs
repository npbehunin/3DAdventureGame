using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;

public class PlayerMovement3DNew : MonoBehaviour
{
	public float accelSpeed, gravity = 20f, slopeForce, slopeForceRayLength;
	public PlayerState currentState;
	public Collider playerCollider;
	public CharacterController controller;
	public Vector3 position, fixedInputPos, rawInputPos;
	public PlayerAnimation playerAnim;
	//Player targeting here!
	public bool CanSetState, Invincible;
	public IntValue Health;
	public int CurrentHealth;
	public BoolValue EnemyCollision;
	
	private float horizontalspeed, verticalspeed;
	private float SwordMomentum, SwordMomentumSmooth, SwordMomentumPower;
	public FloatValue SwordMomentumScale, moveSpeed;
	
	public Vector3Value direction, PlayerTransform, TargetTransform;

	void Start()
	{
		playerCollider = gameObject.GetComponent<Collider>();
		controller = gameObject.GetComponent<CharacterController>();
		
		currentState = PlayerState.Idle;
		SwordMomentumSmooth = 4f;
		SwordMomentumPower = 1f;
		CanSetState = true;
		direction.initialPos = new Vector3(1, 0, 0); //Set to the dir the player spawns in
		PlayerTransform.initialPos = transform.position;
	}

	void Update()
	{
		Debug.Log("Hello");
		PlayerTransform.initialPos = transform.position;
		CheckPlayerMovement();
		CheckForPause();
		CheckStates();
		//GetDirection();
		CheckHealth();
		
		//Controller move
		controller.Move(position * Time.deltaTime);
		
		//Slope check
		if (OnSlope() && (rawInputPos.x > 0 || rawInputPos.z > 0)) //&& if player is moving (ADD THIS!)
		{
			controller.Move(Vector3.down * playerCollider.bounds.size.y / 2 * slopeForce);
		}
		
		//Is Grounded
		if (controller.isGrounded)
			position.y = 0;
		else
			position.y -= gravity * Time.deltaTime;

		//Raw input to check if any input is pressed
		rawInputPos = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));

		//MoveSpeed only applied to x and z.
		position.x *= moveSpeed.initialValue; 
		position.z *= moveSpeed.initialValue;
		
		//Gravity
		position.y -= gravity * Time.deltaTime; //DeltaTime applied here too for acceleration.
	}

	void CheckPlayerMovement()
	{
		if (currentState == PlayerState.Idle || currentState == PlayerState.Walk || currentState == PlayerState.Run)
		{
			Vector3 pos = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
			
			fixedInputPos.x = calcFixedInput(fixedInputPos.x, pos.x);
			fixedInputPos.z = calcFixedInput(fixedInputPos.z, pos.z);
		
			//Position input
			position.x = fixedInputPos.x;
			position.z = fixedInputPos.z;
			
			
		}
	}
	
	//Calculates a new player input with acceleration and deceleration.
	//Turning off "Snap" in the input settings works too, but this helps match controller movement.
	float calcFixedInput(float newInput, float input)
	{
		if (newInput != input)
			if (newInput < -1)
				newInput = -1;
			else if (newInput > 1)
				newInput = 1;
			else
				if (newInput < input)
					if (Math.Abs(newInput - input) < accelSpeed)
						newInput += Math.Abs(newInput - input);
					else
						newInput += accelSpeed;
				else if (newInput > input)
					if (Math.Abs(newInput - input) < accelSpeed)
						newInput -= Math.Abs(newInput - input);
					else
						newInput -= accelSpeed;
		return newInput;
	}

	//Check if the ground normal isn't up.
	private bool OnSlope()
	{
		RaycastHit hit;
		if (Physics.Raycast(transform.position, Vector3.down, out hit, playerCollider.bounds.size.y / 2 * slopeForceRayLength))
			if (hit.normal != Vector3.up)
				return true;
		return false;
	}
	
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
				//playerAnim.SetAnimState(AnimationState.Idle);
				break;
			case PlayerState.Walk:
				//playerAnim.SetAnimState(AnimationState.Walk);
				moveSpeed.initialValue = 2;
				break;
			case PlayerState.Run:
				//playerAnim.SetAnimState(AnimationState.Run);
				break;
			case PlayerState.Attack:
				//playerAnim.SetAnimState(AnimationState.SwordAttack);
				SwordMomentumScale.initialValue += SwordMomentumSmooth * Time.deltaTime;
				SwordMomentum = Mathf.Lerp(SwordMomentumPower, 0, SwordMomentumScale.initialValue);

				//Check if there's an input, otherwise move in the anim's direction.
				//Change this for 3D!
				//if (inputDirection != Vector3.zero)
				//{
				//	position = (SwordMomentum * inputDirection);
				//}
				//else
				//{
				//	position = (SwordMomentum * direction.initialPos);
				//}

				break;
			case PlayerState.Paused:
				//playerAnim.AnimPause(true);
				Invincible = true; //Invincible while paused
				break;
			case PlayerState.Hitstun:
				position = Vector3.zero;
				//playerAnim.AnimPause(true);
				break;
			default:
				currentState = PlayerState.Idle;
				break;
		}
		
		//CurrentState special checks
		//IF not paused
		if (currentState != PlayerState.Paused)
		{
			//playerAnim.AnimPause(false);
		}

		//If not walk
		if (currentState != PlayerState.Walk)
		{
			moveSpeed.initialValue = 4;
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

	//Check if game is paused or if player is hitstunned
	public void CheckForPause() //Change this to a signal too!
	{
		if (PauseGame.IsPaused)
		{
			if (CanSetState)
			{
				CanSetState = false;
				//laststate = currentState;
			}
			currentState = PlayerState.Paused;
		}
	}

	//Returns the direction the player's animation is facing.
	//Probably needs to be changed for 3D player rotation!
	//public void GetDirection()
	//{
	//	if (targetMode.CanTarget)
	//	{
	//		switch (targetMode.direction)
	//		{
	//			case AnimatorDirection.Up:
	//				direction.initialPos = new Vector3(0, 1, 0);
	//				break;
	//			case AnimatorDirection.Down:
	//				direction.initialPos = new Vector3(0, -1, 0);
	//				break;
	//			case AnimatorDirection.Left:
	//				direction.initialPos = new Vector3(-1, 0, 0);
	//				break;
	//			case AnimatorDirection.Right:
	//				direction.initialPos = new Vector3(1, 0, 0);
	//				break;
	//			default:
	//				direction.initialPos = Vector3.zero;
	//				break;
	//		}
	//	}
	//	else
	//	{
	//		if (position != Vector3.zero)
	//		{
	//			if (Mathf.Abs(position.x) >= Mathf.Abs(position.y))
	//			{
	//				if (Mathf.Round(position.x) != 0)
	//				{
	//					direction.initialPos = new Vector3(Mathf.Round(position.x), 0, 0); //x dir
	//				}
	//			}
	//			else
	//			{
	//				if (Mathf.Round(position.y) != 0)
	//				{
	//					direction.initialPos = new Vector3(0, Mathf.Round(position.y), 0); //y dir
	//				}
	//			}
	//		}
	//	}
	//}

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
		//3D!
		//inputDirection = new Vector3(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"), 0).normalized;
	}
}