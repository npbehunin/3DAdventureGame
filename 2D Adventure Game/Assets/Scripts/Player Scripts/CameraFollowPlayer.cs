using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollowPlayer : MonoBehaviour
{

	public Transform target;

	public float smoothSpeed;
	public Vector3 offset, desiredPosition;
	public Vector3Value ShakeDir;
	private Vector3 velocity = Vector3.zero;

	public bool Knocked;

	void Start()
	{
		Knocked = false;
	}

	void Update()
	{
		desiredPosition = target.position + offset;
		//Vector3 smoothedPosition = Vector3.SmoothDamp(transform.position, desiredPosition, smoothSpeed);
		Vector3 smoothedPosition = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothSpeed);
		transform.position = smoothedPosition;
	}

	public void StartSwordShake()
	{
		StartCoroutine(SwordShake(.5f, ShakeDir.initialPos));
	}

	//Adjust this to take into account the player's position so it feels consistent even if the player is moving.
	public IEnumerator SwordShake(float amount, Vector3 direction)
	{
		offset.x = direction.x * amount;
		offset.y = direction.y * amount;
		smoothSpeed = .2f;
		yield return new WaitForSeconds(.15f);
		offset.x = 0;
		offset.y = 0;
		smoothSpeed = .15f;
	}

	//IEnumerator CameraShake(float amount)
	//{
		
	//}
}
	