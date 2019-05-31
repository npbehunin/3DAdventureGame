using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Jelly : Enemy
{
	public float JumpMomentum, JumpMomentumPower, JumpMomentumScale, JumpMomentumSmooth;
	public bool CanSetDifference;

	public Vector3 difference, playerDirection;

	public Coroutine knockCo;
	
	protected override void StartValues()
	{
		base.StartValues();
		currentState = EnemyState.Idle;
		rb = GetComponent<Rigidbody2D>();
		target = GameObject.FindWithTag("Player").transform; //Fix
		Health = 45;
		Damage = 1;
		JumpMomentumSmooth = 4f;
		JumpMomentumPower = 4f;
		MoveSpeed = 1.75f;
		chaseRadius = 3.5f;
		attackRadius = 1;
		CanSetDifference = false;
	}

	protected override void Update()
	{
		base.Update();
		if (Attacking)
		{
			//Run this through a while loop to work better with pause system
			JumpMomentumScale += JumpMomentumSmooth * Time.deltaTime;
			JumpMomentum = Mathf.Lerp(JumpMomentumPower, 0, JumpMomentumScale);
			position = (JumpMomentum * JumpPosition);
		}

		if (currentState == EnemyState.Knocked)
		{
			if (CanSetDifference)
			{
				CanSetDifference = false;
				difference = (transform.position - target.position).normalized;
				playerDirection = PlayerMovement.test;
			}

			float smooth = 4f;
			float power = 2.5f;
			knockMomentumScale += smooth * Time.deltaTime;
			knockMomentum = Mathf.Lerp(power, 0, knockMomentumScale);
			position = (knockMomentum * difference);
			
			//position = (knockMomentum * new Vector3(difference.x * playerDirection.x, difference.y * playerDirection.y, 0));
			//Use this instead for knockback depending on player's position. HOWEVER, knockback from projectiles won't work if the player is standing still.
		}
		else
		{
			knockMomentum = 0;
			knockMomentumScale = 0;
		}
	}

	protected override void FixedUpdate()
	{
		base.FixedUpdate();
		if (Attacking && currentState == EnemyState.Attack)
		{
			rb.MovePosition(transform.position + position * MoveSpeed * Time.deltaTime);
		}

		if (currentState == EnemyState.Knocked)
		{
			if (rb.bodyType != RigidbodyType2D.Static)
			{
				rb.MovePosition(transform.position + position * MoveSpeed * Time.deltaTime);
			}
		}
	}
	
	//Follow event
	public void FollowPlayer()
	{
		ChangeState(EnemyState.Target);
		position = Vector3.MoveTowards(transform.position, target.position, MoveSpeed * Time.deltaTime);
		rb.MovePosition(position);
	}

	//Attack event
	public void JumpAttackCoroutine()
	{
		ChangeState(EnemyState.Attack);
		if (!Attacking)
		{
			JumpCoroutine = StartCoroutine(JumpAtTarget());
		}
	}
	
	//Knocked event
	public void KnockEvent()
	{
		StartCoroutine(Knocked());
		if (knockCo != null)
		{
			StopCoroutine(knockCo);
		}
		if (JumpCoroutine != null)
		{
			StopCoroutine(JumpCoroutine);
			ResetJump();
		}
	}
	
	//Knocked coroutine
	private IEnumerator Knocked()
	{
		currentState = EnemyState.Paused;
		yield return Hitstun.StartHitstun();
		CanSetDifference = true;
		knockCo = StartCoroutine(SetKnockedState());
	}
	
	private IEnumerator SetKnockedState()
	{
		currentState = EnemyState.Knocked;
		yield return CustomTimer.Timer(.5f);
		currentState = EnemyState.Idle;
	}

	void ResetJump()
	{
		JumpMomentum = 0;
		JumpMomentumScale = 0;
		Attacking = false;
	}
	
	private IEnumerator JumpAtTarget()
	{
		//Animation for winding up attack
		JumpPosition = (transform.position - target.position) * -1;
		yield return CustomTimer.Timer(.5f);
		Attacking = true;
		yield return CustomTimer.Timer(.5f);
		currentState = EnemyState.Idle;
		yield return CustomTimer.Timer(1f);
		ResetJump();
	}
}
