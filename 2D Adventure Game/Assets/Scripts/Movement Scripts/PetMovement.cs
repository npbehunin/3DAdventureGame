using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PetMovement : MonoBehaviour {

	public Transform target;
	public bool CanFollowPlayer, FollowPath; //CanFollowPath;
	
	public float ChaseRadius;
	public float WalkRadius;
	public float StopRadius;
	public float WarpRadius;
	public float MoveSpeed;
	public Rigidbody2D rb;

	public PetState CurrentState;

	public PlayerMovement player;
	public UnitFollow path;
	
	void Start () 
	{
		
	}

	void FixedUpdate()
	{
		if (!FollowPath)
		{
			
			path.StopFollowPath();
			CheckDistance();
		}
	}
	
	void Update () 
	{
		if (FollowPath)
		{
			//CanFollowPath = true;
			path.CheckIfCanFollowPath();
		}
		
		if (Vector3.Distance(target.position, transform.position) > WarpRadius)
		{
			OutOfRange();
		}

		if (CurrentState == PetState.Run)
		{
			MoveSpeed = player.MoveSpeed;
		}

		if (CurrentState == PetState.Walk)
		{
			MoveSpeed = 1;
		}

		if (CurrentState == PetState.Idle)
		{
			MoveSpeed = 0;
		}

		//Check any collision between pet and player
		RaycastHit hit;
		int wallLayerMask = 1 << 9;
		if (Physics2D.Linecast(transform.position, target.position, wallLayerMask))//, 15, wallLayerMask))
		{
			//Debug.Log("Hello");
			FollowPath = true;
			//Debug.DrawRay(transform.position, hit.point, Color.yellow, 30f, false);
		}
		else
		{
			//Debug.Log("Oy don't follow path");
			//Debug.DrawRay(transform.position, target.position, Color.yellow, 30f, false);
			FollowPath = false;
		}
	}
		
	void CheckDistance()
	{
		if (Vector3.Distance(target.position, transform.position) <= ChaseRadius
		    && Vector3.Distance(target.position, transform.position) > WalkRadius)
		{
			CanFollowPlayer = true;
			CurrentState = PetState.Run;
		}
		
		if (Vector3.Distance(target.position, transform.position) <= WalkRadius
		    && Vector3.Distance(target.position, transform.position) > StopRadius)
		{
			if (CurrentState == PetState.Idle)
			{
				StartCoroutine(IdleWaitTime());
			}
			else
			{
				CanFollowPlayer = true;
				CurrentState = PetState.Walk;
			}
		}
		
		if (Vector3.Distance(target.position, transform.position) <= StopRadius)
		{
			CanFollowPlayer = false;
			CurrentState = PetState.Idle;
		}
		
		if (CanFollowPlayer)
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

	private IEnumerator IdleWaitTime()
	{
		yield return new WaitForSeconds(.75f);
		CanFollowPlayer = true;
		CurrentState = PetState.Run;
	}
}

//TO DO

//Pathfinding works great. A new path is created when the pet gets out of range. Adjust the bools to work consistently
//and edit the warp radius a bit so the pet can actually complete certain paths.
