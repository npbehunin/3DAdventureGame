using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bat : Enemy {

	//Bat will fly towards the player and "bounce" off. Could also slow down the movement.
	//Bat will fly towards two random points, then target the player again
	
	protected override void Start () 
	{
		base.Start();
		currentState = EnemyState.Idle;
		rb = GetComponent<Rigidbody2D>();
		target = GameObject.FindWithTag("Player").transform;
		Health = 2;
		Damage = 1;
		MoveSpeed = 3;
		chaseRadius = 4;
		attackRadius = 0;
	}
	
	// Update is called once per frame
	//protected override void Update () 
	//{
		
	//}

	protected override void Update()
	{
		base.Update();
		Debug.Log(position);
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
			RandomPosition();
		}
	}

	//Check big radius around player, make sure the number is smaller than it.
	//Check radius around bat, make sure it doesn't go bigger than this too.
	
	void RandomPosition()
	{
		float playerrange = Vector3.Distance(target.position, target.position * 2);
		float batrange = Vector3.Distance(transform.position, transform.position * 2);
		//Vector3 randompos1 = 

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
		yield return new WaitForSeconds(.5f);
		currentState = EnemyState.Random;
	}
}
