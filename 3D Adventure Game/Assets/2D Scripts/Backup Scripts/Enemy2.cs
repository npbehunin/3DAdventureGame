using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy2 : MonoBehaviour
{
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
		private float horizontalspeed, verticalspeed;
		public float JumpMomentum, JumpMomentumPower, JumpMomentumScale, JumpMomentumSmooth;

		public EquipWeapon WeaponEquipped;

		public Vector3 position, JumpPosition;

		public Rigidbody2D rb;

		public Transform target, home;

		public Coroutine JumpCoroutine;

		protected virtual void Start()
		{

		}

		void FixedUpdate()
		{
			if (Attacking)
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
		void Update()
		{
			if (Health <= 0)
			{
				gameObject.SetActive(false);
			}

			if (Attacking)
			{
				//float JumpMomentum = 0, JumpMomentumScale = 0, JumpMomentumSmooth = 4, JumpMomentumPower = 1;
				JumpMomentumScale += JumpMomentumSmooth * Time.deltaTime;
				JumpMomentum = Mathf.Lerp(JumpMomentumPower, 0, JumpMomentumScale);
				position = (JumpMomentum * JumpPosition);
				//Debug.Log(JumpMomentumScale);
				//Debug.Log(JumpMomentumScale);

				//WHY ISN'T THIS FORMULA WORKING THE EXACT SAME AS THE PLAYER AAAA
			}

			if (currentState == EnemyState.Paused)
			{
				StopCoroutine(JumpCoroutine);
			}
		}

		void OnTriggerEnter2D(Collider2D col)
		{
			if (col.gameObject.CompareTag("WeaponHitbox"))
			{
				//Damage = WeaponEquipped.WeaponDamage;
				TakeDamage();
			}
		}

		void TakeDamage()
		{
			Health -= Damage;
		}

		protected void CheckDistance()
		{
			if (Vector3.Distance(target.position, transform.position) <= chaseRadius
			    && Vector3.Distance(target.position, transform.position) > attackRadius)
			{
				if (currentState != EnemyState.Knocked && currentState != EnemyState.Attack &&
				    currentState != EnemyState.Paused)
				{
					position = Vector3.MoveTowards(transform.position, target.position, MoveSpeed * Time.deltaTime);
					rb.MovePosition(position);
					ChangeState(EnemyState.Target);
				}
			}

			if (Vector3.Distance(target.position, transform.position) <= attackRadius)
			{
				ChangeState(EnemyState.Attack);
				JumpCoroutine = StartCoroutine(JumpAtTarget());
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
			//Animation for winding up attack
			JumpPosition = (transform.position - target.position) * -1;
			yield return new WaitForSeconds(.5f);
			Attacking = true;
			yield return new WaitForSeconds(.5f);
			currentState = EnemyState.Idle;
			yield return new WaitForSeconds(1);
			JumpMomentum = 0;
			JumpMomentumScale = 0;
			Attacking = false;
		}
	}
}
