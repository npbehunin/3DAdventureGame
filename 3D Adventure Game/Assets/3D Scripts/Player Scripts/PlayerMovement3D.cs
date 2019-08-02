using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine;

public class PlayerMovement3D : MonoBehaviour
{
	public float accelSpeed, gravity = 20f, slopeForce, slopeForceRayLength;
	public PlayerState currentState;
	public Collider playerCollider;
	public CharacterController controller;
	public Vector3 position, fixedInputPos, rawInputPos;
	public PlayerAnimation playerAnim;
	//Player targeting here!
	public bool CanSetState, Invincible;
	public IntValue Health;
	public int CurrentHealth;
	public BoolValue EnemyCollision;
	
	private float horizontalspeed, verticalspeed;
	private float SwordMomentum, SwordMomentumSmooth, SwordMomentumPower;
	public FloatValue SwordMomentumScale, moveSpeed;
	
	public Vector3Value direction, PlayerTransform, TargetTransform;

	void Start()
	{
		playerCollider = gameObject.GetComponent<Collider>();
		controller = gameObject.GetComponent<CharacterController>();
	}

	void Update()
	{
		Debug.Log("Hi");
		
		//Controller move
		controller.Move(position * Time.deltaTime);
		
		//Slope check
		if (OnSlope() && (rawInputPos.x > 0 || rawInputPos.z > 0)) //&& if player is moving (ADD THIS!)
		{
			controller.Move(Vector3.down * playerCollider.bounds.size.y / 2 * slopeForce);
		}
		
		Vector3 pos = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
		
		fixedInputPos.x = calcFixedInput(fixedInputPos.x, pos.x);
		fixedInputPos.z = calcFixedInput(fixedInputPos.z, pos.z);
	
		//Position input
		position.x = fixedInputPos.x;
		position.z = fixedInputPos.z;
		
		//Is Grounded
		if (controller.isGrounded)
			position.y = 0;
		else
			position.y -= gravity * Time.deltaTime;

		//Raw input to check if any input is pressed
		rawInputPos = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));

		//MoveSpeed only applied to x and z.
		position.x *= moveSpeed.initialValue; 
		position.z *= moveSpeed.initialValue;
		
		//Gravity
		position.y -= gravity * Time.deltaTime; //DeltaTime applied here too for acceleration.
	}
	//Calculates a new player input with acceleration and deceleration.
	//Turning off "Snap" in the input settings works too, but this helps match controller movement.
	float calcFixedInput(float newInput, float input)
	{
		if (newInput != input)
			if (newInput < -1)
				newInput = -1;
			else if (newInput > 1)
				newInput = 1;
			else
				if (newInput < input)
					if (Math.Abs(newInput - input) < accelSpeed)
						newInput += Math.Abs(newInput - input);
					else
						newInput += accelSpeed;
				else if (newInput > input)
					if (Math.Abs(newInput - input) < accelSpeed)
						newInput -= Math.Abs(newInput - input);
					else
						newInput -= accelSpeed;
		return newInput;
	}

	//Check if the ground normal isn't up.
	private bool OnSlope()
	{
		RaycastHit hit;
		if (Physics.Raycast(transform.position, Vector3.down, out hit, playerCollider.bounds.size.y / 2 * slopeForceRayLength))
			if (hit.normal != Vector3.up)
				return true;
		return false;
	}
}

//NOTES
//When transferring over things from our old player movement script, the player seems to either not move at all or move
//very slowly. When we transfer again, check the game after each segment to see what might cause it.