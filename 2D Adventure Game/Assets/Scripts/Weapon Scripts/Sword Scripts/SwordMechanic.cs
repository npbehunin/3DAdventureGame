using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwordMechanic : Weapon
{

	public bool SwordEquipped;
	public bool CanSwing;

	public int SwingNumber;
	public int AnimatorSwingNumber;
	public int MaxSwingNumber;

	public float SwingTime;
	public float SwingTimeDelay;
	
	public Animator animator;
	public PlayerMovement player;

	private Coroutine SwingCoroutine;
	private Coroutine ClickCoroutine;
	public Vector3 playerdirection;

	public EquipWeapon equipweapon;

	protected virtual void Start()
	{
		CanSwing = true;
		SwingNumber = 0;
		MaxSwingNumber = 3;
		SwingTime = .1f;
		SwingTimeDelay = .3f;
		AnimatorSwingNumber = 0;
	}

	protected virtual void Update()
	{
		SwordEquipped = equipweapon.SwordEquipped;
		animator.SetInteger("SwordAttackState", SwingNumber);
		
		if (Input.GetMouseButtonDown(0))
		{
			if (SwordEquipped && CanSwing)
			{
				if (SwingNumber < MaxSwingNumber)
				{
					SwordSwing();
					CanSwing = false;
				}
			}
		}
		//if (AnimatorSwingNumber > 3) //Delete this if we ever add more than 3 sword swing animations.
		//{
		//	AnimatorSwingNumber = 0;
		//}
	}

	void SwordSwing()
	{
		if (SwingCoroutine != null)
		{
			StopCoroutine(SwingCoroutine);
		}
		player.SwordMomentumScale = 0;
		SwingCoroutine = StartCoroutine(SwordSwingTiming());
		SwingNumber += 1;
		player.GetSwordSwingDirection();
		//AnimatorSwingNumber += 1;
	}

	protected void OnDisable()
	{
		ResetSwordAttack();
	}

	private void ResetSwordAttack()
	{
		SwingNumber = 0;
		CanSwing = true;
		player.currentState = PlayerState.Idle;
	}

	private IEnumerator SwordSwingTiming()
	{
		player.currentState = PlayerState.Attack;
		yield return new WaitForSeconds(SwingTime);
		CanSwing = true;
		yield return new WaitForSeconds(SwingTimeDelay);
		ResetSwordAttack();
	}
}