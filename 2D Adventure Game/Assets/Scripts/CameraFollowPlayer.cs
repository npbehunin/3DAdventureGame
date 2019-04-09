using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollowPlayer : MonoBehaviour
{

	public Transform target;
	private Vector3 newPos;

	//private Vector2 position;

	void FixedUpdate()
	{
		newPos.Set(target.transform.position.x, target.transform.position.y, -10);
		transform.position = newPos;
	}
}
	