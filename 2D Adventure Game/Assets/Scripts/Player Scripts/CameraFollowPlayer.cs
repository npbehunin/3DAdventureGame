using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollowPlayer : MonoBehaviour
{

	public Transform target;

	public float smoothSpeed;
	public Vector3 offset, desiredPosition;
	private Vector3 velocity = Vector3.zero;

	void Update()
	{
		desiredPosition = target.position + offset;
		//Vector3 smoothedPosition = Vector3.SmoothDamp(transform.position, desiredPosition, smoothSpeed);
		Vector3 smoothedPosition = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothSpeed);
		transform.position = smoothedPosition;
		//Debug.Log(offset.x);
	}

	public IEnumerator SwordShake(float amount)
	{
		//Adjust these settings for a snappier camera. Right now it's kinda barfy.
		offset.x = .5f;
		smoothSpeed = .5f;
		yield return new WaitForSeconds(.15f);
		offset.x = 0;
		smoothSpeed = .15f;
	}

	//IEnumerator CameraShake(float amount)
	//{
		
	//}
}
	