using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sentry : Enemy
{
	
	//IDEA
	//If the player is outside the range, sentry can "dodge" arrow attacks
	//Forces the player to be in danger to hit him.
	
	//OR
	//Adjust the radius so the enemy has a larger horizontal radius than vertical. Works better for screen size.
	
	public bool CanThrow;
	public GameObject projectileType;
	//public int Mask;

	protected override void StartValues()
	{
		base.StartValues();
		currentState = EnemyState.Idle;
		rb = GetComponent<Rigidbody2D>();
		//target = GameObject.FindWithTag("Player").transform;
		Health = 3;
		//Damage = 1;
		MoveSpeed = 0;
		chaseRadius = 5;
		attackRadius = 5;
		CanThrow = true;
	}
	
	protected override void Update()
	{
		base.Update();	
		//Debug.DrawLine(transform.position, target.position, Color.red);
	}

	protected override void InRadiusEvent()
	{
		ChangeState(EnemyState.Attack);
		int Mask = 1 << 9;
		if (CanThrow)
		{
			if (Physics2D.Linecast(transform.position, target, Mask))
			{
				currentState = EnemyState.Idle;
			}
			else
			{
				CanThrow = false;
				StartCoroutine(ThrowRock());
			}
		}
		else
		{
			currentState = EnemyState.Idle;
		}
	}

	IEnumerator ThrowRock()
	{
		yield return CustomTimer.Timer(.5f);
		projectileType.GetComponent<Projectile>().target = target;
		GameObject rock = Instantiate(projectileType, gameObject.transform.position, Quaternion.identity);
		yield return CustomTimer.Timer(2f);
		currentState = EnemyState.Idle;
		CanThrow = true;
	}
}
