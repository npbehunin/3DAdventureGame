using UnityEngine;
using System.Collections;

public class UnitFollow : MonoBehaviour {


	//public Transform target;
	float speed = 3.5f;
	public Vector3[] path;
	int targetIndex;
	public Coroutine FollowPathCoroutine;
	public Coroutine UpdateThePath;
	public bool CannotReachPlayer;
	public BoolValue PetCanFollowPath;
	public Vector3Value TargetTransform;

	void Start() {
		//PathRequestManager.RequestPath(transform.position,target.position, OnPathFound);
		//CanFollowPath = true;
		CannotReachPlayer = false;
		//This method needs to stop once the pet's linecast with the player is true. We need to tell everything to reset.
	}

	void Update()
	{
		Debug.Log(targetIndex);
	}

	public void StopFollowPath()
	{
		PetCanFollowPath.initialBool = true;
		if (FollowPathCoroutine != null)
		{
			StopCoroutine(FollowPathCoroutine);
		}

		if (UpdateThePath != null)
		{
			StopCoroutine(UpdateThePath);
		}
	}

	public void CheckIfCanFollowPath()
	{
		//Enable this when we want to enable bools again
		//if (CanFollowPath)
		{
			//CanFollowPath = false;
			Debug.Log("Requesting a path");
			PathRequestManager.RequestPath(transform.position, TargetTransform.initialPos, OnPathFound);
		}
	}
	
	public void OnPathFound(Vector3[] newPath, bool pathSuccessful) { //When a path is found...
		if (pathSuccessful) {
			path = newPath;
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
			CannotReachPlayer = true;
			StopFollowPath();
		}
	}

	IEnumerator FollowPath()
	{
		//...follow the path!
		if (path.Length != 0)
		{
			Vector3 currentWaypoint = path[0]; //OUT OF RANGE ERROR HERE
			UpdateThePath = StartCoroutine(UpdatePath());
			while (true)
			{
				if (transform.position == currentWaypoint)
				{
					targetIndex++;
					if (targetIndex >= path.Length)
					{
						Debug.Log("Hiya test"); //THIS IS THE END OF THE PATH FOLLOWING.
						yield break;
					}

					currentWaypoint = path[targetIndex];
				}

				//Debug.Log("REEE");
				//CanReachEnemy.initialBool = true;
				transform.position =
					Vector3.MoveTowards(transform.position, currentWaypoint,
						speed * Time.deltaTime); //Adjust this to work with our movement script
				yield return null;
			}
		}
		else //Not actually redundant lol
		{
			StopFollowPath();
			Debug.Log("AAGUh");
		}
	}

	IEnumerator UpdatePath()
	{
		yield return new WaitForSeconds(2f);
		StopFollowPath();
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

//KNOWN ISSUES
//If the player is inside red points on the grid, the pet will teleport. This means even if the player isn't colliding
//with the wall, the player can still be in the red point on the grid.

//NOTES
//I added a second check on followpath to make sure the length of the array is greater than 0, that way if the path
//length IS zero we won't get an error when we set the currentwaypoint.