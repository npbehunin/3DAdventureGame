using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Jelly : Enemy
{

	public Transform target;
	public Transform home;

	public float chaseRadius;
	public float attackRadius;

	void Start ()
	{
		target = GameObject.FindWithTag("Player").transform;
	}
	
	void FixedUpdate () 
	{
		CheckDistance();
	}

	void CheckDistance()
	{
		if (Vector3.Distance(target.position, transform.position) <= chaseRadius 
		    && Vector3.Distance(target.position, transform.position) > attackRadius)
		{
			transform.position = Vector3.MoveTowards(transform.position, target.position, MoveSpeed * Time.deltaTime);
		}
	}
}
