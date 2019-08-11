using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GraphicsDirection : MonoBehaviour
{
	private Quaternion targetRotation;
	public float turnSpeed;
	public Vector3Value position, direction;
	
	void Start () 
	{
		
	}
	
	// Update is called once per frame
	void Update ()
	{
		targetRotation = Quaternion.LookRotation(direction.initialPos);
		transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed);
	}
}
