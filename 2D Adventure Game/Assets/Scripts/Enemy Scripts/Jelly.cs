using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Jelly : Enemy
{
	protected override void Start ()
	{
		currentState = EnemyState.Idle;
		rb = GetComponent<Rigidbody2D>();
		target = GameObject.FindWithTag("Player").transform;
		Health = 10;
		Damage = 1;
		JumpMomentumSmooth = 4f;
		JumpMomentumPower = 4f;
	}
	
	//void FixedUpdate () 
	//{
		//CheckDistance();
	//}
}
