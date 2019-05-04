using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum AnimatorDirection
{
	Up, Down, Left, Right
}

public class LookTowardsTarget : MonoBehaviour {
	
    public Transform target;
	public bool UseMouse;
	public bool CanTarget;

	private Vector3 targetDir;

	public AnimatorDirection direction;
    
	void Start () 
	{
		
	}
	
	// Update is called once per frame
	void Update ()
	{
		if (CanTarget)
		{
			if (UseMouse)
			{
				targetDir = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 10)) - transform.position;
				//Debug.Log(Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0)));
				//Debug.Log(targetDir);
			}
			else
			{
				targetDir = target.position - transform.position;
			}
			
			float angle = Vector3.SignedAngle(targetDir, transform.right, Vector3.back);
			
			if (angle > 45f && angle < 135f)
			{
				direction = AnimatorDirection.Up;
			}

			if (angle > 135f || angle < -135f)
			{
				direction = AnimatorDirection.Left;
			}

			if (angle > -135 && angle < -45)
			{
				direction = AnimatorDirection.Down;
			}

			if (angle > -45 && angle < 45)
			{
				direction = AnimatorDirection.Right;
			}

			//Debug.Log(direction);
			//Debug.Log(angle);
		}
	}
}
