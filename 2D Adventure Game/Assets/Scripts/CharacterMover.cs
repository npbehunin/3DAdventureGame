using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters;
using UnityEngine;
using UnityEngine.UI;

public class CharacterMover : MonoBehaviour
{
	private CharacterController Controller;
	
	public float Gravity;
	public float MoveSpeed;
	public float JumpSpeed;
	
	public Vector3 position;
	private Vector3 rotation;

	Rigidbody m_rb;
	private RollingRock rollingrock;

	private float distance = -1.3f;
	private int rockmask = 1 << 9;

	
	void Start ()
	{
		Controller = GetComponent<CharacterController>();
		//m_rb = rollingrock.GetComponent<Rigidbody>();
	}
	
	void Update ()
	{
		{
			position.Set(MoveSpeed * Input.GetAxis("Horizontal"), position.y, 0);
			if (Controller.isGrounded)
			{
				position.Set(MoveSpeed * Input.GetAxis("Horizontal"), 0, 0);

				if (Input.GetButton("Jump"))
				{
					position.y = JumpSpeed;
				}
			}
		}
		position.y -= Gravity * Time.deltaTime;
		//normalposition.y = position.y + Gravity * Time.deltaTime;
		Controller.Move(position * Time.deltaTime);
	}

	void OnTriggerStay(Collider other)
	{
		if (other.gameObject.CompareTag("RollingRock"))
		{
			//if (Input.GetAxis("Horizontal") > 0)
			{
				MoveSpeed = 7f;
			}
			//else MoveSpeed = 9f;
		}
	}

	void OnCollisionStay(Collision other)
	{
		if (other.gameObject.CompareTag("RollingRock"))
		{
			RaycastHit hit;
			if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.right), out hit, Mathf.Infinity, rockmask))
			{
				if (Input.GetAxis("Horizontal") > 0)
				{
					Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.right) * 100);
					Debug.Log("Stopped right side");
					MoveSpeed = 0f;
				}
			}


			if (Physics.Raycast(transform.position, transform.TransformDirection(-Vector3.right), out hit, Mathf.Infinity, rockmask))
			{
				if (Input.GetAxis("Horizontal") < 0)
				{
					Debug.DrawRay(transform.position, transform.position + transform.TransformDirection(-Vector3.right) * 100);
					Debug.Log("Stopped left side");
					MoveSpeed = 0f;
				}
			}
			//else MoveSpeed = 9f;
		}
	}

	void OnTriggerExit(Collider other)
	{
		if (other.gameObject.CompareTag("RollingRock"))
		{
			MoveSpeed = 9f;
		}
	}
}