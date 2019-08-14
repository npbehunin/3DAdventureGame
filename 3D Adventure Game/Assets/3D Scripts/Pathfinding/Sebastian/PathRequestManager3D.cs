using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Threading;

public class PathRequestManager3D : MonoBehaviour {

	Queue<PathResult> results = new Queue<PathResult>();

	static PathRequestManager3D instance;
	Pathfinding3D pathfinding;

	void Awake() {
		instance = this;
		pathfinding = GetComponent<Pathfinding3D>();
	}

	void Update() {
		if (results.Count > 0) {
			int itemsInQueue = results.Count;
			lock (results) {
				for (int i = 0; i < itemsInQueue; i++) {
					PathResult result = results.Dequeue ();
					result.callback (result.path, result.success);
				}
			}
		}
	}

	public static void RequestPath(PathRequest3D request) {
		ThreadStart threadStart = delegate {
			instance.pathfinding.FindPath (request, instance.FinishedProcessingPath);
		};
		threadStart.Invoke ();
	}

	public void FinishedProcessingPath(PathResult result) {
		lock (results) {
			results.Enqueue (result);
		}
	}



}

public struct PathResult {
	public Vector3[] path;
	public bool success;
	public Action<Vector3[], bool> callback;

	public PathResult (Vector3[] path, bool success, Action<Vector3[], bool> callback)
	{
		this.path = path;
		this.success = success;
		this.callback = callback;
	}

}

public struct PathRequest3D {
	public Vector3 pathStart;
	public Vector3 pathEnd;
	public Action<Vector3[], bool> callback;

	public PathRequest3D(Vector3 _start, Vector3 _end, Action<Vector3[], bool> _callback) {
		pathStart = _start;
		pathEnd = _end;
		callback = _callback;
	}

}