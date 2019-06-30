using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public enum PetState
{
	Attack, Heel, Idle, Run, Walk
}

public class PetMovement : MonoBehaviour {

	//public Transform target;
	public BoolValue EnemyExists, CanFollowPath, EnemyFound, PetIsAttacking;
	public bool CanFollowPlayer, FollowPath, Attacking, CanAttack, AttackMode, Jumping, CanGetTargetDir;
	public Vector3Value enemyPos, PlayerTransform, TargetTransform;
	public Vector3 position, difference;
	
	public float ChaseRadius, WalkRadius, StopRadius, WarpRadius, MoveSpeed, JumpMomentum, JumpMomentumScale;
	public Rigidbody2D rb;

	public PetState CurrentState;

	public FloatValue playerMoveSpeed;
	public UnitFollow path;

	public Coroutine AttackCoroutine;
	
	void Start ()
	{
		CanAttack = true;
		CurrentState = PetState.Idle;
		TargetTransform.initialPos = PlayerTransform.initialPos;
		//CanReachEnemy.initialBool = false;
		CanFollowPath.initialBool = true;
		PetIsAttacking.initialBool = false;
	}

	void FixedUpdate()
	{
		if (!FollowPath)
		{
			CheckDistance();
		}

		if (Attacking)
		{
			rb.MovePosition(transform.position + position * MoveSpeed * Time.deltaTime);
		}
	}
	
	void Update () 
	{
		//Temporary way to set pet to attack state
		if (Input.GetKeyDown(KeyCode.LeftShift))
		{
			path.StopFollowPath();
			//CanFollowPath.initialBool = true;
			if (!AttackMode)
			{
				AttackMode = true;
			}
			else
			{
				AttackMode = false;
				CurrentState = PetState.Idle;
			}
		}
		
		//***Need a way to send a signal to the pet to let it know its target is dead. Then...
		//EnemyLOS.initialBool = false;

		if (CurrentState != PetState.Attack)
		{
			TargetTransform.initialPos = PlayerTransform.initialPos;
		}
		else
		{
			TargetTransform.initialPos = enemyPos.initialPos;
		}
		
		CheckPath();
		
		//If pet is too far, teleport
		if (Vector3.Distance(PlayerTransform.initialPos, transform.position) > WarpRadius)
		{
			OutOfRange();
		}

		//Movespeed
		switch (CurrentState)
		{
			case PetState.Run:
				MoveSpeed = playerMoveSpeed.initialValue;
				break;
			case PetState.Walk:
				MoveSpeed = 1;
				break;
			case PetState.Idle:
				MoveSpeed = 0;
				break;
			case PetState.Attack:
				MoveSpeed = 4;
				break;
			default:
				MoveSpeed = 0;
				break;
		}
	}

	void CheckPath()
	{
		if (FollowPath && CanFollowPath.initialBool)
		{
			CanFollowPath.initialBool = false;
			path.CheckIfCanFollowPath();
			CanFollowPlayer = false;
		}
		
		if (!FollowPath)
		{
			path.StopFollowPath();
			//CanFollowPath.initialBool = true;
		}

		if (path.CannotReachPlayer) //Runs once
		{
			path.CannotReachPlayer = false;
			if (CurrentState != PetState.Attack)
			{
				OutOfRange();
			}
			else
			{
				//DO SOMETHING WHEN IT CAN'T REACH THE ENEMY
				transform.position = enemyPos.initialPos;
			}
		}

		//ENEMY LINECAST CHECK
		int wallLayerMask = 1 << 9;
		if (AttackMode && EnemyExists.initialBool && EnemyFound) //EnemyFound means there's one in LOS of the PLAYER, not pet
		{
			if (Physics2D.Linecast(transform.position, enemyPos.initialPos, wallLayerMask))
			{
				FollowPath = true;
				Debug.DrawLine(transform.position, enemyPos.initialPos, Color.yellow);
			}
			else
			{
				FollowPath = false;
			}
		}
		else
		{
			//Player check if the above condition isn't met
			if (Physics2D.Linecast(transform.position, PlayerTransform.initialPos, wallLayerMask))
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
		
	void CheckDistance()
	{
		if (AttackMode && EnemyExists.initialBool) //If attackmode is enabled and an enemy target exists...
		{
			//Attack check
			PetIsAttacking.initialBool = true;
			CanFollowPlayer = false;
			CurrentState = PetState.Attack;
			if (Attacking)
			{
				Attack();
			}
			else
			{
				if (Vector3.Distance(enemyPos.initialPos, transform.position) <= WalkRadius)
				{
					if (CanAttack)
					{
						AttackCoroutine = StartCoroutine(AttackEnemy());
						CanAttack = false;
					}
				}
				else
				{
					Vector3 pos = Vector3.MoveTowards(transform.position, enemyPos.initialPos, MoveSpeed * Time.deltaTime);
					rb.MovePosition(pos);
				}
			}
		}
		else
		{
			PetIsAttacking.initialBool = false;
			if (AttackCoroutine != null)
			{
				ResetAttack();
				StopCoroutine(AttackCoroutine);
			}
			if (Vector3.Distance(PlayerTransform.initialPos, transform.position) <= ChaseRadius
			    && Vector3.Distance(PlayerTransform.initialPos, transform.position) > WalkRadius)
			{
				CanFollowPlayer = true;
				CurrentState = PetState.Run;
			}

			if (Vector3.Distance(PlayerTransform.initialPos, transform.position) <= WalkRadius
			    && Vector3.Distance(PlayerTransform.initialPos, transform.position) > StopRadius)
			{
				if (CurrentState == PetState.Idle)
				{
					StartCoroutine(IdleWaitTime());
				}
				else
				{
					CanFollowPlayer = true;
					CurrentState = PetState.Walk;
				}
			}

			if (Vector3.Distance(PlayerTransform.initialPos, transform.position) <= StopRadius)
			{
				CanFollowPlayer = false;
				CurrentState = PetState.Idle;
			}

			if (CanFollowPlayer)
			{
				Vector3 temp = Vector3.MoveTowards(transform.position, PlayerTransform.initialPos, MoveSpeed * Time.deltaTime);
				rb.MovePosition(temp);
			}
		}
	}

	void Attack()
	{
		if (CanGetTargetDir)
		{
			CanGetTargetDir = false;
			difference = enemyPos.initialPos - transform.position;
		}
		float smooth = 4f;
		float power = 2.5f;
		JumpMomentumScale += smooth * Time.deltaTime;
		JumpMomentum = Mathf.Lerp(power, 0, JumpMomentumScale);

		if (Jumping)
		{
			position = (JumpMomentum * difference);
		}
		else
		{
			position = (JumpMomentum * -difference);
		}
	}
	
	void OutOfRange()
	{
		Debug.Log("OutOfRange");
		transform.position = PlayerTransform.initialPos;
		//CanFollowPath = true;
		CurrentState = PetState.Idle;
	}

	private IEnumerator AttackEnemy()
	{
		yield return CustomTimer.Timer(.5f);
		Attacking = true;
		Jumping = true;
		yield return CustomTimer.Timer(.5f);
		JumpMomentum = 0;
		JumpMomentumScale = 0;
		Jumping = false;
		yield return CustomTimer.Timer(1f);
		ResetAttack();
	}

	void ResetAttack()
	{
		CanAttack = true;
		Attacking = false;
		Jumping = false;
		JumpMomentum = 0;
		JumpMomentumScale = 0;
		CanGetTargetDir = true;
	}

	private IEnumerator IdleWaitTime() //*Fix this to only run once
	{
		yield return new WaitForSeconds(.75f);
		CanFollowPlayer = true;
		//CurrentState = PetState.Run; //DON'T LEAVE THIS IN
	}
}

//TO DO

//1: Prevent the array out of index error when the player toggles Attackmode over and over rapidly.

//2: Tell the pet to do something when it can't reach the enemy instead of just teleporting to it. (6/24/19)

//3: Do a radius check on the player before telling the pet to attack something. Right now the pet just attacks the
//closest enemy on the list, but it should also check to make sure it's within the player's radius. (6/22/19)

//Known issues:

//1: Range exception error is caused when a path is requested too many times in a row...

//2: THE DUMB IDLEWAITTIME COROUTINE CAUSES THE ATTACK STATE TO FIGHT WITH THE RUN STATE AND CAUSED ALL THE PROBLEMS
