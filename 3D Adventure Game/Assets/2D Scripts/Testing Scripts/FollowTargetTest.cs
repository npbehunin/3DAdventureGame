using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowTargetTest : MonoBehaviour {

	public Transform target;

	public bool CanTarget;
	public bool CanDoCoroutine;
	public bool IsPaused;
	
	public float chaseRadius;
	public float stopRadius;
	public float MoveSpeed;
	public Rigidbody2D rb;

	public Coroutine delaycoroutine;
	
	// Use this for initialization
	void Start ()
	{
		CanDoCoroutine = true;
	}

	void Update()
	{
		PauseGame.IsPaused = IsPaused;
	}
	
	// Update is called once per frame
	void FixedUpdate () 
	{
		if (!IsPaused)
		{
			CheckDistance();
		}
	}

	void CheckDistance()
	{
		if (Vector3.Distance(target.position, transform.position) <= chaseRadius
		    && Vector3.Distance(target.position, transform.position) > stopRadius)
		{
			if (CanDoCoroutine)
			{
				CanDoCoroutine = false;
				delaycoroutine = StartCoroutine(DelayFollow());
			}
		}
		
		//if (Vector3.Distance(target.position, transform.position) <= stopRadius)
		//{
			//CanTarget = false;
		//}
		
		if (CanTarget)
		{
			Vector3 temp = Vector3.MoveTowards(transform.position, target.position, MoveSpeed * Time.deltaTime);
			rb.MovePosition(temp);
		}
	}

	//IEnumerator DelayFollow()
	//{
	//	CanTarget = false;
	//	yield return new WaitForSeconds(1f);
	//	CanTarget = true;
	//	yield return new WaitForSeconds(1f);
	//	CanDoCoroutine = true;
	//}
	
	IEnumerator DelayFollow()
	{
		CanTarget = false;
		yield return CustomTimer.Timer(1f);
		CanTarget = true;
		yield return CustomTimer.Timer(1f);
		CanDoCoroutine = true;
	}

	
}