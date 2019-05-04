using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;

public enum PlayerState
{
	Idle, Walk, Run, Attack, Interact, Dead
}

public class PlayerMovement : MonoBehaviour
{

	public PlayerState currentState;
	public Rigidbody2D rb;
	public Animator animator;
	public LookTowardsTarget targetMode;
	//public ShootingMechanic shootMechanic;

	private float horizontalspeed;
	private float verticalspeed;
	public float SwordMomentumScale;
	public float SwordMomentum;
	public float SwordMomentumSmooth;
	public float SwordMomentumPower;
	public float MoveSpeed;

	public Vector3 test;

	public Vector3 position;

	void Start()
	{
		currentState = PlayerState.Idle;
		rb = GetComponent<Rigidbody2D>();
		//animator = gameObject.GetComponent<Animator>();
		SwordMomentumSmooth = 4f;
		SwordMomentumPower = 1f;
	}

	void FixedUpdate()
	{
		if (currentState == PlayerState.Run || currentState == PlayerState.Walk || currentState == PlayerState.Attack)
		{
			rb.MovePosition(transform.position + position * Time.deltaTime);
		}
	}

	void Update()
	{
		if (currentState != PlayerState.Attack)
		{
			position.Set((MoveSpeed * Input.GetAxisRaw("Horizontal")), (MoveSpeed * Input.GetAxisRaw("Vertical")), 0);
		}

		if (position != Vector3.zero)
		{
			horizontalspeed = position.x;
			verticalspeed = position.y;

			//Run
			if (currentState == PlayerState.Idle || currentState == PlayerState.Run)
			{
				animator.SetFloat("SpeedX", horizontalspeed);
				animator.SetFloat("SpeedY", verticalspeed);
				currentState = PlayerState.Run;
				MoveSpeed = 4;
				animator.SetBool("Running", true);
			}

			if (currentState != PlayerState.Run)
			{
				animator.SetBool("Running", false);
			}

			//Walk
			if (currentState == PlayerState.Walk)
			{
				MoveSpeed = 2;
				animator.SetBool("Walking", true);
			}
			else
			{
				MoveSpeed = 4;
				animator.SetBool("Walking", false);
			}
		}

		//Idle
		if (currentState != PlayerState.Attack)
		{
			if (position == Vector3.zero)
			{
				currentState = PlayerState.Idle;
				animator.SetBool("Running", false);
			}
		}

		//Attack
		if (currentState == PlayerState.Attack)
		{
			animator.SetFloat("SpeedX", horizontalspeed);
			animator.SetFloat("SpeedY", verticalspeed);
			SwordMomentumScale += SwordMomentumSmooth * Time.deltaTime;
			SwordMomentum = Mathf.Lerp(SwordMomentumPower, 0, SwordMomentumScale);
			position = (MoveSpeed * SwordMomentum * test);
			//Debug.Log(SwordMomentumPower);
		}
	}

	public void GetSwordSwingDirection()
	{
		test = new Vector3(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"), 0);
	}
}