using UnityEngine;
using System.Collections;

//The info (fcost and index number in heap) for each position on a gridpoint.
public class Node3D : IHeapItem3D<Node3D> {
	
	public bool walkable;
	public Vector3 worldPosition;
	public int gridX;
	public int gridY;
	public int movementPenalty;

	public int gCost;
	public int hCost;
	public Node3D parent;
	int heapIndex;
	
	public Node3D(bool _walkable, Vector3 _worldPos, int _gridX, int _gridY, int _penalty) {
		walkable = _walkable;
		worldPosition = _worldPos;
		gridX = _gridX;
		gridY = _gridY;
		movementPenalty = _penalty;
	}

	public int fCost {
		get {
			return gCost + hCost;
		}
	}

	public int HeapIndex {
		get {
			return heapIndex;
		}
		set {
			heapIndex = value;
		}
	}

	public int CompareTo(Node3D nodeToCompare) {
		int compare = fCost.CompareTo(nodeToCompare.fCost);
		if (compare == 0) {
			compare = hCost.CompareTo(nodeToCompare.hCost);
		}
		return -compare;
	}
}