using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerRigidbodyMovement : MonoBehaviour
{
	public bool SwordEquipped;
	public bool CanSwing;
	public int SwordPhase;

	public PlayerState currentState;
	public Rigidbody2D rb;
	public Animator animator;

	private float horizontalspeed;
	private float verticalspeed;
	public float MoveSpeed;
	
	public Vector3 position;

	void Start ()
	{
		SwordEquipped = true;
		currentState = PlayerState.Idle;
		rb = GetComponent<Rigidbody2D>();
		animator = gameObject.GetComponent<Animator>();
	}

	void FixedUpdate()
	{
		if (currentState == PlayerState.Run)
		{
			rb.MovePosition(transform.position + position * Time.deltaTime);
		}
	}

	void Update()
	{
		Debug.Log(currentState);
		position.Set((MoveSpeed * Input.GetAxisRaw("Horizontal")), (MoveSpeed * Input.GetAxisRaw("Vertical")), 0);
		horizontalspeed = position.x;
		verticalspeed = position.y;
		
		//Run Animator
		if (position != Vector3.zero)
		{
			if (currentState == PlayerState.Idle || currentState == PlayerState.Run || currentState != PlayerState.Attack)
			{
				currentState = PlayerState.Run;
				animator.SetBool("Running", true);
				animator.SetFloat("SpeedX", horizontalspeed);
				animator.SetFloat("SpeedY", verticalspeed);
			}
		}

		//Idle
		if (currentState != PlayerState.Attack);
         	{
			    if (position == Vector3.zero)
			        {
				        //currentState = PlayerState.Idle;
				        animator.SetBool("Running", false);
			        }
		         }
		
		//SWORD ATTACKING
		if (SwordEquipped)
		{
			if (Input.GetMouseButtonDown(0))
			{
				if (SwordPhase == 0)
				{
					StartCoroutine(SwordAttack());
				}

				if (SwordPhase == 1)
				{
					CanSwing = true;

				}
			}
		}
		Debug.Log(CanSwing);
	}

	private IEnumerator SwordAttack()
	{
		currentState = PlayerState.Attack;
		animator.SetBool("Running", false);
		animator.SetInteger("SwordAttackState", 1);
		CanSwing = false;
		yield return new WaitForSeconds(.1f);
		SwordPhase = 1;
		yield return new WaitForSeconds(.2f);
		if (CanSwing == true)
		{
			animator.SetInteger("SwordAttackState", 2);
			CanSwing = false;
			yield return new WaitForSeconds(.3f);
			if (CanSwing == true)
			{
				animator.SetInteger("SwordAttackState", 3);
				yield return new WaitForSeconds(.3f);
				SwordPhase = 0;
				CanSwing = false;
				animator.SetInteger("SwordAttackState", 0);
				currentState = PlayerState.Idle;
			}
			else
			{
				{
					SwordPhase = 0;
					animator.SetInteger("SwordAttackState", 0);
					currentState = PlayerState.Idle;
				}
			}
		}
		else
		{
			{
				SwordPhase = 0;
				animator.SetInteger("SwordAttackState", 0);
				currentState = PlayerState.Idle;
			}
		}
	}
}
