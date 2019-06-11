using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Jelly : Enemy
{
	private float JumpMomentum, JumpMomentumPower, JumpMomentumScale, JumpMomentumSmooth;
	private bool CanSetDifference;
	private Vector3 difference;//playerDirection;
	private Coroutine knockCo;
	
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
		CanAttack = true;
	}

	protected override void Update()
	{
		base.Update();
		if (!CanAttack)
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
				//playerDirection = PlayerMovement.test;
			}

			float smooth = 4f;
			float power = 2.5f;
			knockMomentumScale += smooth * Time.deltaTime;
			knockMomentum = Mathf.Lerp(power, 0, knockMomentumScale);
			position = (knockMomentum * difference);
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
		if (!CanAttack && currentState == EnemyState.Attack)
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
	protected override void InRadiusEvent()
	{
		base.InRadiusEvent();
		ChangeState(EnemyState.Target);
		position = Vector3.MoveTowards(transform.position, target.position, MoveSpeed * Time.deltaTime);
		rb.MovePosition(position);
	}

	//Attack event
	protected override void AttackEvent()
	{
		base.AttackEvent();
		if (CanAttack)
		{
			ChangeState(EnemyState.Attack);
			JumpCoroutine = StartCoroutine(JumpAtTarget());
		}
		else
		{
			ChangeState(EnemyState.Target);
		}
	}
	
	//Knocked event
	protected override void KnockedEvent()
	{
		base.KnockedEvent();
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
		yield return CustomTimer.Timer(.25f);
		currentState = EnemyState.Idle;
	}

	void ResetJump()
	{
		JumpMomentum = 0;
		JumpMomentumScale = 0;
		CanAttack = true;
	}
	
	private IEnumerator JumpAtTarget()
	{
		//Animation for winding up attack
		JumpPosition = (transform.position - target.position) * -1;
		yield return CustomTimer.Timer(.5f);
		CanAttack = false;
		yield return CustomTimer.Timer(.5f);
		currentState = EnemyState.Idle;
		yield return CustomTimer.Timer(1f);
		ResetJump();
	}
}

//TO DO
//During the first half of the prep jump, enemy can be knocked away.
//During the second half, the enemy won't be knocked and will continue its jump.

//The jelly knocked state should end when the sword swing ends.
//Jelly can be hit even if knocked. Set CanCollide to true a bit before it exits the knocked state.
