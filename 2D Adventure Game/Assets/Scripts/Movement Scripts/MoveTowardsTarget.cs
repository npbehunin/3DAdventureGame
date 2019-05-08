using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class MoveTowardsTarget : MonoBehaviour {

	public Transform target;

	public bool CanTarget;
	public bool IsPet;
	
	public float chaseRadius;
	public float stopRadius;
	public float warpRadius;
	public float MoveSpeed;
	public Rigidbody2D rb;
	
	// Use this for initialization
	void Start ()
	{
		
	}

	void Update()
	{
		
	}
	
	// Update is called once per frame
	void FixedUpdate () 
	{
		CheckDistance();
	}

	void CheckDistance()
	{
		if (Vector3.Distance(target.position, transform.position) <= chaseRadius
		&& Vector3.Distance(target.position, transform.position) > stopRadius)
		{
			CanTarget = true;
		}
		
		if (Vector3.Distance(target.position, transform.position) <= stopRadius)
		{
			CanTarget = false;
		}
		
		if (Vector3.Distance(target.position, transform.position) > warpRadius)
		{
			if (IsPet)
			{
				OutOfRange();
			}
		}
		
		if (CanTarget)
		{
			Vector3 temp = Vector3.MoveTowards(transform.position, target.position, MoveSpeed * Time.deltaTime);
			rb.MovePosition(temp);
		}
	}

	void OutOfRange()
	{
		Debug.Log("OutOfRange");
		transform.position = target.position;
	}
}
