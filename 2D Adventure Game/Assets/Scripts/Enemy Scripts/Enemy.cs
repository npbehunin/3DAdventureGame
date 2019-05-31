using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Events;

public enum EnemyState
{
	Idle, Walk, Target, Attack, Knocked, Random, Delay, Dead, Paused
}

public class Enemy : MonoBehaviour
{
	public EnemyState currentState, laststate;

	public int Health, Damage;

	public bool Attacking, CanSetState;
	public static bool CanCollide;

	public float MoveSpeed, chaseRadius, attackRadius;
	public float knockMomentumScale, knockMomentum;
	private float horizontalspeed,verticalspeed;
	
	public Vector3 position, JumpPosition, colPos;

	public EquipWeapon WeaponEquipped;
	public Rigidbody2D rb;
	public Transform target, home;
	public Coroutine JumpCoroutine;
	public Knockback knockback;
	public UnityEvent EventInRadius, EventAttack, EventPlayerCollision, EventKnocked;
	
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
	protected void OnTriggerStay2D(Collider2D col)
	{
		if (CanCollide)
		{
			if (col.gameObject.CompareTag("WeaponHitbox"))
			{
				CanCollide = false;
				RunEventKnocked(col.transform.position);
			}

			if (col.gameObject.CompareTag("Player"))
			{
				if (EventPlayerCollision != null)
				{
					EventPlayerCollision.Invoke();
				}
			}
		}
	}
	
	//Knocked stuff
	public void RunEventKnocked(Vector3 pos)
	{
		Damage = WeaponEquipped.WeaponDamage;
		TakeDamage();
		if (EventKnocked != null)
		{
			EventKnocked.Invoke();
		}

		colPos = pos;
	}

	//Take damage
	void TakeDamage()
	{
		Health -= Damage;
	}
	
	protected void CheckDistance()
	{
		if (currentState != EnemyState.Knocked && currentState != EnemyState.Attack &&
		     currentState != EnemyState.Delay && currentState != EnemyState.Random && currentState != EnemyState.Paused)
		{
			if (Vector3.Distance(target.position, transform.position) <= chaseRadius
			    && Vector3.Distance(target.position, transform.position) > attackRadius)
			{
				//Follow player
				if (EventInRadius != null)
				{
					
					EventInRadius.Invoke(); //Will run constantly
				}
			}

			if (Vector3.Distance(target.position, transform.position) <= attackRadius)
			{
				if (!Attacking && EventAttack!=null)
				{
					//Attack player
					EventAttack.Invoke(); //Runs once
				}
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
}
