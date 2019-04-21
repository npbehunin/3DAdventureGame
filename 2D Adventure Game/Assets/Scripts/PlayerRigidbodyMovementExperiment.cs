using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;

public enum PlayerState
{
	Idle, Walk, Run, Attack, Interact
}

public class PlayerRigidbodyMovementExperiment : MonoBehaviour 
{

	public PlayerState currentState;
	public Rigidbody2D rb;
	public Animator animator;

	private float horizontalspeed;
	private float verticalspeed;
	public float MoveSpeed;
	
	
	
	public Vector3 position;

	void Start ()
	{
		currentState = PlayerState.Idle;
		rb = GetComponent<Rigidbody2D>();
		animator = gameObject.GetComponent<Animator>();
	}

	void FixedUpdate()
	{
		if (currentState == PlayerState.Run)
		{
			rb.MovePosition(transform.position + position * Time.deltaTime);
		}
	}

	void Update()
	{
		position.Set((MoveSpeed * Input.GetAxisRaw("Horizontal")), (MoveSpeed * Input.GetAxisRaw("Vertical")), 0);
		horizontalspeed = position.x;
		verticalspeed = position.y;

		if (position != Vector3.zero)
		{
			//Run
			if (currentState == PlayerState.Idle || currentState == PlayerState.Run)
			{
				currentState = PlayerState.Run;
				MoveSpeed = 4;
				animator.SetBool("Running", true);
				animator.SetFloat("SpeedX", horizontalspeed);
				animator.SetFloat("SpeedY", verticalspeed);
			}
			
			//Walk
			if (currentState == PlayerState.Walk)
			{
				MoveSpeed = 2;
				//animator.SetBool("Walking", true);
			}
			//else
			{
				//animator.SetBool("Walking", false);
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
	}
}