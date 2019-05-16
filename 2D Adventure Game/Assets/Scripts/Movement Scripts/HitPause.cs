using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitPause : MonoBehaviour {

	//Briefly pauses the object it's attached to.
	//Needs to pause the animation, set any rigidbodies to kinematic, and prevent any input.
	public Rigidbody2D rb;
	public Animator animator;
	public bool HitPaused;
	
	void Start ()
	{
		rb = gameObject.GetComponent<Rigidbody2D>();
		//animator = gameObject.GetComponent<Animator>();
		HitPaused = false;
	}
	
	
}
