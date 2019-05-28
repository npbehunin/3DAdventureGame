using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;

public enum PlayerState
{
	Idle, Walk, Run, Attack, Paused, Dead
}

public class PlayerMovement : MonoBehaviour
{
	public PlayerState currentState, laststate;
	public Rigidbody2D rb;
	public Animator animator;
	public LookTowardsTarget targetMode;
	//public Hitstun hitstun;

	private float horizontalspeed, verticalspeed;
	public float SwordMomentumScale, SwordMomentum, SwordMomentumSmooth, SwordMomentumPower, MoveSpeed;

	public Vector3 test, position;
	
	void Start()
	{
		currentState = PlayerState.Idle;
		rb = GetComponent<Rigidbody2D>();
		SwordMomentumSmooth = 4f;
		SwordMomentumPower = 1f;
	}

	void FixedUpdate()
	{
		if (currentState == PlayerState.Run || currentState == PlayerState.Walk || currentState == PlayerState.Attack)
		{
			rb.MovePosition(transform.position + position * MoveSpeed * Time.deltaTime);
		}
	}

	void Update()
	{
		CheckForPause();
		if (currentState != PlayerState.Attack && currentState != PlayerState.Paused)
		{
			position = new Vector3(Input.GetAxisRaw("Horizontal"), (Input.GetAxisRaw("Vertical")), 0).normalized;
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
		if (currentState != PlayerState.Attack && currentState != PlayerState.Paused)
		{
			if (position == Vector3.zero)
			{
				currentState = PlayerState.Idle;
				animator.SetBool("Running", false);
				test = Vector3.zero;
			}
		}

		//Attack
		if (currentState == PlayerState.Attack)
		{
			animator.SetFloat("SpeedX", horizontalspeed);
			animator.SetFloat("SpeedY", verticalspeed);
			SwordMomentumScale += SwordMomentumSmooth * Time.deltaTime;
			SwordMomentum = Mathf.Lerp(SwordMomentumPower, 0, SwordMomentumScale);
			//Debug.Log(SwordMomentumScale);
			position = (SwordMomentum * test);
		}
	}

	//Check if game is paused
	public void CheckForPause()
	{
		if (PauseGame.IsPaused)
		{
			currentState = PlayerState.Paused;
		}

		if (Input.GetKeyDown(KeyCode.Alpha8))
		{
			if (!PauseGame.IsPaused)
			{
				laststate = currentState;
				PauseGame.PauseTheGame();
			}
			else
			{
				PauseGame.UnpauseTheGame();
				currentState = laststate;
			}
		}
		
		if (currentState == PlayerState.Paused)
		{
			Debug.Log("Player paused!");
			rb.bodyType = RigidbodyType2D.Static;
		}
		else
		{
			rb.bodyType = RigidbodyType2D.Dynamic;
		}
	}
	
	public void GetSwordSwingDirection()
	{
		test = new Vector3(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"), 0).normalized;
	}
}