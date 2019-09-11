using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GridNew : MonoBehaviour {

	public bool displayGridGizmos;
	public LayerMask unwalkableMask, rayMask;
	public Vector2 gridWorldSize;
	public float nodeRadius;
	NodeNew[,] grid;

	float nodeDiameter;
	int gridSizeX, gridSizeY;

	void Awake() {
		nodeDiameter = nodeRadius*2;
		gridSizeX = Mathf.RoundToInt(gridWorldSize.x/nodeDiameter);
		gridSizeY = Mathf.RoundToInt(gridWorldSize.y/nodeDiameter);
		CreateGrid();
	}

	public int MaxSize {
		get {
			return gridSizeX * gridSizeY;
		}
	}

	void CreateGrid() {
		grid = new NodeNew[gridSizeX,gridSizeY];
		Vector3 worldBottomLeft = transform.position - Vector3.right * gridWorldSize.x/2 - Vector3.forward * gridWorldSize.y/2; //*** Vector3 forward to up

		for (int x = 0; x < gridSizeX; x ++) {
			for (int y = 0; y < gridSizeY; y ++) {
				Vector3 worldPoint = worldBottomLeft + Vector3.right * (x * nodeDiameter + nodeRadius) + Vector3.forward * (y * nodeDiameter + nodeRadius); // ***
				RaycastHit hit;
				bool walkable = new bool();
				if (Physics.Raycast(worldPoint, Vector3.down, out hit, rayMask)) //Only checks raycast on these layers
					walkable = !(Physics.CheckSphere(hit.point, nodeRadius, unwalkableMask));
				//walkable = !(Physics.CheckSphere(worldPoint,nodeRadius,unwalkableMask)); //Default
				grid[x,y] = new NodeNew(walkable,worldPoint, x,y);
			}
		}
	}

	public List<NodeNew> GetNeighbours(NodeNew node) {
		List<NodeNew> neighbours = new List<NodeNew>();

		for (int x = -1; x <= 1; x++) {
			for (int y = -1; y <= 1; y++) {
				if (x == 0 && y == 0)
					continue;
				//if (x != 0 && y != 0) //Ignores diagonal neighbors
				//	continue;

				int checkX = node.gridX + x;
				int checkY = node.gridY + y;

				if (checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY) {
					neighbours.Add(grid[checkX,checkY]);
				}
			}
		}

		return neighbours;
	}
	
	public NodeNew NodeFromWorldPoint(Vector3 _worldPosition)
	{
		float posX = ((_worldPosition.x - transform.position.x) + gridWorldSize.x * 0.5f) / nodeDiameter;
		float posY = ((_worldPosition.z - transform.position.z) + gridWorldSize.y * 0.5f) / nodeDiameter;

		posX = Mathf.Clamp(posX, 0, gridWorldSize.x - 1);
		posY = Mathf.Clamp(posY, 0, gridWorldSize.y - 1);

		int x = Mathf.FloorToInt(posX);
		int y = Mathf.FloorToInt(posY);

		return grid[x, y];
	}

	void OnDrawGizmos() {
		Gizmos.DrawWireCube(transform.position,new Vector3(gridWorldSize.x,1,gridWorldSize.y)); //*** swap positions for y
		if (grid != null && displayGridGizmos) {
			foreach (NodeNew n in grid) {
				Gizmos.color = (n.walkable)?Color.white:Color.red;
				Gizmos.DrawCube(n.worldPosition, Vector3.one * (nodeDiameter-.1f));
			}
		}
	}
}