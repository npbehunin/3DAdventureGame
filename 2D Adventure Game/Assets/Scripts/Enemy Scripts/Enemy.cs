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
	public Vector3Value CamShakeDir;
	public int Health, Damage;
	public IntValue WeaponDamage;
	public bool CanCollide;
	public Rigidbody2D rb;
	public Transform target, home;

	public EnemyState currentState;
	protected EnemyState laststate;
	protected Vector3 position, JumpPosition, colPos;
	protected bool CanAttack;
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
	}

	protected virtual void FixedUpdate()
	{
		CheckDistance();
	}
	
	protected virtual void Update () 
	{
		if (Health <= 0 && !Hitstun.HitStunEnabled)
		{
			gameObject.SetActive(false);
		}
		CheckForPause();

		//if (currentState != EnemyState.Knocked && currentState != EnemyState.Paused) //Just a backup
		//{
		//	CanCollide = true;
		//}
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
		if (CanCollide)
		{
			if (col.gameObject.CompareTag("WeaponHitbox")) //Change to sword hitbox? Would need a seperate "CanCollide" for each weapon type it checks for.
			{
				CanCollide = false;
				RunEventKnocked(col.transform.position);
			}

			if (col.gameObject.CompareTag("Player"))
			{
				CollisionEvent();
			}
		}
	}
	
	protected void OnTriggerExit2D(Collider2D col) //If the sword swings too fast, the sword hitbox might never disappear. Be sure to adjust the sword hitbox in its anim.
	{
		if (col.gameObject.CompareTag("WeaponHitbox"))
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
		colPos = pos;
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
			if (Vector3.Distance(target.position, transform.position) <= chaseRadius
			    && Vector3.Distance(target.position, transform.position) > attackRadius)
			{
				InRadiusEvent();
			}

			if (Vector3.Distance(target.position, transform.position) <= attackRadius)
			{
				AttackEvent();
			}
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

//TO DO
//The reason why the enemy was registering multiple collisions from ontriggerenter was because THE PLAYER WAS ENTERING
//THE STATIC STATE. Entering the static state disabled the player's hitboxes (sword hitbox), and caused the sword
//hitbox to blink in and out when static was enabled.
