using UnityEngine;
using System.Collections;

public class UnitFollow : MonoBehaviour {


	public Transform target;
	float speed = 3.5f;
	Vector3[] path;
	int targetIndex;
	public Coroutine FollowPathCoroutine;
	public bool CanFollowPath;

	void Start() {
		PathRequestManager.RequestPath(transform.position,target.position, OnPathFound);
		CanFollowPath = true;
		//This method needs to stop once the pet's linecast with the player is true. We need to tell everything to reset.
	}

	void Update()
	{
		Debug.Log(targetIndex);
		//Debug.Log(path);
	}

	public void StopFollowPath()
	{
		if (FollowPathCoroutine != null)
		{
			StopCoroutine(FollowPathCoroutine);
			CanFollowPath = true;
		}
	}

	public void CheckIfCanFollowPath()
	{
		//Enable this when we want to enable bools again
		if (CanFollowPath)
		{
			CanFollowPath = false;
			Debug.Log("Doing the thing");
			PathRequestManager.RequestPath(transform.position,target.position, OnPathFound);
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

			FollowPathCoroutine = StartCoroutine(FollowPath());
		}
	}

	IEnumerator FollowPath()
	{
		//...follow the path!
		if (path != null)
		{
			Vector3 currentWaypoint = path[0];
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

				transform.position =
					Vector3.MoveTowards(transform.position, currentWaypoint,
						speed * Time.deltaTime); //Adjust this to work with our movement script
				yield return null;

			}
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

//TO DO 5/12/19

//Implement the movement script to work alongside this unit movement

//Tell the script to create a new path whenever the raycast between the pet and player results in a collision. Otherwise,
//do the simple follow movement we already have in our pet movement script.