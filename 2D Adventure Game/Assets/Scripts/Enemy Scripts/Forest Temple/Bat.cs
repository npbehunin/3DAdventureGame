using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions.Comparers;
using Random = UnityEngine.Random;

public class Bat : Enemy {

	//Bat will fly towards the player and "bounce" off. Could also slow down the movement.
	//Bat will fly towards two random points, then target the player again

	public bool CanSetRandomPos;
	public Vector2 RandomPos;
	public int FlyPhase;
	public float lerpchange;
	public Vector2 dir;
	public Vector2 lastdir;

	private float journeyLength;

	protected override void StartValues()
	{
		base.StartValues();
		currentState = EnemyState.Idle;
		rb = GetComponent<Rigidbody2D>();
		//target = GameObject.FindWithTag("Player").transform;
		Health = 50000;
		//Damage = 1;
		MoveSpeed = 3;
		chaseRadius = 6;
		attackRadius = 0;
		CanSetRandomPos = true;
		FlyPhase = 1;
		lerpchange = 0;
	}

	protected override void FixedUpdate()
	{
		if (currentState != EnemyState.Paused)
		{
			base.FixedUpdate();
			if (currentState == EnemyState.Delay)
			{
				position = Vector3.MoveTowards(transform.position, target, -(MoveSpeed * Time.deltaTime));
				rb.MovePosition(position);
			}

			if (currentState == EnemyState.Random)
			{
				MoveTowardsRandomPos();
			}
		}
		
		//Prevents jittering if it somehow goes inside target. No damage.
		float BounceRadius = .5f;
		if (Vector3.Distance(transform.position, target) <= BounceRadius)
		{
			CollisionEvent(); //This is set to false in ResetAttack.
		}
	}
	
	void MoveTowardsRandomPos()
	{
		if (CanSetRandomPos)
		{
			CanSetRandomPos = false;
			lerpchange = 0;
			switch (FlyPhase)
			{
				case 1:
					SetRandomPosition(RandomPosition(4.5f));
					break;
				case 2:
					SetRandomPosition(RandomPosition(3.5f));
					break;
				default:
					FlyPhase = 0;
					currentState = EnemyState.Idle;
					break;
			}
		}
		if (transform.position.x > RandomPos.x + .1 || transform.position.x < RandomPos.x - .1 || 
		    transform.position.y > RandomPos.y + .1 || transform.position.y < RandomPos.y - .1)
		{
			dir = (RandomPos - (Vector2) transform.position).normalized;
			ChangeDirection(dir);
		}
		else
		{
			lastdir = dir;
			lerpchange = 0;
			CanSetRandomPos = true;
			FlyPhase += 1;
		}
	}

	//Lerp between last direction and new direction
	void ChangeDirection(Vector2 dir)
	{
		lerpchange += .05f;
		Vector2 dirchange = Vector2.Lerp(lastdir, dir, lerpchange);
		position = ((Vector2)transform.position + dirchange * MoveSpeed * Time.deltaTime);
		rb.MovePosition(position);
	}

	//Generates the random point
	static Vector2 RandomPosition(float radius)
	{
		float angle = Random.Range (0f, Mathf.PI * 2);
		float x = Mathf.Sin (angle) * radius;
		float y = Mathf.Cos (angle) * radius;

		return new Vector2 (x, y);
	}

	//Runs a check before setting the random point to RandomPos.
	void SetRandomPosition(Vector2 randompos)
	{
		if (randompos.x > 0)
		{
			if (transform.position.x < target.x)
			{
				randompos.x *= -1;
			}
		}

		if (randompos.x < 0)
		{
			if (transform.position.x > target.x)
			{
				randompos.x *= -1;
			}
		}
		
		if (randompos.y > 0)
		{
			if (transform.position.y < target.y)
			{
				randompos.y *= -1;
			}
		}

		if (randompos.y < 0)
		{
			if (transform.position.y > target.y)
			{
				randompos.y *= -1;
			}
		}
		randompos += new Vector2(target.x, target.y);
		RandomPos = randompos;
		Debug.DrawLine(transform.position, randompos, Color.yellow, 5f);
	}

	//In radius event
	protected override void InRadiusEvent()
	{
		if (currentState!=EnemyState.Delay)
		{
			//currentState = EnemyState.Target;
			Vector3 dir = (target - transform.position).normalized;
			ChangeDirection(dir);
		}
	}

	//Collision event
	public override void CollisionEvent()
	{
		currentState = EnemyState.Delay;
		StartCoroutine(TargetPlayerDelay());
	}
	
	protected override IEnumerator HitstunCo()
	{
		yield return base.HitstunCo();
		CollisionEvent();
		//New state goes here!
	}

	//Stop this coroutine if knocked.
	IEnumerator TargetPlayerDelay()
	{
		yield return CustomTimer.Timer(1f);
		currentState = EnemyState.Random;
		lastdir = -(target - transform.position).normalized;
		yield return null;
	}

	void OnEnable()
	{
		currentState = EnemyState.Idle;
		rb = GetComponent<Rigidbody2D>();
		//target = GameObject.FindWithTag("Player").transform;
		Health = 2;
		//Damage = 1;
		MoveSpeed = 3;
		chaseRadius = 6;
		attackRadius = 0;
		CanSetRandomPos = true;
		FlyPhase = 1;
	}
}

//TO DO:
//Add an attack state and keep it seperate from idle state.
//Add an attack hitbox anim.

//Known issues:
//Bat doesn't have an attack state or an attack hitbox to collide off the player or pet with yet.
