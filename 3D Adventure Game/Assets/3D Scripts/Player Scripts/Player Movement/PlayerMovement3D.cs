using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEditor.VersionControl;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Networking;

public enum PlayerState
{
	Idle, Walk, Run, Attack, Slide, Hitstun, Dead, Paused,
}
public class PlayerMovement3D : MonoBehaviour
{
	public float gravity = 20f, slopeForce, slopeForceRayLength, accel, decel;//, _slopeLimit;
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
	public FloatValue moveSpeed;

	public Vector3Value direction, PlayerTransform;

	public LayerMask slopeMask;
	//public List<PlayerStateClass> stateList;
	public List<int> stateList, activeStateList;
	
	//Test values for our smoothmove function.
	public float startValue, endValue;

	public Vector3 spherepos;
	public float sphereradius;

	void Start()
	{
		//CreateStates();
		playerCollider = gameObject.GetComponent<Collider>();
		controller = gameObject.GetComponent<CharacterController>();
		currentState = PlayerState.Idle;
		CanSetState = true;
		direction.initialPos = new Vector3(0, 0, -1); //For now, face towards the screen
		PlayerTransform.initialPos = transform.position;
	}

	void Update()
	{
		Debug.DrawRay(transform.position, direction.initialPos, Color.green);
		PlayerMovement();
		CheckForPause();
		CheckStates();
		CheckHealth();
		//CheckStatePriority();
		PlayerTransform.initialPos = transform.position;
		SetState(PlayerState.Idle, true); //Idle is always active

		//if (!controller.isGrounded)
		//{
		//	Debug.Log("Not on the ground");
		//}
	}

	void OnDrawGizmos()
	{
		Gizmos.DrawSphere(spherepos, sphereradius);
	}

	void PlayerMovement()
	{
		//Controller move
		controller.Move(newPos * Time.deltaTime);
		
		//Slope check
		//if (OnSlope())// && (Math.Abs(rawInputPos.x) > 0 || Math.Abs(rawInputPos.z) > 0)) //&& if player is moving (ADD THIS!)
		RaycastHit hit;
		//if (Physics.Raycast(transform.position, Vector3.down, out hit, controller.height / 2 * slopeForceRayLength, slopeMask))
		float radius = controller.radius;
		Vector3 pos = transform.position + Vector3.down * (radius);
		bool isOnGround = Physics.SphereCast(pos, radius, Vector3.down, out hit, slopeForceRayLength, slopeMask);
		spherepos = pos;
		sphereradius = radius;
		Debug.DrawRay(hit.point, Vector3.down, Color.red);
		if (isOnGround)
		{
			//slopeForceRayLength = 5f; //1 default
			//Debug.Log(hit.point);
			controller.Move(new Vector3(0, -.8f, 0) * 50 * Time.deltaTime);
			//controller.Move(Vector3.down * playerCollider.bounds.size.y / 2 * slopeForce * Time.deltaTime);
			float normalAngle = Vector3.Angle(hit.normal, Vector3.up);
			float anglePercent = (Math.Abs(Math.Abs(normalAngle) - 90) / 90);
			//Debug.Log(position);
			bool slideable = normalAngle >= controller.slopeLimit;

			//if (normalAngle >= -90 && normalAngle <= 90) //Ignore ceiling
			//{
			//	position.x *= anglePercent;
			//	position.z *= anglePercent;
			//	position.y *= 1 - anglePercent;
			//}
			
			
			
			if (slideable)
			{
				SetState(PlayerState.Slide, true);
				float slideSpeed = 35f;
				position.x = ((1f - hit.normal.y) * hit.normal.x) * slideSpeed;
				position.z = ((1f - hit.normal.y) * hit.normal.z) * slideSpeed;	
				if (position.x > 10)
					position.x = 10;
				if (position.z > 10)
					position.z = 10;
			}
			else
			{
				SetState(PlayerState.Slide, false);
			}	
		}
		else
		{
			slopeForceRayLength = .5f; //.1f default
			SetState(PlayerState.Slide, false);
		}
		//Current issues for SLOPE:
		//After walking off a ledge, the player will slide down steeper slopes faster
		//The player will still snap down to ledges within the .1f ray length if he drops from a tiny platform
		//(The player bounces down slopes within the slope angle)
		
				
		
		//Calculate the input position
		//*For the controller, instead of taking in every small movement, lets just put it into walk if the controller
		//is halfway, and run if the controller is pushed farther.
		rawInputPos = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized;
		
		if (currentState == PlayerState.Idle || currentState == PlayerState.Run || currentState == PlayerState.Walk)
		{
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

		//Temp for setting a run state
		if (position.x != 0 || position.z != 0)
		{
			SetState(PlayerState.Run, true);
		}
		else
		{
			SetState(PlayerState.Run, false);
		}

		newPos.x = CalculateAccel(newPos.x, position.x);
		newPos.z = CalculateAccel(newPos.z, position.z);
		newPos.y = position.y;
		
		//Is Grounded
		if (controller.isGrounded)
			position.y = 0;
		else
			if (Math.Abs(position.y) > 25) //25 = max falling speed.
				position.y = -25;
			else
				position.y -= gravity * Time.deltaTime;
		
		
		//Gravity
		//position.y -= gravity * Time.deltaTime; //DeltaTime applied here too for acceleration.

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
		//Debug.Log(newPos);
		if (canSwordAccel.initialBool)
		{
			Debug.Log("Can sword move");
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
			SetState(PlayerState.Dead, true);
			Debug.Log("Player has died");
		}
		else
		{
			SetState(PlayerState.Dead, false);
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
			SetState(PlayerState.Paused, true);
		}
		else
		{
			SetState(PlayerState.Paused, false);
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
		//PlayerState lastState = currentState;
		SetState(PlayerState.Hitstun, true);
		yield return CustomTimer.Timer(.075f);
		SetState(PlayerState.Hitstun, false);
		Debug.Log("Hitstun ended");
	}

	//Get sword swing direction, not called through update.
	public void GetSwordSwingDirection()
	{
		//3D!
		//inputDirection = new Vector3(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"), 0).normalized;
	}

	////HERE'S A GREAT EXAMPLE OF SOMETHING ONLY RUNNING ONCE WOO
	//public void SetState(PlayerState state)
	//{
	//	if (currentState != state && state != PlayerState.Idle)
	//	{
	//		//Debug.Log("Set state to " + state);
	//		currentState = state;
	//	}
	//	
	//	//Set Idle
	//	if (state == PlayerState.Idle && currentState != PlayerState.Attack && currentState != PlayerState.Paused
	//	    && currentState != PlayerState.Hitstun)
	//	{
	//		if (position == Vector3.zero || rawInputPos == Vector3.zero)
	//		{
	//			currentState = state;
	//		}
	//	}
	//}
	
	//Check which active state has the highest value
	public void CheckStatePriority()
	{
		currentState = (PlayerState) newState();
	}

	public int newState()
	{
		int i = 0;
		foreach (var c in activeStateList)
			if (c > i)
				i = c;
		return i;
	}

	//Set a state to active or inactive
	public void SetState(PlayerState state, bool _active)
	{
		int thing = (int) state;
		if (_active)
		{
			if (!activeStateList.Contains(thing))
			{
				activeStateList.Add(thing);
			}
		}
		else
		{
			if (activeStateList.Contains(thing))
			{
				activeStateList.Remove(thing);
			}
		}
		
		CheckStatePriority();
	}
}
//TO DO
//With this new acceleration, player jitters a lot compared to old script.
//If player is stopped (for example, up against a wall), newPos should be set to 0 in that direction. Otherwise there's accel time
//to go back positive from that point.

//NOTES
//We added a simple state machine that simply checks the order of the PlayerState enum and uses their values as "priority" values.
//We now set states to active or inactive. Then, the setstate function will simply check which state in the activeStateList has
//the highest value, and sets the currentState to that value.