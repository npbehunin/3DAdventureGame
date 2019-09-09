using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetingMode : MonoBehaviour
{
	private List<GameObject> EnemiesList = new List<GameObject>();
	private GameObject[] enemies;
	private GameObject target;
	//public Vector3Value targetPosition;
	//public BoolValue EnemyExists, PetIsAttacking, EnemyLOS;

	void Start () 
	{
		//If enemies instantiate in the scene, this should run again.
		enemies = GameObject.FindGameObjectsWithTag("Enemy");
		EnemiesList.AddRange(enemies);
		//EnemyExists.initialBool = false;
	}
	
	void Update ()
	{
		target = GetClosestTarget(EnemiesList);

		if (target != null)
		{
			if (target.GetComponent<Enemy>() != null)
			{
				target.GetComponent<Enemy>().IsTarget = true;
			}
			
			if (target.activeSelf) //If the target is active
			{
				//Do something
			}
			else
			{
				EnemiesList.Remove(target);
			}
		}
		else //If there are no targets
		{
			//EnemyLOS.initialBool = false;
		}
	}

	//*To fix this, pass through a list that contains ONLY ENEMIES IN LOS.
	GameObject GetClosestTarget (List<GameObject> objectList)
    {
        GameObject bestTarget = null;
        float closestDistanceSqr = Mathf.Infinity;
        Vector3 currentPosition = transform.position;
        foreach(GameObject potentialTarget in objectList)
        {
	        if (potentialTarget.GetComponent<Enemy>() != null) //*reee
	        {
		        if (potentialTarget.GetComponent<Enemy>().EnemyLOS) //*Probably fix this lol
		        {
			        Vector3 directionToTarget = potentialTarget.transform.position - currentPosition;
			        float dSqrToTarget = directionToTarget.sqrMagnitude;
			        if(dSqrToTarget < closestDistanceSqr)
			        {
				        closestDistanceSqr = dSqrToTarget;
				        bestTarget = potentialTarget;
			        }
		        }
		    }
	        else
	        {
		        Debug.Log("Enemy component not found");
	        }
	    }
	    return bestTarget;
    }
}
