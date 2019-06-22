using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetingSystem : MonoBehaviour
{
	public List<GameObject> EnemiesList = new List<GameObject>();
	public GameObject[] enemies;
	public GameObject target;
	public Vector3Value targetPosition;
	public BoolValue EnemyExists;
	public bool NoEnemiesFound;
	
	void Start () 
	{
		//If enemies instantiate in the scene, this should run again.
		enemies = GameObject.FindGameObjectsWithTag("Enemy");
		EnemiesList.AddRange(enemies);
		NoEnemiesFound = false;
	}
	
	void Update ()
	{
		if (!EnemyExists.initialBool && !NoEnemiesFound)
		{
			target = GetClosestTarget(EnemiesList);
			Debug.Log("Checking target");
		}

		if (target != null)
		{
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
			//Debug.Log("No enemies found!");
			NoEnemiesFound = true;
		}
	}

	GameObject GetClosestTarget (List<GameObject> objectList)
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
