using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System;

public class Pathfinding3D : MonoBehaviour {

	Grid3D grid;
	
	void Awake() {
		grid = GetComponent<Grid3D>();
	}
	

	public void FindPath(PathRequest3D request, Action<PathResult> callback) {
		
		Stopwatch sw = new Stopwatch();
		sw.Start();
		
		Vector3[] waypoints = new Vector3[0];
		bool pathSuccess = false;
		
		Node3D startNode = grid.NodeFromWorldPoint(request.pathStart);
		Node3D targetNode = grid.NodeFromWorldPoint(request.pathEnd);
		startNode.parent = startNode;
		
		
		if (startNode.walkable && targetNode.walkable) {
			Heap3D<Node3D> openSet = new Heap3D<Node3D>(grid.MaxSize);
			HashSet<Node3D> closedSet = new HashSet<Node3D>();
			openSet.Add(startNode);
			
			while (openSet.Count > 0) {
				Node3D currentNode = openSet.RemoveFirst();
				closedSet.Add(currentNode);
				
				if (currentNode == targetNode) {
					sw.Stop();
					//print ("Path found: " + sw.ElapsedMilliseconds + " ms");
					pathSuccess = true;
					break;
				}
				
				foreach (Node3D neighbour in grid.GetNeighbours(currentNode)) {
					if (!neighbour.walkable || closedSet.Contains(neighbour)) {
						continue;
					}
					
					int newMovementCostToNeighbour = currentNode.gCost + GetDistance(currentNode, neighbour) + neighbour.movementPenalty;
					if (newMovementCostToNeighbour < neighbour.gCost || !openSet.Contains(neighbour)) {
						neighbour.gCost = newMovementCostToNeighbour;
						neighbour.hCost = GetDistance(neighbour, targetNode);
						neighbour.parent = currentNode;
						
						if (!openSet.Contains(neighbour))
							openSet.Add(neighbour);
						else 
							openSet.UpdateItem(neighbour);
					}
				}
			}
		}
		if (pathSuccess) {
			waypoints = RetracePath(startNode,targetNode);
			pathSuccess = waypoints.Length > 0;
		}
		callback (new PathResult (waypoints, pathSuccess, request.callback));
		
	}
	
	Vector3[] RetracePath(Node3D startNode, Node3D endNode) {
		List<Node3D> path = new List<Node3D>();
		Node3D currentNode = endNode;
		
		while (currentNode != startNode) {
			path.Add(currentNode);
			currentNode = currentNode.parent;
		}
		Vector3[] waypoints = SimplifyPath(path);
		Array.Reverse(waypoints);
		return waypoints;
	}
	
	Vector3[] SimplifyPath(List<Node3D> path) {
		List<Vector3> waypoints = new List<Vector3>();
		Vector2 directionOld = Vector2.zero;
		
		for (int i = 1; i < path.Count; i ++) {
			Vector2 directionNew = new Vector2(path[i-1].gridX - path[i].gridX,path[i-1].gridY - path[i].gridY);
			if (directionNew != directionOld) {
				waypoints.Add(path[i - 1].worldPosition); //Changed [i] to [i-1] to include target
			}
			directionOld = directionNew;
		}
		return waypoints.ToArray();
	}
	
	//Distance between nodes to help calculate gcost and hcost. Diagonal is 14, other is 10
	int GetDistance(Node3D nodeA, Node3D nodeB) {
		int dstX = Mathf.Abs(nodeA.gridX - nodeB.gridX);
		int dstY = Mathf.Abs(nodeA.gridY - nodeB.gridY);
		
		if (dstX > dstY)
			return 14*dstY + 10* (dstX-dstY);
		return 14*dstX + 10 * (dstY-dstX);
	}
}