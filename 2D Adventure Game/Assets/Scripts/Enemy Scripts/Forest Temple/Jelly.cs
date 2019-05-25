using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Jelly : Enemy
{
	public float JumpMomentum, JumpMomentumPower, JumpMomentumScale, JumpMomentumSmooth;
	
	protected override void StartValues()
	{
		base.StartValues();
		currentState = EnemyState.Idle;
		rb = GetComponent<Rigidbody2D>();
		target = GameObject.FindWithTag("Player").transform;
		Health = 10;
		Damage = 1;
		JumpMomentumSmooth = 4f;
		JumpMomentumPower = 4f;
		MoveSpeed = 1.75f;
		chaseRadius = 3.5f;
		attackRadius = 1;
	}

	protected override void Update()
	{
		base.Update();
		if (Attacking)
		{
			JumpMomentumScale += JumpMomentumSmooth * Time.deltaTime;
			JumpMomentum = Mathf.Lerp(JumpMomentumPower, 0, JumpMomentumScale);
			position = (JumpMomentum * JumpPosition);
		}

		//if (currentState == EnemyState.Paused)
		//{
			//if (JumpCoroutine != null)
			//{
				//StopCoroutine(JumpCoroutine);
				//JumpMomentum = 0;
				//JumpMomentumScale = 0;
				//Attacking = false;
			//}
		//}
	}

	protected override void FixedUpdate()
	{
		base.FixedUpdate();
		if (Attacking && currentState == EnemyState.Attack)
		{
			//Debug.Log(JumpPosition);
			rb.MovePosition(transform.position + position * MoveSpeed * Time.deltaTime);
		}
	}
	
	public void FollowPlayer()
	{
		ChangeState(EnemyState.Target);
		position = Vector3.MoveTowards(transform.position, target.position, MoveSpeed * Time.deltaTime);
		rb.MovePosition(position);
	}

	public void JumpAttackCoroutine()
	{
		ChangeState(EnemyState.Attack);
		if (!Attacking)
		{
			JumpCoroutine = StartCoroutine(JumpAtTarget());
		}
	}
	
	private IEnumerator JumpAtTarget()
	{
		//Debug.Log("Starting jump at target");
		//Animation for winding up attack
		JumpPosition = (transform.position - target.position) * -1;
		yield return new WaitForSeconds(.5f);
		Attacking = true;
		//Debug.Log("YEET");
		yield return new WaitForSeconds(.5f);
		currentState = EnemyState.Idle;
		yield return new WaitForSeconds(1f);
		JumpMomentum = 0;
		JumpMomentumScale = 0;
		Attacking = false;
	}
}
