using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerRigidbodyMovement : MonoBehaviour
{

	public Rigidbody2D rb;
	public Animator animator;

	private float horizontalspeed;
	private float verticalspeed;

	public float MoveSpeed;
	public Vector3 position;

	void Start ()
	{
		rb = GetComponent<Rigidbody2D>();
		MoveSpeed = .06f;

		animator = gameObject.GetComponent<Animator>();
	}

	void FixedUpdate()
	{
		rb.MovePosition(transform.position + position);
	}

	void Update()
	{
		position.Set((MoveSpeed * Input.GetAxisRaw("Horizontal")), (MoveSpeed * Input.GetAxisRaw("Vertical")), 0);

		Debug.Log(position);
		horizontalspeed = position.x;
		verticalspeed = position.y;

		animator.SetFloat("SpeedX", horizontalspeed);
		animator.SetFloat("SpeedY", verticalspeed);
	}
}
