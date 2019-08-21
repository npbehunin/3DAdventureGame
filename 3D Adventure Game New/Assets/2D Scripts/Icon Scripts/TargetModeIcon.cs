using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetModeIcon : MonoBehaviour {

	public Vector3Value targetPosition;
	
	void Update () 
	{
		transform.position = new Vector3(targetPosition.initialPos.x, targetPosition.initialPos.y + 1, 0);
	}
}
