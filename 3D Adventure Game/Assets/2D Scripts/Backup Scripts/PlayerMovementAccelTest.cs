using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovementAccelTest : MonoBehaviour {

	public PlayerState currentState;
	public Rigidbody2D rb;
	public Animator animator;
	public LookTowardsTarget targetMode;

	private float horizontalspeed;
	private float verticalspeed;
	
	public float SwordMomentumScale;
	public float SwordMomentum;
	public float SwordMomentumSmooth;
	public float SwordMomentumPower;

	public float SpeedTemp1;
	public float SpeedTemp2;
	public float MaxSpeed;
	public float MoveSpeed;

	public bool Accelerating;
	public bool Decelerating;

	public Vector3 test;
	public Vector3 test2;
	public bool cantest2;

	public Vector3 position;

	void Start()
	{
		MaxSpeed = 4;
		SpeedTemp1 = 1;
		SpeedTemp2 = 0;
		MoveSpeed = 0;
		currentState = PlayerState.Idle;
		rb = GetComponent<Rigidbody2D>();
		SwordMomentumSmooth = 4f;
		SwordMomentumPower = 1f;
	}

	void FixedUpdate()
	{
		if (currentState == PlayerState.Run || currentState == PlayerState.Walk || currentState == PlayerState.Attack)
		{
			if (position.x > 0 || position.x < 0 || position.y > 0 || position.y < 0)
			{
				//Debug.Log("Keyboard is being pressed");
				rb.MovePosition(transform.position + position * MoveSpeed * Time.deltaTime);
			}
		}
	}

	void Update()
	{
		if (currentState != PlayerState.Attack)
		{
			position = new Vector3(Input.GetAxisRaw("Horizontal"), (Input.GetAxisRaw("Vertical")), 0).normalized; //Keep this the same! Just change movespeed.
		}

		//Speed
		
		//A few problems: The position will multiply the movespeed which works great for acceleration, but deceleration
		//is still snapped because position's getaxisraw is set to 0. We can't just check getaxis because it would make
		//the player snap back after something like a sword swing if the key is still held down.
		if (MoveSpeed < MaxSpeed)
		{
			if (position.x != 0 || position.y != 0)
			{
				Debug.Log("hi");
				Accelerating = true;
			}
			else
			{
				Accelerating = false;
			}
		}
		else
		{
			Accelerating = false;
		}

		if (MoveSpeed > 0)
		{
			if (position.x == 0 && position.y == 0)
			{
				Decelerating = true;
			}
			else
			{
				Decelerating = false;
			}
		}
		else
		{
			Decelerating = false;
		}
		
		if (Accelerating)
		{
			cantest2 = true;
			if (MoveSpeed < MaxSpeed)
			{
				SpeedTemp1 = 1f;
				SpeedTemp2 += (SpeedTemp1 * Time.deltaTime);
				MoveSpeed = Mathf.Lerp(0, MaxSpeed, SpeedTemp2);
			}

			if (MoveSpeed >= MaxSpeed)
			{
				Accelerating = false;
			}
		}
		else if (Decelerating)
		{
			cantest2 = false;
			if (MoveSpeed > 0)
			{
				SpeedTemp1 = 1f;
				SpeedTemp2 += (SpeedTemp1 * Time.deltaTime);
				MoveSpeed = Mathf.Lerp(MaxSpeed, 0, SpeedTemp2);
			}

			if (MoveSpeed <= 0)
			{
				Decelerating = false;
			}
		}
		else
		{
			cantest2 = true;
			SpeedTemp1 = 1f;
			SpeedTemp2 = 0f;
		}
		
		//Vector3 for deceleration.
		if (cantest2)
		{
			//Debug.Log("Test 2 vector");
			test2 = new Vector3(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"), 0).normalized;
			cantest2 = false;
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
				MaxSpeed = 4;
				animator.SetBool("Running", true);
			}

			if (currentState != PlayerState.Run)
			{
				animator.SetBool("Running", false);
			}

			//Walk
			if (currentState == PlayerState.Walk)
			{
				MaxSpeed = 2;
				animator.SetBool("Walking", true);
			}
			else
			{
				MaxSpeed = 4;
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
				Debug.Log("New movement who dis");
				//Debug.Log("Keyboard is NOT being pressed");
				rb.MovePosition(transform.position + test2 * MoveSpeed * Time.deltaTime);
			}
		}

		//Attack
		if (currentState == PlayerState.Attack)
		{
			animator.SetFloat("SpeedX", horizontalspeed);
			animator.SetFloat("SpeedY", verticalspeed);
			SwordMomentumScale += SwordMomentumSmooth * Time.deltaTime;
			SwordMomentum = Mathf.Lerp(SwordMomentumPower, 0, SwordMomentumScale);
			position = (SwordMomentum * test);
		}
	}

	public void GetSwordSwingDirection()
	{
		test = new Vector3(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"), 0).normalized;
	}
}
