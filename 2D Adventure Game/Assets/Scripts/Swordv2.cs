using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Swordv2 : Weapon
{

	public bool SwordEquipped;
	public bool CanSwing;

	public int SwingNumber;
	public int MaxSwingNumber;

	public Animator animator;
	public PlayerMovement player;

	private Coroutine Swing;
	public Vector3 playerdirection;

	protected virtual void Start()
	{
		SwordEquipped = true;
		CanSwing = true;
		SwingNumber = 0;
		MaxSwingNumber = 3;
	}

	protected virtual void Update()
	{
		animator.SetInteger("SwordAttackState", SwingNumber);
		
		if (Input.GetMouseButtonDown(0))
		{
			if (SwordEquipped && CanSwing)
			{
				if (SwingNumber < MaxSwingNumber)
				{
					if (Swing != null)
					{
						StopCoroutine(Swing);
					}
					player.SwordMomentumScale = 0;
					Swing = StartCoroutine(SwordSwingTiming());
					SwingNumber += 1;
					playerdirection = new Vector3(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"), 0);
				}
			}
		}
	}

	private void ResetSwordAttack()
	{
		SwingNumber = 0;
		player.currentState = PlayerState.Idle;
	}

	private IEnumerator SwordSwingTiming()
	{
		player.currentState = PlayerState.Attack;
		CanSwing = false;
		yield return new WaitForSeconds(.1f);
		CanSwing = true;
		yield return new WaitForSeconds(.3f);
		ResetSwordAttack();
	}
}
