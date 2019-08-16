using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEditorInternal;
using UnityEngine;

public enum PlayerState
{
	Idle, Walk, Run, Slide, Attack, Paused, Dead, Hitstun
}
public class PlayerMovement3D : MonoBehaviour
{
	public float accelSpeed, gravity = 20f, slopeForce, slopeForceRayLength, accel, decel;//, _slopeLimit;
	public PlayerState currentState;
	public Collider playerCollider;
	public CharacterController controller;
	public Vector3 position, rawInputPos, newPos; //fixedInputPos,
	public PlayerAnimation playerAnim;
	//Player targeting here!
	public bool CanSetState, canDecelDelay, Invincible;
	public IntValue Health;
	public int CurrentHealth;
	public BoolValue EnemyCollision, canSwordAccel;
	
	private float horizontalspeed, verticalspeed;
	[SerializeField] private float SwordMomentum, SwordMomentumSmooth, SwordMomentumPower;
	public FloatValue SwordMomentumScale, moveSpeed;
	
	public Vector3Value direction, PlayerTransform, TargetTransform;

	public LayerMask slopeMask;
	
	//Test values for our smoothmove function.
	public float startValue, endValue, lerpValue;

	void Start()
	{
		playerCollider = gameObject.GetComponent<Collider>();
		controller = gameObject.GetComponent<CharacterController>();
		//controller.slopeLimit = _slopeLimit;
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
		
		//Debug.Log(position.x);
		controller.Move(newPos * Time.deltaTime);
		
		//Slope check
		//if (OnSlope())// && (Math.Abs(rawInputPos.x) > 0 || Math.Abs(rawInputPos.z) > 0)) //&& if player is moving (ADD THIS!)
		RaycastHit hit;
		if (Physics.Raycast(transform.position, Vector3.down, out hit, controller.height / 2 * slopeForceRayLength, slopeMask))
		{
			if (hit.normal != Vector3.up)
			{
				controller.Move(Vector3.down * playerCollider.bounds.size.y / 2 * slopeForce);
				bool slideable = Vector3.Angle(hit.normal, Vector3.up) >= controller.slopeLimit;
				if (slideable)
				{
					currentState = PlayerState.Slide;
					float slideSpeed = 35f;
					position.x = ((1f - hit.normal.y) * hit.normal.x) * slideSpeed;
					position.z = ((1f - hit.normal.y) * hit.normal.z) * slideSpeed;
				}
				else
				{
					currentState = PlayerState.Idle;
				}	
			}
			else
			{
				currentState = PlayerState.Idle;
			}
		}
		//If we check if the controller is grounded, this will no longer work.
		//The problem is that the player will "snap" to a slope if they get close to it after doing something like falling off a ledge.
		
				
		
		//Calculate the input position
		//*For the controller, instead of taking in every small movement, lets just put it into walk if the controller
		//is halfway, and run if the controller is pushed farther.

		if (currentState == PlayerState.Idle || currentState == PlayerState.Run || currentState == PlayerState.Walk)
		{
			rawInputPos = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized;

			position.x = rawInputPos.x;
			position.z = rawInputPos.z;
			
			//MoveSpeed only applied to x and z.
			position.x *= moveSpeed.initialValue; 
			position.z *= moveSpeed.initialValue;
		}

		if (currentState != PlayerState.Attack)
		{
			canSwordAccel.initialBool = true;
			startValue = 0;
			endValue = 0;
		}

		newPos.x = CalculateAccel(newPos.x, position.x);
		newPos.z = CalculateAccel(newPos.z, position.z);
		newPos.y = position.y;
		
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
	
	Vector3 MoveTo(Vector3 pos)
	{
		return (pos - transform.position).normalized * moveSpeed.initialValue;
	}

	//Calculate acceleration by comparing newPos to position.
	private float CalculateAccel(float pos, float targetpos)
	{
		if (pos < targetpos)
			if (Math.Abs(targetpos - pos) > accel)
				pos += accel;
			else
				pos += Math.Abs(targetpos - pos);
		if (pos > targetpos)
			if (Math.Abs(targetpos - pos) > decel)
				pos -= decel;
			else
				pos -= Math.Abs(targetpos - pos);
		return pos;
	}

	//Test function to move in a direction with accel, decel, power, and a wait time
	void SwordMove(Vector3 pos, float _accel, float _decel, float maxSpeed, float time)
	{
		Debug.Log(newPos);
		if (canSwordAccel.initialBool)
		{
			StartCoroutine(waitBeforeDecel());
			endValue = 0; //Reset
			startValue += _accel * Time.deltaTime;
			//if (Math.Abs(position.x) < maxSpeed && Math.Abs(position.z) < maxSpeed) //*Doesn't work for diagonal movement!
			if (pos.x != 0)
				if (Math.Abs(position.x) <= Math.Abs(maxSpeed * pos.x))
					position.x += (startValue * pos.x);
				else
					if (canDecelDelay)
						canSwordAccel.initialBool = false;

			if (pos.z != 0)
				if (Math.Abs(position.z) <= Math.Abs(maxSpeed * pos.z))
					position.z += (startValue * pos.z);
				else
					if (canDecelDelay)
						canSwordAccel.initialBool = false;
		}
		else
		{
			canDecelDelay = false;
			startValue = 0;
			endValue += _decel * Time.deltaTime;
			if (Math.Abs(position.x) - endValue > 0)
				position.x -= (endValue * pos.x);
			else
				position.x -= position.x;
			
			if (Math.Abs(position.z) - endValue > 0)
				position.z -= (endValue * pos.z);
			else
				position.z -= position.z;
		}
	}
	//The accel and decel seems swapped depending which direction is being faced

	private IEnumerator waitBeforeDecel()
	{
		yield return CustomTimer.Timer(.05f);
		canDecelDelay = true;
	}

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
			//default:
			//	currentState = PlayerState.Idle;
			//	break;
		}

		switch (currentState)
		{
			case PlayerState.Attack:
				accel = .9f;
				decel = .6f;
				break;
			case PlayerState.Slide:
				accel = .2f;
				decel = .2f;
				break;
			default:
				accel = .7f;
				decel = .6f;
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
//With this new acceleration, player jitters a lot compared to old script.
//If player is stopped (for example, up against a wall), newPos should be set to 0 in that direction. Otherwise there's accel time
//to go back positive from that point.

//NOTES
//On the 60hz monitor the player seems to jitter very briefly after changing input position.
