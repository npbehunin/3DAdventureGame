using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.WSA;
using Random = System.Random;

public enum PetStatev2
{
	Idle, Walk, Run, Sit, Wait, PathFollow, EnemyFollow, EnemyRepos, AttackAnticipation, 
	AttackJump, AttackJumpBack, Knocked, Paused, Hitstun, Dead
}
public class PetMovement3D : MonoBehaviour
{
	public BoolValue EnemyExists, CanFollowPath, EnemyIsInLOS, PetIsAttacking;
	public bool FollowPath, AttackMode, Attacking, CanReachEnemy, EnemyLOS, EnemyIsInRadius, CanAttackDelay, 
		Invincible, CanSetRandPos;
	public Vector3Value enemyPos, PlayerTransform, TargetTransform, PetTransform, pathPos;
	public Vector3 position, newPos, difference, RepositionDir;
	
	public float MoveSpeed, JumpMomentum, JumpMomentumScale, randX, randY, gravity, slopeForce, slopeForceRayLength, accel, decel;
	public IntValue Health;
	public int CurrentHealth;
	public Collider petCollider;

	public PetAnimation PetAnim;
	public CharacterController controller;

	public FloatValue playerMoveSpeed;
	public UnitFollowNew path;

	public Coroutine AttackCoroutine, AttackDelay;
    public LayerMask wallLayerMask;

	public PetStatev2 currentState;
	
	void Start ()
	{
		currentState = PetStatev2.Idle;
		TargetTransform.initialPos = PlayerTransform.initialPos;
		CanFollowPath.initialBool = true;
		PetIsAttacking.initialBool = false;
		EnemyIsInLOS.initialBool = false;
		Attacking = false;
		PetTransform.initialPos = transform.position;
		CanAttackDelay = true;
		CanSetRandPos = true;
		controller = gameObject.GetComponent<CharacterController>();
		petCollider = gameObject.GetComponent<Collider>();
	}
	
	void Update () 
	{
		controller.Move(newPos * Time.deltaTime);
		
		switch (currentState)
		{
			case PetStatev2.Run:
				MoveSpeed = playerMoveSpeed.initialValue;
				break;
			case PetStatev2.Walk:
				MoveSpeed = 1;
				break;
			case PetStatev2.Idle:
			case PetStatev2.Wait:
			case PetStatev2.Hitstun:
				MoveSpeed = 0;
				break;
			case PetStatev2.Paused:
			case PetStatev2.Dead:
				Invincible = true; //Always invincible in these states
				MoveSpeed = 0;
				break;
			default:
				MoveSpeed = 6;
				break;
		}

		////Acceleration values
		//switch (currentState)
		//{
		//	case PetStatev2.PathFollow:
		//		accel = .05f;
		//		decel = .05f;
		//		break;
		//	default:
		//		accel = .4f;
		//		decel = .4f;
		//		break;
		//}

		//Used to be in FixedUpdate
		switch (currentState)
		{
			case PetStatev2.Idle:
			case PetStatev2.Walk:
			case PetStatev2.Run:
				position.x = MoveTo(PlayerTransform.initialPos).x;
				position.z = MoveTo(PlayerTransform.initialPos).z;
				break;
			case PetStatev2.PathFollow:
				position.x = MoveTo(new Vector3(pathPos.initialPos.x, 0, pathPos.initialPos.z)).x;
				position.z = MoveTo(new Vector3(pathPos.initialPos.x, 0, pathPos.initialPos.z)).z;
				break;
			case PetStatev2.Wait:
			case PetStatev2.Hitstun:
				break;
			case PetStatev2.AttackJump:
				position.x = JumpMovement(1).x;
				position.z = JumpMovement(1).z;
				break;
			case PetStatev2.AttackJumpBack:
				position.x = JumpMovement(-1).x;
				position.z = JumpMovement(-1).z;
				break;
			case PetStatev2.EnemyFollow:
			case PetStatev2.EnemyRepos:
				position.x = MoveTo(RepositionDir).x;
				position.z = MoveTo(RepositionDir).z;
				break;
		}
		
		if (OnSlope())
		{
			controller.Move(slopeForce * petCollider.bounds.size.y / 2 * Vector3.down);
		}
		
		//Is Grounded
		if (controller.isGrounded)
			position.y = 0;
		else
			position.y -= gravity * Time.deltaTime;
		
		//Gravity
		position.y -= gravity * Time.deltaTime; //DeltaTime applied here too for acceleration.

		newPos.x = CalculateAccel(newPos.x, position.x);
		newPos.z = CalculateAccel(newPos.z, position.z);
		newPos.y = position.y;
		
		//These were moved here so that OutOfRange checks will run and won't be overriden by controller.move functions.
		CheckPath();
		CheckConditions();
		CheckDistance();
		CheckHealth();
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
	
	private bool OnSlope()
	{
		RaycastHit hit;
		if (Physics.Raycast(transform.position, Vector3.down, out hit, controller.height / 2 * slopeForceRayLength))
			if (hit.normal != Vector3.up)
				return true;
		return false;
	}

	//Returns a normalized direction. Make sure to only set x and z.
	Vector3 MoveTo(Vector3 pos)
	{
		return (pos - transform.position).normalized * MoveSpeed;
	}

	Vector3 JumpMovement(float dir)
	{
		if (difference.magnitude > 1f)
		{
			difference = difference.normalized; //Prevents long jumps
		}
		float smooth = 4f;
		float power = 2.5f;
		JumpMomentumScale += smooth * Time.deltaTime;
		JumpMomentum = Mathf.Lerp(power, 0, JumpMomentumScale);
		Vector3 Movement = transform.position + (JumpMomentum * difference * dir) * MoveSpeed;
		return Movement;
	}

	//Collision detection
	void OnTriggerStay2D(Collider2D col)
	{
		if (!Invincible && AttackMode && col.gameObject.CompareTag("EnemyAttackHitbox")) //Can only get hurt in attackmode
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
			currentState = PetStatev2.Dead;
			Debug.Log("Pet has died");
		}
	}

	//Normal Update checks
	void CheckConditions()
	{
		//Fix!-------------
		if (!PetIsAttacking.initialBool)
		{
			EnemyLOS = EnemyIsInLOS.initialBool; //Only updates if pet stops attacking
		}
		PetTransform.initialPos = transform.position;
		
		if (Input.GetKeyDown(KeyCode.LeftShift))
		{
			path.StopFollowPath();
			if (!AttackMode)
			{
				AttackMode = true;
			}
			else
			{
				AttackMode = false;
				currentState = PetStatev2.Idle;
			}
		}

		if (FollowPath)
		{
			currentState = PetStatev2.PathFollow;
		}
		else
		{
			currentState = PetStatev2.Idle; //CHECK IF THIS WORKS WHILE ATTACKING AN ENEMY
		}
	}

	//Distance and state checking
	void CheckDistance()
	{
		//If pet is too far, teleport
		float WarpRadius = 18f;
		if (Vector3.Distance(PlayerTransform.initialPos, transform.position) > WarpRadius)
		{
			OutOfRange();
		}
		
		//Fix!-------------
		if (AttackMode && EnemyExists.initialBool && EnemyLOS && CanReachEnemy && EnemyIsInRadius) //If attackmode is enabled and an enemy target exists...
		{
			//Attack check
			TargetTransform.initialPos = enemyPos.initialPos;
			PetIsAttacking.initialBool = true;
			
			float AttackRadius = 2f;
			float RepositionRadius = 1.75f;
			float PlayerRadius = 4f;
			if (!Attacking)
			{
				if (Vector3.Distance(enemyPos.initialPos, transform.position) <= AttackRadius && 
				    Vector3.Distance(enemyPos.initialPos, transform.position) > RepositionRadius)
				{
					AttackCoroutine = StartCoroutine(AttackEnemy());
					//Debug.Log("Attack");
				}
				else
				{
					if (Vector3.Distance(enemyPos.initialPos, transform.position) <= RepositionRadius)
					{
						if (CanSetRandPos)
						{
							CanSetRandPos = false;
							currentState = PetStatev2.EnemyRepos; //Runs once from here
							if (Vector3.Distance(PlayerTransform.initialPos, transform.position) <= PlayerRadius)
							{
								randX = UnityEngine.Random.Range(-1, 1); //Avoid .net conflict by using UnityEngine
								randY = UnityEngine.Random.Range(-1, 1);
								if (randX == 0)
								{
									randX = 1f;
								}
								if (randY == 0)
								{
									randY = 1f;
								}
								RepositionDir = new Vector3(transform.position.x + randX, transform.position.y + randY);
							}
							else
							{
								RepositionDir = PlayerTransform.initialPos; //Reposition towards the player
							}
						}
						else
						{
							//Do nothing until an attack
						}
						
						if (CanAttackDelay) //Keeping for now
						{
							CanAttackDelay = false;
							AttackDelay = StartCoroutine(AttackEnemyDelay());
						}
					}
					else
					{
						currentState = PetStatev2.EnemyFollow;
						RepositionDir = enemyPos.initialPos; //Enemy
					}	
				}
			}
			else
			{
				if (AttackDelay != null)
				{
					StopCoroutine(AttackDelay);
				}
			}
		}
		else //FOLLOW PLAYER CHECKS
		{
			if (!Attacking) //Lets the attack finish before switching back
			{
				TargetTransform.initialPos = PlayerTransform.initialPos;
				PetIsAttacking.initialBool = false;
				if (AttackCoroutine != null)
				{
					ResetAttack();
					StopCoroutine(AttackCoroutine);
				}

				if (AttackDelay != null)
				{
					StopCoroutine(AttackDelay);
				}

				if (!FollowPath)
				{
					float ChaseRadius = 18f;
					float WalkRadius = 3f;
					float StopRadius = 2.5f;
					if (Vector3.Distance(PlayerTransform.initialPos, transform.position) <= ChaseRadius
					    && Vector3.Distance(PlayerTransform.initialPos, transform.position) > WalkRadius)
					{
						currentState = PetStatev2.Run;
					}

					if (Vector3.Distance(PlayerTransform.initialPos, transform.position) <= WalkRadius
					    && Vector3.Distance(PlayerTransform.initialPos, transform.position) > StopRadius)
					{
						if (currentState == PetStatev2.Idle)
						{
							StartCoroutine(IdleWaitTime()); //Wait a bit before walking
						}
						else
						{
							currentState = PetStatev2.Walk;
						}
					}

					if (Vector3.Distance(PlayerTransform.initialPos, transform.position) <= StopRadius)
					{
						currentState = PetStatev2.Idle;
					}
				}
			}
		}

		//How far the pet can keep attacking before returning to the player
		float DetectionRadiusX = 10f;
		float DetectionRadiusY = 6f;
		if (Mathf.Abs(enemyPos.initialPos.x - PlayerTransform.initialPos.x) <= DetectionRadiusX &&
		    Mathf.Abs(enemyPos.initialPos.y - PlayerTransform.initialPos.y) <= DetectionRadiusY)
		{
			EnemyIsInRadius = true;
		}
		else
		{
			//Debug.Log("Not within radius");
			EnemyIsInRadius = false;
		}
	}

	//Pathfinding checks
	void CheckPath()
	{
		if (FollowPath && CanFollowPath.initialBool)
		{
			CanFollowPath.initialBool = false;
			path.CheckIfCanFollowPath(TargetTransform.initialPos);
		}
		
		if (!FollowPath)
		{
			path.StopFollowPath();
			CanFollowPath.initialBool = true;
		}

		if (!path.CanReachTarget) //Runs once
		{
			path.CanReachTarget = true;
			if (TargetTransform.initialPos == PlayerTransform.initialPos)
			{
				OutOfRange();
			}
			//else
			{
				//DO SOMETHING WHEN IT CAN'T REACH THE ENEMY
				StartCoroutine(CantReachEnemyDelay());
			}
		}

		//ENEMY LINECAST CHECK
		if (currentState != PetStatev2.Wait) 
		{
			//int wallLayerMask = 1 << 10;
			if (AttackMode && EnemyExists.initialBool && EnemyLOS) //EnemyFound means there's one in LOS of the PLAYER, not pet
			{
				if (Physics.Linecast(transform.position, enemyPos.initialPos, wallLayerMask))
				{
					FollowPath = true;
					Debug.DrawLine(transform.position, enemyPos.initialPos, Color.yellow);
				}
				else
				{
					CanReachEnemy = true;
					FollowPath = false; //*Prone to getting stuck in thin sections where it can see it, but can't reach
				}
			}
			else if (!Attacking)
			{
				//Player check if the above condition isn't met
				if (Physics.Linecast(transform.position, PlayerTransform.initialPos, wallLayerMask))
				{
					FollowPath = true;
					Debug.DrawLine(transform.position, PlayerTransform.initialPos, Color.yellow);
				}
				else
				{
					FollowPath = false;
				}
			}
		}
	}

	void OutOfRange()
	{
		Debug.Log("Pet OutOfRange");
		transform.position = PlayerTransform.initialPos;
		//transform.position = PlayerTransform.initialPos;
		ResetAttack();
	}

	void ResetAttack()
	{
		CanAttackDelay = true;
		JumpMomentum = 0;
		JumpMomentumScale = 0;
		currentState = PetStatev2.EnemyFollow;
		Attacking = false;
		CanSetRandPos = true;
	}
	
	//Hitstun signal
	public void StartHitstun()
	{
		StartCoroutine(HitstunCo());
	}
	
	//Hitstun coroutine
	public IEnumerator HitstunCo()
	{
		PetStatev2 lastState = currentState;
		currentState = PetStatev2.Hitstun;
		yield return CustomTimer.Timer(.075f);
		currentState = lastState;
	}
	
	//When a path can't be created to the enemy...
	private IEnumerator CantReachEnemyDelay()
	{
		CanReachEnemy = false;
		currentState = PetStatev2.Wait;
		//Debug.Log("Entered wait state");
		yield return CustomTimer.Timer(.75f);
		currentState = PetStatev2.Idle;
	}
	
	private IEnumerator IdleWaitTime() //*Fix this to only run once
	{
		yield return new WaitForSeconds(.75f);
		currentState = PetStatev2.Walk;
	}
	
	//Sets a delay to attack even if the pet isn't outside the reposition radius
	private IEnumerator AttackEnemyDelay()
	{
		yield return CustomTimer.Timer(.75f);
		//Debug.Log("Attacking anyways");
		AttackCoroutine = StartCoroutine(AttackEnemy());
	}
	
	private IEnumerator AttackEnemy()
	{
		Attacking = true;
		currentState = PetStatev2.AttackAnticipation;
		yield return CustomTimer.Timer(.3f); //Leap forward
		PetAnim.SetAttack(true); //Temp anim
		difference = enemyPos.initialPos - transform.position;
		currentState = PetStatev2.AttackJump;
		yield return CustomTimer.Timer(.5f); //Leap back
		currentState = PetStatev2.AttackJumpBack;
		PetAnim.SetAttack(false); //Temp anim
		JumpMomentum = 0;
		JumpMomentumScale = 0;
		yield return CustomTimer.Timer(1f);
		ResetAttack();
	}

	private IEnumerator Invincibility()
	{
		Invincible = true;
		yield return CustomTimer.Timer(2f);
		Invincible = false;
	}
}

//TO DO:

//KNOWN ISSUES:
//At the beginning of execution the pet will run towards the player first instead of the enemy.
//After killing an enemy the pet immediately stops its attack and goes back to idle.
