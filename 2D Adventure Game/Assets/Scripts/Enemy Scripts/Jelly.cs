using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Jelly : Enemy
{
	private Rigidbody2D rb;

	public Transform target;
	public Transform home;

	public float chaseRadius;
	public float attackRadius;

	void Start ()
	{
		currentState = EnemyState.Idle;
		rb = GetComponent<Rigidbody2D>();
		target = GameObject.FindWithTag("Player").transform;
		Health = 3;
		Damage = 1;
	}
	
	void FixedUpdate () 
	{
		CheckDistance();
	}

	void CheckDistance()
	{
		if (Vector3.Distance(target.position, transform.position) <= chaseRadius 
		    && Vector3.Distance(target.position, transform.position) > attackRadius)
		{
			if (currentState == EnemyState.Idle || currentState == EnemyState.Walk || currentState == EnemyState.Target
			    || currentState != EnemyState.Knocked)
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
