using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetingSystem : MonoBehaviour
{
	public GameObject[] enemies;
	public GameObject target;
	public Vector3Value targetPosition;
	public BoolValue PetCanFollowTarget;
	
	void Start () 
	{
		//If enemies instantiate in the scene, this should run again.
		enemies = GameObject.FindGameObjectsWithTag("Enemy");
	}
	
	void Update ()
	{
		if (!PetCanFollowTarget.initialBool)
		{
			target = GetClosestTarget(enemies);
		}

		if (target != null)
		{
			targetPosition.initialPos = target.transform.position;
		}
	}

	void FindClosestTarget()
	{
		//targetTransform.initialTransform = GetClosestTarget(enemies).transform;
	}

	GameObject GetClosestTarget (GameObject[] objectList)
    {
        GameObject bestTarget = null;
        float closestDistanceSqr = Mathf.Infinity;
        Vector3 currentPosition = transform.position;
        foreach(GameObject potentialTarget in objectList)
        {
            Vector3 directionToTarget = potentialTarget.transform.position - currentPosition;
            float dSqrToTarget = directionToTarget.sqrMagnitude;
            if(dSqrToTarget < closestDistanceSqr)
            {
                closestDistanceSqr = dSqrToTarget;
                bestTarget = potentialTarget;
            }
        }
        return bestTarget;
    }
	
}
