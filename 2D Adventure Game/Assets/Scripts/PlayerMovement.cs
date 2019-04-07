using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.UIElements;
using UnityEngine;

public class PlayerMovement : MonoBehaviour 
{

	private CharacterController Controller;
	public Animator animator;

	private float horizontalspeed;
	private float verticalspeed;

	public float MoveSpeed;
	public Vector3 position;

	void Start ()
	{
		Controller = GetComponent<CharacterController>();
		MoveSpeed = 3;

		animator = gameObject.GetComponent<Animator>();
	}

	void Update()
	{
		position.Set((MoveSpeed * Input.GetAxisRaw("Horizontal")), (MoveSpeed * Input.GetAxisRaw("Vertical")), 0);
		Controller.Move(position * Time.deltaTime);

		horizontalspeed = position.x;
		verticalspeed = position.y;

		animator.SetFloat("SpeedX", horizontalspeed);
		animator.SetFloat("SpeedY", verticalspeed);
	}
}