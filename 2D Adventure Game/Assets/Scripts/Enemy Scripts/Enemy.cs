using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Events;

public enum EnemyState
{
	Idle, Walk, Target, Attack, Knocked, Paused, Dead
}
public class Enemy : MonoBehaviour
{
	public EnemyState currentState;

	public int Health, Damage;
	
	public bool Attacking, CanAttack;

	public float MoveSpeed, chaseRadius, attackRadius;
	private float horizontalspeed,verticalspeed;
	public float JumpMomentum, JumpMomentumPower, JumpMomentumScale, JumpMomentumSmooth;

	public EquipWeapon WeaponEquipped;

	public Vector3 position, JumpPosition;
	
	public Rigidbody2D rb;

	public Transform target, home;

	public Coroutine JumpCoroutine;

	public UnityEvent EventInRadius, EventAttack;
	
	protected virtual void Start ()
	{
		
	}

	protected virtual void FixedUpdate()
	{
		CheckDistance();
	}
	
	// Update is called once per frame
	protected virtual void Update () 
	{
		if (Health <= 0)
		{
			gameObject.SetActive(false);
		}
	}

	void OnTriggerEnter2D(Collider2D col)
	{
		if (col.gameObject.CompareTag("WeaponHitbox"))
		{
			Damage = WeaponEquipped.WeaponDamage;
			TakeDamage();
		}
	}

	void TakeDamage()
	{
		Health -= Damage;
	}
	
	protected void CheckDistance()
	{
		if (currentState != EnemyState.Knocked && currentState != EnemyState.Attack &&
		    currentState != EnemyState.Paused)
		{
			if (Vector3.Distance(target.position, transform.position) <= chaseRadius
			    && Vector3.Distance(target.position, transform.position) > attackRadius)
			{
				//Follow player
				EventInRadius.Invoke();
				ChangeState(EnemyState.Target);
			}

			if (Vector3.Distance(target.position, transform.position) <= attackRadius)
			{
				if (!Attacking)
				{
					//Attack player
					EventAttack.Invoke();
					ChangeState(EnemyState.Attack);
				}
			}
		}
	}

	private void ChangeState(EnemyState newState)
	{
		if (currentState != newState)
		{
			currentState = newState;
		}
	}
}
