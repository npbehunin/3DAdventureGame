using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
public enum EnemyState
{
	Idle, Walk, Target, Attack, Knocked, Paused, Dead
}
public class Enemy : MonoBehaviour
{
	public EnemyState currentState;

	public int Health, Damage;
	
	public bool Attacking;

	public float MoveSpeed, chaseRadius, attackRadius;
	private float horizontalspeed,verticalspeed;
	public float JumpMomentum, JumpMomentumPower, JumpMomentumScale, JumpMomentumSmooth;

	public EquipWeapon WeaponEquipped;

	public Vector3 position, JumpPosition;
	
	public Rigidbody2D rb;

	public Transform target, home;

	public Coroutine JumpCoroutine;
	
	protected virtual void Start ()
	{
		
	}

	void FixedUpdate()
	{
		if (Attacking && currentState == EnemyState.Attack)
		{
			//Debug.Log(JumpPosition);
			rb.MovePosition(transform.position + position * MoveSpeed * Time.deltaTime);
		}
		else
		{
			CheckDistance();
		}
	}
	
	// Update is called once per frame
	void Update () 
	{
		if (Health <= 0)
		{
			gameObject.SetActive(false);
		}

		if (Attacking)
		{
			JumpMomentumScale += JumpMomentumSmooth * Time.deltaTime;
			JumpMomentum = Mathf.Lerp(JumpMomentumPower, 0, JumpMomentumScale);
			position = (JumpMomentum * JumpPosition);
		}

		if (currentState == EnemyState.Paused)
		{
			if (JumpCoroutine != null)
			{
				StopCoroutine(JumpCoroutine);
				JumpMomentum = 0;
				JumpMomentumScale = 0;
				Attacking = false;
			}
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
				position = Vector3.MoveTowards(transform.position, target.position, MoveSpeed * Time.deltaTime);
				rb.MovePosition(position);
				ChangeState(EnemyState.Target);
			}

			if (Vector3.Distance(target.position, transform.position) <= attackRadius)
			{
				if (!Attacking)
				{
					ChangeState(EnemyState.Attack);
					JumpCoroutine = StartCoroutine(JumpAtTarget());
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
	
	private IEnumerator JumpAtTarget()
	{
		//Debug.Log("Starting jump at target");
		//Animation for winding up attack
		JumpPosition = (transform.position - target.position) * -1;
		yield return new WaitForSeconds(.5f);
		Attacking = true;
		//Debug.Log("YEET");
		yield return new WaitForSeconds(.5f);
		currentState = EnemyState.Idle;
		yield return new WaitForSeconds(1.5f);
		JumpMomentum = 0;
		JumpMomentumScale = 0;
		Attacking = false;
	}
}
