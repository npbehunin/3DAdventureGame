using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Events;

public enum EnemyState
{
	Idle, Walk, Target, Attack, Knocked, Random, Delay, Dead, Paused, Hitstun
}

public class Enemy : MonoBehaviour
{
	public Signal CamShakeSignal;
	public Vector3Value CamShakeDir, PlayerTransform, PetTransform;
	public int Health, Damage;
	public IntValue WeaponDamage;
	public bool CanCollide, EnemyLOS, IsTarget, PlayerIsTarget;
	public BoolValue PetIsAttacking, EnemyIsInRadius;
	public Rigidbody2D rb;
	//public Transform , pet;

	public EnemyState currentState;
	protected EnemyState laststate;
	protected Vector3 position, JumpPosition, target, knockDirection;
	public bool CanAttack;
	protected Coroutine JumpCoroutine, KnockedCoroutine;
	protected float MoveSpeed, chaseRadius, attackRadius, knockMomentumScale, knockMomentum;
	
	private bool CanSetState;
	//private float horizontalspeed,verticalspeed;
	
	protected virtual void Start ()
	{
		StartValues();
	}

	protected virtual void StartValues()
	{
		CanSetState = true;
		CanCollide = true;
		IsTarget = false;
	}

	protected virtual void FixedUpdate()
	{
		CheckDistance();
	}
	
	protected virtual void Update () 
	{
		if (PlayerIsTarget)
		{
			target = PlayerTransform.initialPos;
		}
		else
		{
			target = PetTransform.initialPos;
		}
		
		if (Health <= 0 && !Hitstun.HitStunEnabled)
		{
			gameObject.SetActive(false);
		}
		CheckForPause();

		//*Fix obviously
		//Check if this enemy is in LOS (of player).
		float chaseRadiusX = 8f;
		float chaseRadiusY = 4.5f;
		int wallLayerMask = 1 << 9;
		if (Math.Abs(PlayerTransform.initialPos.x - transform.position.x) <= chaseRadiusX && 
		    Math.Abs(PlayerTransform.initialPos.y - transform.position.y) <= chaseRadiusY)
		{
			if (Physics2D.Linecast(PlayerTransform.initialPos, transform.position, wallLayerMask))//, 15, wallLayerMask))
			{
				EnemyLOS = false;
			}
			else
			{
				Debug.DrawLine(PlayerTransform.initialPos, transform.position, Color.yellow);
				EnemyLOS = true;
			}
		}
		else
		{
			EnemyLOS = false;
		}
	}

	//Checks if game is paused
	protected void CheckForPause()
	{
		if (PauseGame.IsPaused)
		{
			if (CanSetState)
			{
				CanSetState = false;
				laststate = currentState;
			}
			currentState = EnemyState.Paused;
		}
		else
		{
			if (!CanSetState)
			{
				CanSetState = true;
				currentState = laststate;
			}
		}

		//For now, no other enemies besides the jelly script get hitstunned
		
		if (currentState == EnemyState.Hitstun)
		{
			position = Vector3.zero;
		}
		
		if (currentState == EnemyState.Paused)
		{
			rb.bodyType = RigidbodyType2D.Static;
		}
		else
		{
			rb.bodyType = RigidbodyType2D.Dynamic;
		}
	}

	//Check triggers
	protected void OnTriggerEnter2D(Collider2D col)
	{
		knockDirection = (transform.position - col.transform.position).normalized;
		if (CanCollide)
		{
			if (col.gameObject.CompareTag("WeaponHitbox") || col.gameObject.CompareTag("PetAttackHitbox")) //Change to sword hitbox? Would need a seperate "CanCollide" for each weapon type it checks for.
			{
				Debug.Log("Collision");
				CanCollide = false;
				RunEventKnocked(col.transform.position);
			}

			if (col.gameObject.CompareTag("Pet") && IsTarget && PetIsAttacking.initialBool)
			{
				Debug.Log("Collision directly");
				CollisionEvent();
			}
			
			if (col.gameObject.CompareTag("Player"))
			{
				CollisionEvent();
			}
		}
	}
	
	protected void OnTriggerExit2D(Collider2D col) //If the sword swings too fast, the sword hitbox might never disappear. Be sure to adjust the sword hitbox in its anim.
	{
		if (col.gameObject.CompareTag("WeaponHitbox") || col.gameObject.CompareTag("PetAttackHitbox"))
		{
			CanCollide = true;
		}
	}
	
	//Knocked stuff
	public void RunEventKnocked(Vector3 pos)
	{
		if (KnockedCoroutine != null)
		{
			StopCoroutine(KnockedCoroutine);
		}
		//KnockedCoroutine = StartCoroutine(Invincibility());	
		TakeDamage();
		CamShakeSignal.Raise();
		CamShakeDir.initialPos = transform.position - pos;
		KnockedEvent();
		//colPos = pos;
	}

	//Take damage
	void TakeDamage()
	{
		Health -= WeaponDamage.initialValue;
	}
	
	protected void CheckDistance()
	{
		if (currentState == EnemyState.Idle || currentState == EnemyState.Target)
		{
			if (Vector3.Distance(target, transform.position) <= chaseRadius
			    && Vector3.Distance(target, transform.position) > attackRadius)
			{
				InRadiusEvent();
			}

			if (Vector3.Distance(target, transform.position) <= attackRadius)
			{
				AttackEvent();
			}
		}

		float detectionRadius = 1.5f;
		if (IsTarget && PetIsAttacking.initialBool)
		{
			//Seperating these 'if' statements prevents this from always running
			if (Vector3.Distance(transform.position, PetTransform.initialPos) <= detectionRadius)
			{
				PlayerIsTarget = false;
			}
		}
		else
		{
			PlayerIsTarget = true;
		}

		if (!PetIsAttacking.initialBool)
		{
			IsTarget = false; //Keep this in mind in case enemies have trouble moving correctly
			target = PlayerTransform.initialPos;
		}
	}
	
	public void ChangeState(EnemyState newState)
	{
		if (currentState != newState)
		{
			currentState = newState;
		}
	}

	//private IEnumerator Invincibility()
	//{
	//	CanCollide = false;
	//	yield return CustomTimer.Timer(.15f);
	//	CanCollide = true;
	//}

	protected virtual void InRadiusEvent()
	{
		//Do something
	}
	protected virtual void AttackEvent()
	{
		//Do something
	}
	protected virtual void CollisionEvent()
	{
		//Do something
	}
	protected virtual void KnockedEvent()
	{
		//Do something
	}
}

//TO DO:

//Notes:
//The reason why the enemy was registering multiple collisions from ontriggerenter was because THE PLAYER WAS ENTERING
//THE STATIC STATE. Entering the static state disabled the player's hitboxes (sword hitbox), and caused the sword
//hitbox to blink in and out when static was enabled.
