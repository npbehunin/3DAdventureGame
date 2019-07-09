using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetingSystem : MonoBehaviour
{
	public List<GameObject> EnemiesList = new List<GameObject>();
	//public List<GameObject> EnemiesInLOSList = new List<GameObject>();
	public GameObject[] enemies;
	public GameObject target;//, possibleTarget;
	public Vector3Value targetPosition;
	public BoolValue EnemyExists, PetIsAttacking, EnemyLOS;
	//public bool ;
	
	void Start () 
	{
		//If enemies instantiate in the scene, this should run again.
		enemies = GameObject.FindGameObjectsWithTag("Enemy");
		EnemiesList.AddRange(enemies);
		//EnemiesFound.initialBool = false;
		EnemyExists.initialBool = false;
	}
	
	void Update ()
	{
		if (!PetIsAttacking.initialBool)
		{
			target = GetClosestTarget(EnemiesList);
			//Debug.Log("Checking target");
		}
		
		if (target != null)
		{
			if (target.GetComponent<Enemy>() != null)
			{
				EnemyLOS.initialBool = target.GetComponent<Enemy>().EnemyLOS;
				target.GetComponent<Enemy>().IsTarget = true;
			}
			
			if (target.activeSelf) //If the target is active
			{
				EnemyExists.initialBool = true;
				targetPosition.initialPos = target.transform.position;
			}
			else
			{
				EnemyExists.initialBool = false; //Remove it from the list
				EnemiesList.Remove(target);
			}
		}
		else //If there are no targets
		{
			EnemyLOS.initialBool = false;
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

//TO DO:

//Ways to handle it:


//Known issues:
//1: No target is being found.
