using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Jelly : Enemy
{
	private float JumpMomentum, JumpMomentumPower, JumpMomentumScale, JumpMomentumSmooth;
	private Vector3 difference;//playerDirection;
	private Coroutine knockCo;

	public JellyAnimation JellyAnim;

	public bool CollisionBounce;
	
	protected override void StartValues()
	{
		base.StartValues();
		currentState = EnemyState.Idle;
		rb = GetComponent<Rigidbody2D>();
		Health = 10;
		//Damage = 1;
		JumpMomentumSmooth = 4f;
		JumpMomentumPower = 4f;
		MoveSpeed = 1.75f;
		chaseRadius = 3.5f;
		attackRadius = 1;
		CanAttack = true;
	}

	protected override void Update()
	{
		base.Update();
		if (!CanAttack)
		{
			JumpMomentumScale += JumpMomentumSmooth * Time.deltaTime;
			JumpMomentum = Mathf.Lerp(JumpMomentumPower, 0, JumpMomentumScale);
			
			//Bounces back if it "collides" with the player.
			if (CollisionBounce)
			{
				position = (JumpMomentum * -JumpPosition);
			}
			else
			{
		position = (JumpMomentum * JumpPosition);
			}
		}

		if (currentState == EnemyState.Knocked)
		{
			float smooth = 4f;
			float power = 2.5f;
			knockMomentumScale += smooth * Time.deltaTime;
			knockMomentum = Mathf.Lerp(power, 0, knockMomentumScale);
			position = (knockMomentum * knockDirection);
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
		if (currentState == EnemyState.Attack)
		{
			float BounceRadius = .5f;
			if (Vector3.Distance(transform.position, target) <= BounceRadius)
			{
				CollisionBounce = true; //This is set to false in ResetAttack.
			}
			
			if (!CanAttack)
			{	
				rb.MovePosition(transform.position + position * MoveSpeed * Time.deltaTime);
			}
		}

		if (currentState == EnemyState.Knocked)
		{
			if (rb.bodyType != RigidbodyType2D.Static)
			{
				//Debug.Log("Moving knocked");
				rb.MovePosition(transform.position + position * MoveSpeed * Time.deltaTime);
			}
		}
	}
	
	//Follow event
	protected override void InRadiusEvent()
	{
		base.InRadiusEvent();
		ChangeState(EnemyState.Target);
		//Debug.Log("Moving target");
		position = Vector3.MoveTowards(transform.position, target, MoveSpeed * Time.deltaTime);
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
	protected override void KnockedEvent() //Move this and hitstunco to enemy script
	{
		base.KnockedEvent();
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
	protected override IEnumerator HitstunCo()
	{
		yield return base.HitstunCo();
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
		CollisionBounce = false;
	}
	
	private IEnumerator JumpAtTarget()
	{
		Debug.Log("Jump coroutine");
		//Animation for winding up attack
		JumpPosition = (transform.position - target).normalized * -1;
		yield return CustomTimer.Timer(.5f);
		CanAttack = false;
		JellyAnim.Attacking = true; //Jump anim
		yield return CustomTimer.Timer(.5f);
		
		currentState = EnemyState.Idle;
		JellyAnim.Attacking = false; //Idle anim
		yield return CustomTimer.Timer(1f);
		ResetJump();
	}
}
//Known issues:
//The enemy will sometimes teleport back before first jumping towards the player.

//TO DO
//During the first half of the prep jump, enemy can be knocked away.
//During the second half, the enemy won't be knocked and will continue its jump.

//NOTES:
//For some reason, the jelly would recieve an extra small jump after leaving the knocked state. The problem seems to be
//fixed after removing collision detection between the pet and enemy, and putting the pet's attack hitbox on a different
//layer.
