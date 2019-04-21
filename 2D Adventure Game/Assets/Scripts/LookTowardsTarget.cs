using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum animatorDirection
{
	Up, Down, Left, Right
}
public class LookTowardsTarget : MonoBehaviour {

    public Transform target;
	public bool UseMouse;

	private animatorDirection direction;
    
	void Start () 
	{
		
	}
	
	// Update is called once per frame
	void Update ()
	{
		Vector3 targetDir = target.position - transform.position;
		float angle = Vector3.SignedAngle(targetDir, transform.right, Vector3.back);

		if (angle > 45f && angle < 135f)
		{
			direction = animatorDirection.Up;
		}
		
		if (angle > 135f || angle < -135f)
		{
			direction = animatorDirection.Left;
		}
		
		if (angle > -135 && angle < -45)
		{
			direction = animatorDirection.Down;
		}
		
		if (angle > -45 && angle < 45)
		{
			direction = animatorDirection.Right;
		}
		Debug.Log(direction);
		//Debug.Log(angle);
	}
}
