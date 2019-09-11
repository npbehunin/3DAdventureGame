using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetingMode : MonoBehaviour
{
	public String TargetTagName;
	public float MaxDistance;
	public LayerMask WallLayerMask;
	public Vector3 target;
	public bool TargetFound;
	
	private List<Vector3> targetList = new List<Vector3>();
	private List<GameObject> potentialTargetList = new List<GameObject>();
	private GameObject[] objectList;
	

	void Start()
	{
		FindObjects();
	}

	//Find objects in the scene
	void FindObjects()
	{
		//If enemies instantiate in the scene, this should run again.
		objectList = GameObject.FindGameObjectsWithTag(TargetTagName);

		for (int i = 0; i < objectList.Length - 1; i++)
		{
			potentialTargetList.Add(objectList[i]);
		}
	}

	//Remove object from the potential target list. (Ex: When an enemy dies)
	void RemoveObject(GameObject obj)
	{
		potentialTargetList.Remove(obj);
	}

	void Update ()
	{
		//*Check line of sight to the target and add it to the target list.
		foreach (GameObject obj in potentialTargetList)
		{
			Vector3 objPos = obj.transform.position;
			float distance = (objPos - transform.position).sqrMagnitude;
			if (Math.Abs(distance) <= (Mathf.Pow(MaxDistance, 2)) && (!Physics2D.Linecast(transform.position, objPos, WallLayerMask)))
			{
				if (!targetList.Contains(objPos))
				{
					targetList.Add(objPos);
				}
			}
			else
			{
				if (targetList.Contains(objPos))
				{
					targetList.Remove(objPos);
				}
			}
		}

		//If the targetList contains at least 1 item, get the closest target.
		if (targetList.Count > 0)
		{
			target = GetClosestTarget(targetList);
			TargetFound = true;
		}
		else
		{
			TargetFound = false;
		}
	}
	
	//Find the closest target.
	Vector3 GetClosestTarget (List<Vector3> targetList)
	{
		Vector3 bestTarget = Vector3.zero;
		float closestDistanceSqr = Mathf.Infinity;
		Vector3 currentPosition = transform.position;
		foreach(Vector3 obj in targetList)
		{
			Vector3 directionToTarget = obj - currentPosition;
			float dSqrToTarget = directionToTarget.sqrMagnitude;
			if(dSqrToTarget < closestDistanceSqr)
			{
				closestDistanceSqr = dSqrToTarget;
				bestTarget = obj;
			}
		}
		return bestTarget;
	}
}

//NOTES
//*Once we implement line of sight checks within the enemy (to detect if it should follow the player or not), the enemy
//script could add itself to this targetList instead of running a line of sight check here.