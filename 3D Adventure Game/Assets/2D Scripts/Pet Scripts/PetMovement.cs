using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public enum PetState
{
	Attack, Heel, Idle, Run, Sit, Walk, Wait, Knocked
}

public class PetMovement : MonoBehaviour {

	public BoolValue EnemyExists, CanFollowPath, EnemyIsInLOS, PetIsAttacking;
	public bool CanFollowPlayer, FollowPath, Attacking, CanAttack, AttackMode, Jumping, 
		CanReachEnemy, EnemyLOS, CanGetReposDir, AttackAnyways, CanCheckDist, EnemyIsInRadius, CanAttackDelay;
	public Vector3Value enemyPos, PlayerTransform, TargetTransform, PetTransform;
	public Vector3 position, difference, RepositionDir;
	
	public float MoveSpeed, JumpMomentum, JumpMomentumScale, randX, randY;
	public Rigidbody2D rb;

	public PetAnimation PetAnim;
	public PetState CurrentState;

	public FloatValue playerMoveSpeed;
	public UnitFollow path;

	public Coroutine AttackCoroutine, AttackDelay;
	
	void Start ()
	{
		CanAttack = true;
		CurrentState = PetState.Idle;
		TargetTransform.initialPos = PlayerTransform.initialPos;
		CanFollowPath.initialBool = true;
		PetIsAttacking.initialBool = false;
		EnemyIsInLOS.initialBool = false;
		CanGetReposDir = true;
		CanReachEnemy = true;
		CanCheckDist = true;
		CanAttackDelay = true;
	}

	void FixedUpdate()
	{
		CheckDistance();

		if (Attacking)
		{
			rb.MovePosition(transform.position + position * MoveSpeed * Time.deltaTime);
		}
	}
	
	void Update ()
	{
		CheckPath();
		
		if (!PetIsAttacking.initialBool)
		{
			EnemyLOS = EnemyIsInLOS.initialBool; //Only updates if pet stops attacking
		}
		PetTransform.initialPos = transform.position;
		
		//Temporary way to set pet to attack state
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
				CurrentState = PetState.Idle;
			}
		}

		if (CurrentState != PetState.Attack)
		{
			TargetTransform.initialPos = PlayerTransform.initialPos;
		}
		else
		{
			TargetTransform.initialPos = enemyPos.initialPos;
		}
		
		//If pet is too far, teleport
		float WarpRadius = 12f;
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
			case PetState.Wait:
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
			if (CurrentState != PetState.Attack && CurrentState != PetState.Wait)
			{
				OutOfRange();
			}
			else
			{
				//DO SOMETHING WHEN IT CAN'T REACH THE ENEMY
				StartCoroutine(CantReachEnemyDelay());
			}
		}

		//ENEMY LINECAST CHECK
		if (CanCheckDist)
		{
			int wallLayerMask = 1 << 9;
			if (AttackMode && EnemyExists.initialBool && EnemyLOS) //EnemyFound means there's one in LOS of the PLAYER, not pet
			{
				if (Physics2D.Linecast(transform.position, enemyPos.initialPos, wallLayerMask))
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
	}
		
	void CheckDistance()
	{
		if (AttackMode && EnemyExists.initialBool && EnemyLOS && CanReachEnemy && EnemyIsInRadius) //If attackmode is enabled and an enemy target exists...
		{
			//Attack check
			PetIsAttacking.initialBool = true;
			CanFollowPlayer = false;
			CurrentState = PetState.Attack;
			if (Attacking)
			{
				JumpAttackMovement();
			}
			else //ATTACK CHECKS
			{
				float AttackRadius = 2f;
				float RepositionRadius = 1.75f;
				float PlayerRadius = 4f;
				if (CanAttack)
				{
					if (Vector3.Distance(enemyPos.initialPos, transform.position) <= AttackRadius && 
					    Vector3.Distance(enemyPos.initialPos, transform.position) > RepositionRadius)
					{
						AttackCoroutine = StartCoroutine(AttackEnemy());
						Debug.Log("Attack");
						CanAttack = false;
					}
					else
					{
						if (Vector3.Distance(enemyPos.initialPos, transform.position) <= RepositionRadius)
						{
							if (Vector3.Distance(PlayerTransform.initialPos, transform.position) <= PlayerRadius)
							{
								if (CanGetReposDir)
								{
									CanGetReposDir = false;
									
									randX = Random.Range(-1, 1);
									randY = Random.Range(-1, 1);
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
							}
							else
							{
								if (CanGetReposDir)
								{
									CanGetReposDir = false;
									RepositionDir = PlayerTransform.initialPos; //Player
								}
							}
						}
						else
						{
							CanGetReposDir = true;
							RepositionDir = enemyPos.initialPos; //Enemy
						}

						if (CanAttackDelay)
						{
							CanAttackDelay = false;
							AttackDelay = StartCoroutine(AttackEnemyDelay());
						}
						
						if (AttackAnyways)
						{
							Debug.Log("Attacking anyways");
							AttackAnyways = false;
							AttackCoroutine = StartCoroutine(AttackEnemy());
							CanAttack = false;
						}
						Vector3 pos = Vector3.MoveTowards(transform.position, RepositionDir, MoveSpeed * Time.deltaTime);
						rb.MovePosition(pos);
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
		}
		else //FOLLOW PLAYER CHECKS
		{
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
				float ChaseRadius = 12f;
				float WalkRadius = 1.5f;
				float StopRadius = .6f;
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
					Vector3 temp = Vector3.MoveTowards(transform.position, PlayerTransform.initialPos,
						MoveSpeed * Time.deltaTime);
					rb.MovePosition(temp);
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
			Debug.Log("Not within radius");
			EnemyIsInRadius = false;
		}
	}

	//Sets a delay to attack even if the pet isn't outside the reposition radius
	private IEnumerator AttackEnemyDelay()
	{
		yield return CustomTimer.Timer(.75f);
		AttackAnyways = true;
	}

	void JumpAttackMovement()
	{
		if (difference.magnitude > 1f)
		{
			difference = difference.normalized; //Prevents long jumps
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
		Debug.Log("Pet OutOfRange");
		transform.position = PlayerTransform.initialPos;
		CurrentState = PetState.Idle;
		ResetAttack();
	}

	//When a path can't be created to the enemy...
	private IEnumerator CantReachEnemyDelay()
	{
		CanReachEnemy = false;
		CurrentState = PetState.Wait;
		Debug.Log("Entered wait state");
		CanCheckDist = false;
		yield return CustomTimer.Timer(.75f);
		CanCheckDist = true;
	}

	private IEnumerator AttackEnemy()
	{
		yield return CustomTimer.Timer(.3f); //Leap forward
		PetAnim.SetAttack(true); //Temp anim
		difference = enemyPos.initialPos - transform.position;
		Attacking = true;
		Jumping = true;
		yield return CustomTimer.Timer(.5f); //Leap back
		PetAnim.SetAttack(false); //Temp anim
		JumpMomentum = 0;
		JumpMomentumScale = 0;
		Jumping = false;
		yield return CustomTimer.Timer(1f);
		ResetAttack();
	}

	void ResetAttack()
	{
		CanAttackDelay = true;
		CanGetReposDir = true;
		CanAttack = true;
		Attacking = false;
		Jumping = false;
		JumpMomentum = 0;
		JumpMomentumScale = 0;
	}

	private IEnumerator IdleWaitTime() //*Fix this to only run once
	{
		yield return new WaitForSeconds(.75f);
		CanFollowPlayer = true;
	}
}

//TO DO
//1: Cleanup and test for bugs

//Known issues:
