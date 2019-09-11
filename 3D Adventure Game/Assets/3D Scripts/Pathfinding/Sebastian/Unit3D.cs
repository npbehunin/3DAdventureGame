using UnityEngine;
using System.Collections;

//The same script as UnitFollow, but directly handles transform movement.
public class UnitFollow3D : MonoBehaviour {

	const float minPathUpdateTime = .5f; //Default .2
	const float pathUpdateMoveThreshold = 1f; //Default .5

	public Transform target;
	public float speed = 20;
	public float turnSpeed = 3;
	public float turnDst = 5;
	public float stoppingDst = 10;

	Path3D path;

	void Start() {
		StartCoroutine (UpdatePath ());
	}

	public void OnPathFound(Vector3[] waypoints, bool pathSuccessful) {
		if (pathSuccessful) {
			path = new Path3D(waypoints, transform.position, turnDst, stoppingDst);

			StopCoroutine("FollowPath");
			StartCoroutine("FollowPath");
		}
		else
		{
			//Cannot reach the player!
			//Stop the coroutines and set canTeleport to true.
		}
	}

	IEnumerator UpdatePath() {

		if (Time.timeSinceLevelLoad < .3f) {
			yield return new WaitForSeconds (.3f);
		}
		PathRequestManager3D.RequestPath (new PathRequest3D(transform.position, target.position, OnPathFound));

		float sqrMoveThreshold = pathUpdateMoveThreshold * pathUpdateMoveThreshold;
		Vector3 targetPosOld = target.position;

		while (true) {
			yield return new WaitForSeconds (minPathUpdateTime);
			print (((target.position - targetPosOld).sqrMagnitude) + "    " + sqrMoveThreshold);
			if ((target.position - targetPosOld).sqrMagnitude > sqrMoveThreshold) {
				PathRequestManager3D.RequestPath (new PathRequest3D(transform.position, target.position, OnPathFound));
				targetPosOld = target.position;
			}
		}
	}

	IEnumerator FollowPath() {

		bool followingPath = true;
		int pathIndex = 0; //Should reset the index to 0. If not, reset it when following path is false.
		transform.LookAt (path.lookPoints [0]);

		float speedPercent = 1;

		while (followingPath) {
			Vector2 pos2D = new Vector2 (transform.position.x, transform.position.z);
			while (path.turnBoundaries [pathIndex].HasCrossedLine (pos2D)) {
				if (pathIndex == path.finishLineIndex) {
					followingPath = false;
					break;
				} else {
					pathIndex++;
				}
			}

			if (followingPath) {

				if (pathIndex >= path.slowDownIndex && stoppingDst > 0) {
					speedPercent = Mathf.Clamp01 (path.turnBoundaries [path.finishLineIndex].DistanceFromPoint (pos2D) / stoppingDst);
					if (speedPercent < 0.01f) {
						followingPath = false;
					}
				}

				Quaternion targetRotation = Quaternion.LookRotation (path.lookPoints [pathIndex] - transform.position);
				transform.rotation = Quaternion.Lerp (transform.rotation, targetRotation, Time.deltaTime * turnSpeed);
				transform.Translate (Vector3.forward * Time.deltaTime * speed * speedPercent, Space.Self);
			}

			yield return null;

		}
	}

	public void OnDrawGizmos() {
		if (path != null) {
			path.DrawWithGizmos ();
		}
	}
}

//NOTES
//Any minor changes that were required to prevent errors were added.
//"Path3D" and "Line3D" are only necessary for smoothing the path.