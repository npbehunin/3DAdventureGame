using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
public enum EnemyState
{
	Idle, Walk, Target, Attack, Knocked, Dead
}
public class Enemy : MonoBehaviour
{
	public EnemyState currentState;

	public int Health;
	public int Damage;

	public float MoveSpeed;
	private float horizontalspeed;
	private float verticalspeed;

	public EquipWeapon WeaponEquipped;
	
	public Rigidbody2D rb;

	public Transform target;
	public Transform home;

	public float chaseRadius;
	public float attackRadius;
	
	void Start () 
	{
		
	}
	
	// Update is called once per frame
	void Update () 
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
		if (Vector3.Distance(target.position, transform.position) <= chaseRadius 
		    && Vector3.Distance(target.position, transform.position) > attackRadius)
		{
			if (currentState != EnemyState.Knocked)
			{
				Vector3 temp = Vector3.MoveTowards(transform.position, target.position, MoveSpeed * Time.deltaTime);
				rb.MovePosition(temp);
				ChangeState(EnemyState.Target);
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

//To get the damage
