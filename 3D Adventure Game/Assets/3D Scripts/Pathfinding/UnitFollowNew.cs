using System;
using UnityEngine;
using System.Collections;

public class UnitFollowNew : MonoBehaviour {


	//public Transform target;
	float speed = 3.5f;
	public Vector3[] path;
	public int targetIndex;
	public Coroutine FollowPathCoroutine;
	public Coroutine UpdateThePath;
	
	public bool PathIsActive;
	public bool CheckingPath;

	public Vector3 motorUpDirection;
	public Vector3 targetPathPosition;

	void Start() {
		//PathRequestManager.RequestPath(transform.position,target.position, OnPathFound);
		//CanFollowPath = true;
		//CanReachTarget = true;
		//This method needs to stop once the pet's linecast with the player is true. We need to tell everything to reset.
	}

	void Update()
	{
		//Debug.Log(PathIsActive);
	}

	public void StopFollowPath()
	{
		PathIsActive = false;
		if (FollowPathCoroutine != null)
		{
			StopCoroutine(FollowPathCoroutine);
		}

		if (UpdateThePath != null)
		{
			StopCoroutine(UpdateThePath);
		}
	}

	public void CheckIfCanFollowPath(Vector3 targetPosition)
	{
		CheckingPath = true;
		Debug.Log("Requesting a path");
		PathRequestManagerNew.RequestPath(transform.position, targetPosition, OnPathFound);
	}
	
	public void OnPathFound(Vector3[] newPath, bool pathSuccessful) { //When a path is found...
		CheckingPath = false;
		if (pathSuccessful)
		{
			//CanReachTarget = true;
			PathIsActive = true;
			path = newPath;
			Debug.Log("Path successful");
			targetIndex = 0;
			if (FollowPathCoroutine != null)
			{
				StopCoroutine(FollowPathCoroutine);
			}

			//Debug.Log("Path successful");
			FollowPathCoroutine = StartCoroutine(FollowPath());
		}
		else
		{
			Debug.Log("Can't reach target");
			StopFollowPath();
		}
	}

	IEnumerator FollowPath()
	{
		//...follow the path!
		if (path.Length != 0)
		{
			Vector3 currentWaypoint = path[0]; //OUT OF RANGE ERROR HERE
			//UpdateThePath = StartCoroutine(UpdatePath());
			while (true)
			{
				//This crap
				Vector3 dir = currentWaypoint - transform.position;
				Vector3 test = Vector3.ProjectOnPlane(dir, motorUpDirection);
				if (test.magnitude < .75f)
				{
					targetIndex++;
					if (targetIndex >= path.Length)
					{
						Debug.Log("Hiya test"); //THIS IS THE END OF THE PATH FOLLOWING.
						StopFollowPath();
						yield break;
					}

					currentWaypoint = path[targetIndex];
				}
				
				targetPathPosition = currentWaypoint; //Set our target position to be the waypoint.
				yield return null;
			}
		}
		else
		{
			Debug.Log("AAGUh");
			StopFollowPath();
		}
	}

	public void OnDrawGizmos() {
		if (path != null) {
			for (int i = targetIndex; i < path.Length; i ++) {
				Gizmos.color = Color.black;
				Gizmos.DrawCube(path[i], Vector3.one);

				if (i == targetIndex) {
					Gizmos.DrawLine(transform.position, path[i]);
				}
				else {
					Gizmos.DrawLine(path[i-1],path[i]);
				}
			}
		}
	}
}

//TO DO
//Nothing for now, unless a "leniency" option is needed.

//KNOWN ISSUES
//Areas underneath bridges or other floors will be affected by the areas above it (Since we raycast downwards.)
	//Idea 1:
	//Ignore it and just be aware of it when designing rooms.
	//Idea 2:
	//Have a second grid that the pathfinder will check (or "swap out") depending how close each individual enemy is.
//Since a path can run diagonally, it will sometimes "cut through" a wall with two neighboring diagonal unwalkable
	//points. This can be fixed by either making the walls thicker or adjusting the pathfinding to make sure there's
	//at least 1 direct neighbor.


//NOTES
//I added a second check on followpath to make sure the length of the array is greater than 0, that way if the path
//length IS zero we won't get an error when we set the currentwaypoint.