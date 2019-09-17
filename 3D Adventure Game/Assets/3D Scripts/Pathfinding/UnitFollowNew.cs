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
	public bool CanReachTarget;
	//private BoolValue PetCanFollowPath;
	//private Vector3Value TargetTransform;
	//public Transform target;

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
		//Debug.Log(targetIndex);
	}

	public void StopFollowPath()
	{
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
		Debug.Log("Requesting a path");
		PathRequestManagerNew.RequestPath(transform.position, targetPosition, OnPathFound);
	}
	
	public void OnPathFound(Vector3[] newPath, bool pathSuccessful) { //When a path is found...
		if (pathSuccessful)
		{
			CanReachTarget = true;
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
			CanReachTarget = false;
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
				//Debug.Log(Vector3.Distance(Vector3.ProjectOnPlane(currentWaypoint, transform.position), motorUpDirection));
				if (Vector3.Distance(Vector3.ProjectOnPlane(currentWaypoint, transform.position), motorUpDirection) < .75f)
				{
					targetIndex++;
					if (targetIndex >= path.Length)
					{
						Debug.Log("Hiya test"); //THIS IS THE END OF THE PATH FOLLOWING.
						yield break;
					}

					currentWaypoint = path[targetIndex];
				}
				
				targetPathPosition = currentWaypoint; //Set our target position to be the waypoint.
				yield return null;
			}
		}
		else //Not actually redundant lol
		{
			StopFollowPath();
			Debug.Log("AAGUh");
		}
	}

	//IEnumerator UpdatePath()
	//{
	//	yield return new WaitForSeconds(.2f);
	//	StopFollowPath();
	//}

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
//The pathfinding will still create a path on slopes the pet can't traverse over. We either need to check the slope on our
//raycast hit (which would still only give us the normal of the direct point right below the node), or create something like
//a new collider or layer on slides called "slideable" or something that returns an unwalkable gridpoint (better? because it
//checks a collision instead of a raycast.

//KNOWN ISSUES
//If the player is inside red points on the grid, the pet will teleport. This means even if the player isn't colliding
//with the wall, the player can still be in the red point on the grid.

//NOTES
//I added a second check on followpath to make sure the length of the array is greater than 0, that way if the path
//length IS zero we won't get an error when we set the currentwaypoint.