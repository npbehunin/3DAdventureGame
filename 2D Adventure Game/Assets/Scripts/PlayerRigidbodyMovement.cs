using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PlayerState
{
	Walk, Run, Attack, Interact
	
}
public class PlayerRigidbodyMovement : MonoBehaviour
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
		currentState = PlayerState.Run;
		rb = GetComponent<Rigidbody2D>();
		animator = gameObject.GetComponent<Animator>();
		MoveSpeed = .06f;
	}

	void FixedUpdate()
	{
		if (currentState == PlayerState.Run)
		{
			rb.MovePosition(transform.position + position);
		}
	}

	void Update()
	{
		if (currentState == PlayerState.Run)
		{
			position.Set((MoveSpeed * Input.GetAxisRaw("Horizontal")), (MoveSpeed * Input.GetAxisRaw("Vertical")), 0);

			horizontalspeed = position.x;
			verticalspeed = position.y;

			//Run Animator
			animator.SetFloat("SpeedX", horizontalspeed);
			animator.SetFloat("SpeedY", verticalspeed);
		}
	}
}
