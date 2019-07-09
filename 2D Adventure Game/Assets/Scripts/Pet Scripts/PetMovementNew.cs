using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PetStatev2
{
	Idle, Walk, Run, Sit, Wait, PathFollow, EnemyFollow, EnemyRepos, AttackAnticipation, AttackJump, AttackJumpBack, Knocked
}
public class PetMovementNew : MonoBehaviour
{
	public BoolValue EnemyExists, CanFollowPath, EnemyIsInLOS, PetIsAttacking;
	public bool FollowPath, AttackMode, CanReachEnemy, EnemyLOS, EnemyIsInRadius, CanAttackDelay;
	public Vector3Value enemyPos, PlayerTransform, TargetTransform, PetTransform;
	public Vector3 position, difference, RepositionDir;
	
	public float MoveSpeed, JumpMomentum, JumpMomentumScale, randX, randY;
	public Rigidbody2D rb;

	public PetAnimation PetAnim;

	public FloatValue playerMoveSpeed;
	public UnitFollow path;

	public Coroutine AttackCoroutine, AttackDelay;

	public PetStatev2 currentState;
	
	void Start ()
	{
		currentState = PetStatev2.Idle;
	}
	
	void Update () 
	{
		CheckDistance();
		CheckPath();
		CheckConditions();
		switch (currentState)
		{
			case PetStatev2.Run:
				MoveSpeed = playerMoveSpeed.initialValue;
				break;
			case PetStatev2.Walk:
				MoveSpeed = 1;
				break;
			case PetStatev2.Idle:
				MoveSpeed = 0;
				break;
			case PetStatev2.Wait:
				MoveSpeed = 0;
				break;
			case PetStatev2.AttackJump:
				MoveSpeed = 4;
				break;
			default:
				MoveSpeed = 0;
				break;
		}
	}

	void FixedUpdate()
	{
		rb.MovePosition(position);
		switch (currentState)
		{
			case PetStatev2.Idle:
			case PetStatev2.Walk:
			case PetStatev2.Run:
				position = MoveTo(PlayerTransform.initialPos);
				break;
			case PetStatev2.Wait:
				break;
			case PetStatev2.AttackJump:
				position = JumpMovement();
				break;
			case PetStatev2.AttackJumpBack:
				position = -JumpMovement();
				break;
			case PetStatev2.EnemyFollow:
				position = MoveTo(RepositionDir);
				break;
		}
	}

	Vector3 MoveTo(Vector3 pos)
	{
		return Vector3.MoveTowards(transform.position, pos, MoveSpeed * Time.deltaTime);
	}

	Vector3 JumpMovement()
	{
		if (difference.magnitude > 1f)
		{
			difference = difference.normalized; //Prevents long jumps
		}
		float smooth = 4f;
		float power = 2.5f;
		JumpMomentumScale += smooth * Time.deltaTime;
		JumpMomentum = Mathf.Lerp(power, 0, JumpMomentumScale);
		Vector3 Movement = transform.position + (JumpMomentum * difference) * MoveSpeed * Time.deltaTime;
		return Movement;
	}

	void CheckConditions()
	{
		//Fix!-------------
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
				currentState = PetStatev2.Idle;
			}
		}
	}

	void CheckDistance()
	{
		//If pet is too far, teleport
		float WarpRadius = 12f;
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
			{
				float AttackRadius = 2f;
				float RepositionRadius = 1.75f;
				float PlayerRadius = 4f;
				if (currentState == PetStatev2.EnemyFollow || currentState == PetStatev2.EnemyRepos)
				{
					if (Vector3.Distance(enemyPos.initialPos, transform.position) <= AttackRadius && 
					    Vector3.Distance(enemyPos.initialPos, transform.position) > RepositionRadius)
					{
						AttackCoroutine = StartCoroutine(AttackEnemy());
						Debug.Log("Attack");
					}
					else
					{
						if (Vector3.Distance(enemyPos.initialPos, transform.position) <= RepositionRadius)
						{
							if (currentState == PetStatev2.EnemyFollow)
							{
								currentState = PetStatev2.EnemyRepos; //Runs once from here
								if (Vector3.Distance(PlayerTransform.initialPos, transform.position) <= PlayerRadius)
								{
									currentState = PetStatev2.EnemyRepos;
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
								else
								{
									RepositionDir = PlayerTransform.initialPos; //Reposition towards the player
								}
							}
						}
						else
						{
							currentState = PetStatev2.EnemyFollow;
							RepositionDir = enemyPos.initialPos; //Enemy
						}

						if (CanAttackDelay) //Keeping for now
						{
							CanAttackDelay = false;
							AttackDelay = StartCoroutine(AttackEnemyDelay());
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
		}
		else //FOLLOW PLAYER CHECKS
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
				float ChaseRadius = 12f;
				float WalkRadius = 1.5f;
				float StopRadius = .6f;
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

	void CheckPath()
	{
		if (FollowPath && CanFollowPath.initialBool)
		{
			CanFollowPath.initialBool = false;
			path.CheckIfCanFollowPath();
		}
		
		if (!FollowPath)
		{
			path.StopFollowPath();
			CanFollowPath.initialBool = true;
		}

		if (path.CannotReachPlayer) //Runs once
		{
			path.CannotReachPlayer = false;
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
		
	void OutOfRange()
	{
		Debug.Log("Pet OutOfRange");
		transform.position = PlayerTransform.initialPos;
		currentState = PetStatev2.Idle;
		ResetAttack();
	}
	
	void ResetAttack()
	{
		JumpMomentum = 0;
		JumpMomentumScale = 0;
		currentState = PetStatev2.Idle;
	}
	
	//When a path can't be created to the enemy...
	private IEnumerator CantReachEnemyDelay()
	{
		CanReachEnemy = false;
		currentState = PetStatev2.Wait;
		Debug.Log("Entered wait state");
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
		Debug.Log("Attacking anyways");
		AttackCoroutine = StartCoroutine(AttackEnemy());
	}
	
	private IEnumerator AttackEnemy()
	{
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
}

//TO DO:
//Test putting the pet into a followpath state.

//Reverse the enemy direction when it collides with the player or pet.
