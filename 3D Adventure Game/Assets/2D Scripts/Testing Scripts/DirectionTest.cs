using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DirectionTest : MonoBehaviour
{

	public Transform target;
	public float MoveSpeed;
	public Rigidbody rb;
	public Vector3 position;
	public Vector3 dir;
	
	void Start () 
	{
		dir = (target.position - transform.position);
	}
	
	void Update ()
	{
		position = (transform.position + dir * MoveSpeed * Time.deltaTime);
		rb.MovePosition(position);
	}
}
