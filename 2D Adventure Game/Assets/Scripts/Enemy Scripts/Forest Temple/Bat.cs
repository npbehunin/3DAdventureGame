using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Bat : Enemy {

	//Bat will fly towards the player and "bounce" off. Could also slow down the movement.
	//Bat will fly towards two random points, then target the player again

	public bool CanSetRandomPos;
	public Vector2 RandomPos;
	public int FlyPhase;
	
	protected override void Start () 
	{
		base.Start();
		currentState = EnemyState.Idle;
		rb = GetComponent<Rigidbody2D>();
		target = GameObject.FindWithTag("Player").transform;
		Health = 2;
		Damage = 1;
		MoveSpeed = 3;
		chaseRadius = 6;
		attackRadius = 0;
		CanSetRandomPos = false;
		FlyPhase = 0;
	}
	
	// Update is called once per frame
	//protected override void Update () 
	//{
		
	//}

	protected override void Update()
	{
		base.Update();
		//Debug.Log(position);
	}

	protected override void FixedUpdate()
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
	//Ideas for movement pattern:
	//#1: Fly away from the player. After time passes create a position and check for random points inside its circle for x amount of times.
	//#2: Fly away from the player in the flipped x and y direction to keep going the same way it interacted. Fly back after reaching a certain distance.
	//#3: Fly away from the player. Generate a random point along a large first circle. Then generate another random point along the edge of a smaller circle, and
	//finally, fly towards the player again. <--- Try this first. Also try getting bat to continue flying in the same direction after the collision.
	
	//Remember to fix the first random position from being at 0,0!

	//Move towards RandomPos.
	void MoveTowardsRandomPos()
	{
		if (CanSetRandomPos)
		{
			CanSetRandomPos = false;
			switch (FlyPhase)
			{
				case 1:
					SetRandomPosition(RandomPosition(5f));
					break;
				case 2:
					SetRandomPosition(RandomPosition(3f));
					break;
				default:
					FlyPhase = 0;
					currentState = EnemyState.Idle;
					break;
			}
		}

		if (new Vector2(transform.position.x, transform.position.y) != RandomPos)
		{
			position = Vector3.MoveTowards(transform.position, RandomPos, MoveSpeed * Time.deltaTime);
			rb.MovePosition(position);
		}
		else
		{
			CanSetRandomPos = true;
			FlyPhase += 1;
		}
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

	public void FollowPlayer()
	{
		if (currentState!=EnemyState.Delay)
		{
			position = Vector3.MoveTowards(transform.position, target.position, MoveSpeed * Time.deltaTime);
			rb.MovePosition(position);
		}
	}

	public void PlayerCollision()
	{
		currentState = EnemyState.Delay;
		StartCoroutine(TargetPlayerDelay());
	}

	IEnumerator TargetPlayerDelay()
	{
		yield return new WaitForSeconds(1f);
		currentState = EnemyState.Random;
	}
}
