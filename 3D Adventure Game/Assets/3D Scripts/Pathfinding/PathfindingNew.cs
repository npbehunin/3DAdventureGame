using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class PathfindingNew : MonoBehaviour {
	
	PathRequestManagerNew requestManager;
	GridNew grid; //Change if using 3D
	
	void Awake() {
		requestManager = GetComponent<PathRequestManagerNew>();
		grid = GetComponent<GridNew>();
	}
	
	public void StartFindPath(Vector3 startPos, Vector3 targetPos) { //Start finding the path
		StartCoroutine(FindPath(startPos,targetPos));
	}
	
	IEnumerator FindPath(Vector3 startPos, Vector3 targetPos) { //Calculate the path

		Vector3[] waypoints = new Vector3[0];
		bool pathSuccess = false;
		
		NodeNew startNode = grid.NodeFromWorldPoint(startPos);
		NodeNew targetNode = grid.NodeFromWorldPoint(targetPos);

		//if (startNode.walkable && targetNode.walkable) {
		if (startNode.walkable && targetNode.walkable) { //Removed startNode (under the assumption objects will never be inside a wall, which is unwalkable.)
			HeapNew<NodeNew> openSet = new HeapNew<NodeNew>(grid.MaxSize);
			HashSet<NodeNew> closedSet = new HashSet<NodeNew>();
			openSet.Add(startNode);
			
			while (openSet.Count > 0) {
				NodeNew currentNode = openSet.RemoveFirst();
				closedSet.Add(currentNode);
				
				if (currentNode == targetNode) {
					pathSuccess = true;
					break;
				}
				
				foreach (NodeNew neighbour in grid.GetNeighbours(currentNode)) {
					if (!neighbour.walkable || closedSet.Contains(neighbour)) {
						continue;
					}
					
					int newMovementCostToNeighbour = currentNode.gCost + GetDistance(currentNode, neighbour);
					if (newMovementCostToNeighbour < neighbour.gCost || !openSet.Contains(neighbour)) {
						neighbour.gCost = newMovementCostToNeighbour;
						neighbour.hCost = GetDistance(neighbour, targetNode);
						neighbour.parent = currentNode;
						
						if (!openSet.Contains(neighbour))
							openSet.Add(neighbour);
						else
						{
							openSet.UpdateItem(neighbour);
						}
					}
				}
			}
		}
		yield return null;
		if (pathSuccess) {
			waypoints = RetracePath(startNode,targetNode);
		}
		requestManager.FinishedProcessingPath(waypoints,pathSuccess);
	}
	
	Vector3[] RetracePath(NodeNew startNode, NodeNew endNode) {
		List<NodeNew> path = new List<NodeNew>();
		NodeNew currentNode = endNode;

		while (currentNode != startNode) {
			path.Add(currentNode);
			currentNode = currentNode.parent;
		}
		Vector3[] waypoints = SimplifyPath(path, startNode);
		Array.Reverse(waypoints);
		return waypoints;
	}
	
	Vector3[] SimplifyPath(List<NodeNew> path, NodeNew startNode) {
		List<Vector3> waypoints = new List<Vector3>();
		Vector2 directionOld = Vector2.zero;
		for (int i = 1; i < path.Count; i ++) {
			Vector2 directionNew = new Vector2(path[i-1].gridX - path[i].gridX,path[i-1].gridY - path[i].gridY); //Gives us the direction from the previous node to the NEW one
			if (directionNew != directionOld) { //IF THE DIRECTION ISN'T THE SAME, IT ADDS IT
				waypoints.Add(path[i-1].worldPosition);
			}
			directionOld = directionNew;

			if (i == path.Count - 1 && directionOld != new Vector2(path[i].gridX, path[i].gridY) - new Vector2(startNode.gridX, startNode.gridY)) //Adds a neighbor node at the start of the path if needed
				waypoints.Add(path[path.Count-1].worldPosition);
		}
		
		//waypoints.Add(path[path.Count-1].worldPosition); //This works! However, it will always add another node when the path refreshes.
		return waypoints.ToArray();
	}
	
	int GetDistance(NodeNew nodeA, NodeNew nodeB) {
		int dstX = Mathf.Abs(nodeA.gridX - nodeB.gridX);
		int dstY = Mathf.Abs(nodeA.gridY - nodeB.gridY);
		
		if (dstX > dstY)
			return 14*dstY + 10* (dstX-dstY);
		return 14*dstX + 10 * (dstY-dstX);
	}
}

//NOTES
//We added a check in SimplifyPath to make sure a node is added at the beginning of the path if it's a neighboring node
//with the same direction as directionOld.