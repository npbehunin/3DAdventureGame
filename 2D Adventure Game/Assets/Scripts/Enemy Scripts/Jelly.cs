using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Jelly : Enemy
{
	void Start ()
	{
		currentState = EnemyState.Idle;
		rb = GetComponent<Rigidbody2D>();
		target = GameObject.FindWithTag("Player").transform;
		Health = 3;
		Damage = 1;
	}
	
	void FixedUpdate () 
	{
		CheckDistance();
	}
}
