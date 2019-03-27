using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollowPlayer : MonoBehaviour
{

	public Transform target;

	//private Vector2 position;

	void Update()
	{
		transform.position = target.transform.position.x + target.transform.position.y;
	}
}
	