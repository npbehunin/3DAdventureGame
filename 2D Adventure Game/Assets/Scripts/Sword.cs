using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sword : MonoBehaviour
{
	//Sword Checks
	public bool SwordEquipped;
	public bool CanSwing1;
	public bool CanSwing2;
	public bool CanSwing3;
	
	public int SwordPhase;
	public Animator animator;

	public PlayerRigidbodyMovementExperiment player;

	void Start()
	{
		CanSwing1 = true;
		CanSwing2 = false;
		CanSwing3 = false;
	}

	void Update()
	{
		//Sword
		if (SwordEquipped)
		{
			if (Input.GetMouseButtonDown(0))
			{
				if (CanSwing1)
				{
					CanSwing1 = false;
					SwordPhase = 1;
					player.currentState = PlayerState.Attack;
					animator.SetBool("Running", false);
					animator.SetInteger("SwordAttackState", 1);
					StartCoroutine(SwordAttack1());
				}

				if (CanSwing2)
				{
					CanSwing2 = false;
					SwordPhase = 2;
					animator.SetInteger("SwordAttackState", 2);
					StartCoroutine(SwordAttack2());
				}

				if (CanSwing3)
				{
					SwordPhase = 3;
					animator.SetInteger("SwordAttackState", 3);
					StartCoroutine(SwordAttack3());
				}
			}
		}
	}
	
	private void ResetSwordAttack()
	{
		CanSwing2 = false;
		CanSwing3 = false;
		CanSwing1 = true;
		animator.SetInteger("SwordAttackState", 0);
		player.currentState = PlayerState.Idle;
	}
	
	private IEnumerator SwordAttack1()
	{
		yield return new WaitForSeconds(.1f);
		CanSwing2 = true;
		yield return new WaitForSeconds(.3f);
		if (SwordPhase == 2 || SwordPhase == 3)
		{
			CanSwing2 = false;
		}
		else
		{
			ResetSwordAttack();
		}
	}
	
	private IEnumerator SwordAttack2()
	{
		yield return new WaitForSeconds(.1f);
		CanSwing3 = true;
		yield return new WaitForSeconds(.3f);
		if (SwordPhase == 3)
		{
			CanSwing3 = false;
		}
		else
		{
			ResetSwordAttack();
		}
	}
	
	private IEnumerator SwordAttack3()
	{
		yield return new WaitForSeconds(.3f);
		ResetSwordAttack();
	}
}