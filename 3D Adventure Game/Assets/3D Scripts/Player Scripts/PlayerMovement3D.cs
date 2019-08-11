using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEditorInternal;
using UnityEngine;

public class PlayerMovement3D : MonoBehaviour
{
	public float accelSpeed, gravity = 20f, slopeForce, slopeForceRayLength;
	public PlayerState currentState;
	public Collider playerCollider;
	public CharacterController controller;
	public Vector3 position, fixedInputPos, rawInputPos;
	public PlayerAnimation playerAnim;
	//Player targeting here!
	public bool CanSetState, accelerating, canDecelDelay, Invincible;
	public IntValue Health;
	public int CurrentHealth;
	public BoolValue EnemyCollision, canSwordAccel;
	
	private float horizontalspeed, verticalspeed;
	[SerializeField] private float SwordMomentum, SwordMomentumSmooth, SwordMomentumPower;
	public FloatValue SwordMomentumScale, moveSpeed;
	
	public Vector3Value direction, PlayerTransform, TargetTransform;
	
	//Test values for our smoothmove function.
	public float startValue, endValue, lerpValue;

	void Start()
	{
		playerCollider = gameObject.GetComponent<Collider>();
		controller = gameObject.GetComponent<CharacterController>();
		
		currentState = PlayerState.Idle;
		//SwordMomentumSmooth = 6f;
		//SwordMomentumPower = 10f;
		CanSetState = true;
		direction.initialPos = new Vector3(0, 0, -1); //For now, face towards the screen
		PlayerTransform.initialPos = transform.position;
	}

	void Update()
	{
		Debug.DrawRay(transform.position, direction.initialPos, Color.green);
		//Debug.Log(direction.initialPos);
		PlayerMovement();
		CheckForPause();
		CheckStates();
		//GetDirection();
		CheckHealth();
		PlayerTransform.initialPos = transform.position;
	}

	void PlayerMovement()
	{
		//Controller move
		
		Debug.Log(position.x);
		controller.Move(position * Time.deltaTime);
		
		//Slope check
		if (OnSlope())// && (Math.Abs(rawInputPos.x) > 0 || Math.Abs(rawInputPos.z) > 0)) //&& if player is moving (ADD THIS!)
		{
			controller.Move(Vector3.down * playerCollider.bounds.size.y / 2 * slopeForce);
		}
		
		//Calculate the input position
		//*For the controller, instead of taking in every small movement, lets just put it into walk if the controller
		//is halfway, and run if the controller is pushed farther.

		if (currentState == PlayerState.Idle || currentState == PlayerState.Run || currentState == PlayerState.Walk)
		{
			rawInputPos = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized;
			fixedInputPos.x = calcFixedInput(fixedInputPos.x, rawInputPos.x);
			fixedInputPos.z = calcFixedInput(fixedInputPos.z, rawInputPos.z);
			position.x = fixedInputPos.x;
			position.z = fixedInputPos.z;
			
			//MoveSpeed only applied to x and z.
			position.x *= moveSpeed.initialValue; 
			position.z *= moveSpeed.initialValue;
		}
		else
		{
			fixedInputPos = Vector3.zero; //Reset it
		}

		if (currentState != PlayerState.Attack)
		{
			canSwordAccel.initialBool = true;
			startValue = 0;
			endValue = 0;
			//ResetMove.initialBool = false;
		}
		
		//Is Grounded
		if (controller.isGrounded)
			position.y = 0;
		else
			position.y -= gravity * Time.deltaTime;
		
		//Gravity
		position.y -= gravity * Time.deltaTime; //DeltaTime applied here too for acceleration.

		//Set the direction.
		//The only time it ever resets to 0 is if the other input is 1 or -1.
		if (rawInputPos.x != 0)
			direction.initialPos.x = rawInputPos.x;
		if (rawInputPos.z != 0)
			direction.initialPos.z = rawInputPos.z;

		if (rawInputPos.x == -1 || rawInputPos.x == 1)
		{
			direction.initialPos.z = 0;
		}
		if (rawInputPos.z == -1 || rawInputPos.z == 1)
		{
			direction.initialPos.x = 0;
		}
		direction.initialPos.y = 0; //Never facing up or down
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
						newInput += Math.Abs(newInput - input); //Make sure this is frame independent!
					else
						newInput += accelSpeed; //
				else if (newInput > input)
					if (Math.Abs(newInput - input) < accelSpeed)
						newInput -= Math.Abs(newInput - input); //
					else
						newInput -= accelSpeed; //
		return newInput;
	}

	//Test function to move in a direction with accel, decel, power, and a wait time
	void SwordMove(Vector3 pos, float accel, float decel, float maxSpeed, float time)
	{
		if (canSwordAccel.initialBool)
		{
			StartCoroutine(waitBeforeDecel());
			endValue = 0; //Reset
			startValue += accel * Time.deltaTime;
			//if (Math.Abs(position.x) < maxSpeed && Math.Abs(position.z) < maxSpeed) //*Doesn't work for diagonal movement!
			if (pos.x != 0)
			{
				if (Math.Abs(position.x) <= Math.Abs(maxSpeed * pos.x))
				{
					position.x += (startValue * pos.x);
					//Prevents the position from adding a value that goes above maxSpeed.
					//if (Math.Abs(position.x) + startValue < maxSpeed)
					//{
					//	position.x += (startValue * pos.x);
					//}
					//else
					//{
					//	position.x += (maxSpeed * pos.x) - position.x;
					//}
				}
				else
				{
					if (canDecelDelay)
					{
						canSwordAccel.initialBool = false;
					}
				}
			}

			if (pos.z != 0)
			{
				if (Math.Abs(position.z) <= Math.Abs(maxSpeed * pos.z))
				{
					position.z += (startValue * pos.z);
					//if (Math.Abs(position.z) + startValue < maxSpeed)
					//{
					//	position.z += (startValue * pos.z);
					//}
					//else
					//{
					//	position.z += (maxSpeed * pos.z) - position.z;
					//}
				}
				else
				{
					if (canDecelDelay)
					{
						canSwordAccel.initialBool = false;
					}
				}
			}	
		}
		else
		{
			canDecelDelay = false;
			startValue = 0;
			endValue += decel * Time.deltaTime;
			if (Math.Abs(position.x) - endValue > 0)
			{
				position.x -= (endValue * pos.x);
			}
			else
			{
				position.x -= position.x;
			}
			
			if (Math.Abs(position.z) - endValue > 0)
			{
				position.z -= (endValue * pos.z);
			}
			else
			{
				position.z -= position.z;
			}
		}
	}

	private IEnumerator waitBeforeDecel()
	{
		yield return CustomTimer.Timer(.05f);
		canDecelDelay = true;
	}

	//----------------BACKUP IN CASE WE SCREW IT UP---------------
	//void SwordMove(Vector3 pos, float accel, float decel, float maxSpeed, float time)
	//{
	//	if (!canDecel)
	//	{
	//		endValue = 0; //Reset
	//		startValue += accel * Time.deltaTime;
	//		lerpValue = Mathf.Lerp(0, 1, startValue);
	//		if (Math.Abs(position.x) < maxSpeed && Math.Abs(position.z) < maxSpeed) //*Doesn't work for diagonal movement!
	//		{
	//			position.x += (lerpValue * pos.x);
	//			position.z += (lerpValue * pos.z);
	//		}
	//	}
	//	else
	//	{
	//		startValue = 0;
	//		endValue += decel * Time.deltaTime;
	//		lerpValue = Mathf.Lerp(0, 1, endValue);
	//		if (Math.Abs(position.x) > lerpValue)
	//		{
	//			position.x -= (lerpValue * pos.x);
	//		}
	//		else
	//		{
	//			position.x -= position.x;
	//		}
	//		
	//		if (Math.Abs(position.z) > lerpValue)
	//		{
	//			position.z -= (lerpValue * pos.z);
	//		}
	//		else
	//		{
	//			position.z -= position.z;
	//		}
	//	}
//
	//	if (startValue >= 1)
	//	{
	//		canDecel = true;
	//	}
	//}
	
	//Check if the ground normal isn't up.
	private bool OnSlope()
	{
		RaycastHit hit;
		if (Physics.Raycast(transform.position, Vector3.down, out hit, controller.height / 2 * slopeForceRayLength))
			if (hit.normal != Vector3.up)
				return true;
		return false;
	}
	
	//On Trigger with enemy
	void OnTriggerStay2D(Collider2D col)
	{
		if (!Invincible && col.gameObject.CompareTag("EnemyAttackHitbox"))
		{
			TakeDamage();
		}
	}
	
	//Take Damage
	void TakeDamage()
	{
		CurrentHealth -= 1; //1 damage for now
		//StartCoroutine(Invincibility());
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
				break;
			case PlayerState.Walk:
				//playerAnim.SetAnimState(AnimationState.Walk);
				moveSpeed.initialValue = 3;
				break;
			case PlayerState.Run:
				//playerAnim.SetAnimState(AnimationState.Run);
				break;
			case PlayerState.Attack:
				playerAnim.SetAnimState(AnimationState.SwordAttack);
				SwordMove(direction.initialPos, 18f, 6f, 6f, 0f); //Accel, decel, power, time
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
			moveSpeed.initialValue = 6;
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
//TO DO
//Change the sword lerp so the position isn't set to 0 once acceleration starts.
//Fix slope bumpin going down

//NOTES
//When transferring over things from our old player movement script, the player seems to either not move at all or move
//very slowly. When we transfer again, check the game after each segment to see what might cause it.
