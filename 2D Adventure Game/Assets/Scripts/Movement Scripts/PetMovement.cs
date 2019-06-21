using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PetState
{
	Attack, Heel, Idle, Run, Walk
}

public class PetMovement : MonoBehaviour {

	public Transform target;
	public BoolValue CanFollowEnemy;
	public bool CanFollowPlayer, FollowPath, Attacking, CanAttack, Jumping, CanGetTargetDir;
	public Vector3Value enemyPos;
	public Vector3 position, difference;
	
	public float ChaseRadius, WalkRadius, StopRadius, WarpRadius, MoveSpeed, JumpMomentum, JumpMomentumScale;
	public Rigidbody2D rb;

	public PetState CurrentState;

	public FloatValue playerMoveSpeed;
	public UnitFollow path;
	
	void Start ()
	{
		CanAttack = true;
		CurrentState = PetState.Idle;
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
			if (CurrentState != PetState.Attack)
			{
				CurrentState = PetState.Attack;
			}
			else
			{
				CurrentState = PetState.Idle;
			}
		}
		
		if (CurrentState!=PetState.Attack)
		{
			CheckPath();
			CanFollowEnemy.initialBool = false;
		}
		
		//If pet is too far, teleport
		if (Vector3.Distance(target.position, transform.position) > WarpRadius)
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
				CanFollowEnemy.initialBool = true;
				break;
			default:
				MoveSpeed = 0;
				break;
		}
	}

	void CheckPath()
	{
		if (FollowPath)
		{
			path.CheckIfCanFollowPath();
			CanFollowPlayer = false;
		}
		
		if (!FollowPath)
		{
			path.StopFollowPath();
		}

		if (path.CannotReachPlayer)
		{
			OutOfRange();
			path.CannotReachPlayer = false;
		}
		
		//Check any collision between pet and player
		//RaycastHit hit;
		int wallLayerMask = 1 << 9;
		if (Physics2D.Linecast(transform.position, target.position, wallLayerMask))//, 15, wallLayerMask))
		{
			FollowPath = true;
			Debug.DrawLine(transform.position, target.position, Color.yellow);
		}
		else
		{
			FollowPath = false;
		}
	}
		
	void CheckDistance()
	{
		if (CurrentState != PetState.Attack)
		{
			if (Vector3.Distance(target.position, transform.position) <= ChaseRadius
			    && Vector3.Distance(target.position, transform.position) > WalkRadius)
			{
				CanFollowPlayer = true;
				CurrentState = PetState.Run;
			}

			if (Vector3.Distance(target.position, transform.position) <= WalkRadius
			    && Vector3.Distance(target.position, transform.position) > StopRadius)
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

			if (Vector3.Distance(target.position, transform.position) <= StopRadius)
			{
				CanFollowPlayer = false;
				CurrentState = PetState.Idle;
			}

			if (CanFollowPlayer)
			{
				Vector3 temp = Vector3.MoveTowards(transform.position, target.position, MoveSpeed * Time.deltaTime);
				rb.MovePosition(temp);
			}
		}
		else
		{
			if (Attacking)
			{
				Attack();
			}
			else
			{
				if (Vector3.Distance(enemyPos.initialPos, transform.position) <= WalkRadius)
				{
					if (!CanAttack) return; //Reduces nesting
					StartCoroutine(AttackEnemy());
					CanAttack = false;
				}
				else
				{
					Vector3 pos = Vector3.MoveTowards(transform.position, enemyPos.initialPos, MoveSpeed * Time.deltaTime);
					rb.MovePosition(pos);
				}
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
		transform.position = target.position;
		path.CanFollowPath = true;
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
		JumpMomentum = 0;
		JumpMomentumScale = 0;
		CanGetTargetDir = true;
	}

	private IEnumerator IdleWaitTime()
	{
		yield return new WaitForSeconds(.75f);
		CanFollowPlayer = true;
		CurrentState = PetState.Run;
	}
}

//TO DO

//Pathfinding works great. A new path is created when the pet gets out of range. Adjust the bools to work consistently
//and edit the warp radius a bit so the pet can actually complete certain paths.
