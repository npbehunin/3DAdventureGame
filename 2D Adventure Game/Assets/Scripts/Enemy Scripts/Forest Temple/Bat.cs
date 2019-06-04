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
		target = GameObject.FindWithTag("Player").transform;
		Health = 50000;
		Damage = 1;
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
				position = Vector3.MoveTowards(transform.position, target.position, -(MoveSpeed * Time.deltaTime));
				rb.MovePosition(position);
			}

			if (currentState == EnemyState.Random)
			{
				MoveTowardsRandomPos();
			}
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
			if (transform.position.x < target.position.x)
			{
				randompos.x *= -1;
			}
		}

		if (randompos.x < 0)
		{
			if (transform.position.x > target.position.x)
			{
				randompos.x *= -1;
			}
		}
		
		if (randompos.y > 0)
		{
			if (transform.position.y < target.position.y)
			{
				randompos.y *= -1;
			}
		}

		if (randompos.y < 0)
		{
			if (transform.position.y > target.position.y)
			{
				randompos.y *= -1;
			}
		}
		randompos += new Vector2(target.position.x, target.position.y);
		RandomPos = randompos;
		Debug.DrawLine(transform.position, randompos, Color.yellow, 5f);
	}

	//In radius event
	protected override void InRadiusEvent()
	{
		if (currentState!=EnemyState.Delay)
		{
			Vector3 dir = (target.position - transform.position).normalized;
			ChangeDirection(dir);
		}
	}

	//Collision event
	protected override void CollisionEvent()
	{
		currentState = EnemyState.Delay;
		StartCoroutine(TargetPlayerDelay());
	}

	//Stop this coroutine if knocked.
	IEnumerator TargetPlayerDelay()
	{
		yield return CustomTimer.Timer(1f);
		currentState = EnemyState.Random;
		lastdir = -(target.position - transform.position).normalized;
		yield return null;
	}

	void OnEnable()
	{
		currentState = EnemyState.Idle;
		rb = GetComponent<Rigidbody2D>();
		target = GameObject.FindWithTag("Player").transform;
		Health = 2;
		Damage = 1;
		MoveSpeed = 3;
		chaseRadius = 6;
		attackRadius = 0;
		CanSetRandomPos = true;
		FlyPhase = 1;
	}
}
