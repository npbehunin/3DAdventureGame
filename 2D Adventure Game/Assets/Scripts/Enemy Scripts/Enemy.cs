using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Events;

public enum EnemyState
{
	Idle, Walk, Target, Attack, Knocked, Random, Delay, Dead
}

public class Enemy : MonoBehaviour
{
	public EnemyState currentState;

	public int Health, Damage;
	
	public bool Attacking;

	public float MoveSpeed, chaseRadius, attackRadius;
	private float horizontalspeed,verticalspeed;

	public EquipWeapon WeaponEquipped;

	public Vector3 position, JumpPosition;
	
	public Rigidbody2D rb;

	public Transform target, home;

	public Coroutine JumpCoroutine;

	public Knockback knockback;

	public UnityEvent EventInRadius, EventAttack, EventPlayerCollision;
	
	protected virtual void Start ()
	{
		StartValues();
	}

	protected virtual void StartValues()
	{
		knockback = gameObject.GetComponent<Knockback>();
	}

	protected virtual void FixedUpdate()
	{
		CheckDistance();
	}
	
	protected virtual void Update () 
	{
		if (Health <= 0)
		{
			gameObject.SetActive(false);
		}
	}

	//Check triggers
	protected void OnTriggerEnter2D(Collider2D col)
	{
		if (col.gameObject.CompareTag("WeaponHitbox"))
		{
			Damage = WeaponEquipped.WeaponDamage;
			TakeDamage();
			if (knockback != null)
			{
				knockback.Knocked(rb, col.transform.position);
			}
		}

		if (col.gameObject.CompareTag("Player"))
		{
			if (EventPlayerCollision != null)
			{
				EventPlayerCollision.Invoke();
			}
		}
	}

	void TakeDamage()
	{
		Health -= Damage;
	}
	
	protected void CheckDistance()
	{
		if (currentState != EnemyState.Knocked && currentState != EnemyState.Attack &&
		     currentState != EnemyState.Delay && currentState != EnemyState.Random)
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
