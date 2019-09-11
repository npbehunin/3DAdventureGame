using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CameraState
{
	FollowPlayer,
	TargetingMode,
	Conversation,
}

public class CameraFollowPlayer : MonoBehaviour
{

	public Transform player;
	public Transform target;
	public float smoothTime;
	public float maxSpeed;
	public Vector3 offset;
	public Vector3 desiredPosition;
	public Vector3Value ShakeDir;

	public bool TargetingModeActive;

	public CameraState CurrentCameraState;
	
	private Vector3 velocity = Vector3.zero;
	
	void Start()
	{
		CurrentCameraState = CameraState.FollowPlayer;
	}

	void Update()
	{
		switch (CurrentCameraState)
		{
			case CameraState.FollowPlayer:
			{
				desiredPosition = player.position + offset;
				//Vector3 smoothedPosition = Vector3.SmoothDamp(transform.position, desiredPosition, smoothSpeed);
				break;
			}
			case CameraState.TargetingMode:
			{
				desiredPosition = Vector3.Lerp(player.position, target.position, .5f) + offset; //Halfway between player and target.
				break;
			}
		}

		Vector3 smoothedPosition = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime, maxSpeed, Time.deltaTime);
		transform.position = smoothedPosition;

		//Temp target mode switching
		if (TargetingModeActive)
		{
			CurrentCameraState = CameraState.TargetingMode;
		}
		else
		{
			CurrentCameraState = CameraState.FollowPlayer;
		}
	}

	public void StartSwordShake()
	{
		StartCoroutine(SwordShake(.24f, ShakeDir.initialPos));
	}
	
	public IEnumerator SwordShake(float amount, Vector3 direction)
	{
		offset.x = direction.x * amount;
		offset.y = direction.y * amount;
		smoothTime = .1f;
		yield return new WaitForSeconds(.05f);
		offset.x = 0;
		offset.y = 0;
		smoothTime = .15f;
	}
}

//ISSUE
//Camera jumps without accelerating when ran normally in play mode, but it moves smoothly when moving per frame.
	