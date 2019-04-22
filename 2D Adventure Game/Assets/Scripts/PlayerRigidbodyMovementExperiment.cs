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
	public LookTowardsTarget targetMode;
	public ShootingMechanic shootMechanic;

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
		if (currentState == PlayerState.Run || currentState == PlayerState.Walk)
		{
			rb.MovePosition(transform.position + position * Time.deltaTime);
		}
	}

	void Update()
	{
		position.Set((MoveSpeed * Input.GetAxisRaw("Horizontal")), (MoveSpeed * Input.GetAxisRaw("Vertical")), 0);

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

		//Charging Bow
		if (shootMechanic.WeaponEquipped)
		{
			if (Input.GetMouseButton(0))
			{
				animator.SetBool("IsChargingBow", true);
				currentState = PlayerState.Walk;
				targetMode.CanTarget = true;

				switch (targetMode.direction)
				{
					case AnimatorDirection.Up:
						animator.SetFloat("DirectionY", 1);
						animator.SetFloat("DirectionX", 0);
						animator.SetFloat("SpeedX", 0);
						animator.SetFloat("SpeedY", 1);
						break;
					case AnimatorDirection.Down:
						animator.SetFloat("DirectionY", -1);
						animator.SetFloat("DirectionX", 0);
						animator.SetFloat("SpeedX", 0);
						animator.SetFloat("SpeedY", -1);
						break;
					case AnimatorDirection.Left:
						animator.SetFloat("DirectionY", 0);
						animator.SetFloat("DirectionX", -1);
						animator.SetFloat("SpeedX", -1);
						animator.SetFloat("SpeedY", 0);
						break;
					case AnimatorDirection.Right:
						animator.SetFloat("DirectionY", 0);
						animator.SetFloat("DirectionX", 1);
						animator.SetFloat("SpeedX", 1);
						animator.SetFloat("SpeedY", 0);
						break;
					default:
						Debug.Log("No angle detected");
						break;
				}
			}

			if (Input.GetMouseButtonUp(0))
			{
				targetMode.CanTarget = false;
				animator.SetBool("IsChargingBow", false);
				currentState = PlayerState.Idle;
			}
		}
		//Debug.Log(currentState);
		Debug.Log(targetMode.direction);
		Debug.Log(MoveSpeed);
	}
}