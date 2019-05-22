using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwordMechanic : Weapon
{
	public bool SwordEquipped;
	public bool CanSwing;
	public bool CanDelayForCombo;

	public int ComboPhase;

	public int SwingNumber;
	public int AnimatorSwingNumber;
	public int MaxSwingNumber;

	public float SwingTime;
	public float SwordThrust;
	
	public Animator animator;
	public PlayerMovement player;

	private Coroutine SwingCoroutine;
	private Coroutine ClickCoroutine;
	private Coroutine DelayForClickCombo;

	public EquipWeapon equipweapon;

	protected override void Start()
	{
		base.Start();
		ComboPhase = 0;
		CanSwing = true;
		CanDelayForCombo = false;
		AnimatorSwingNumber = 0;
		MaxSwingNumber = 3;
		SwingTime = .15f;
	}

	protected override void Update()
	{
		base.Update();
		SwordEquipped = equipweapon.SwordEquipped;
		animator.SetInteger("SwordAttackState", AnimatorSwingNumber);
		if (Input.GetButtonDown("Fire1"))
		{
			if (SwordEquipped)
			{
				if (CanSwing)
				{
					SwordSwing();
				}

				if (ComboPhase == 1)
				{
					ComboPhase = 2;
				}
				
				if (ComboPhase == 3)
				{
					ComboPhase = 4;
				}
			}
		}
		
		//Knockback
		KnockbackPower = SwordThrust;
		if (PlayerObject != null)
		{
			PlayerObject.GetComponent<PlayerMovement>().GetSwordSwingDirection();
			PlayerDirection = PlayerObject.GetComponent<PlayerMovement>().test;
			if (PlayerDirection != Vector3.zero && PlayerDirection != null)
			{
				SwordThrust = 5;
			}
			else
			{
				SwordThrust = 0;
			}
		}
		else
		{
			SwordThrust = 0;
		}
	}

	//Enables the next sword swing
	void SwordSwing()
	{
		if (SwingNumber < MaxSwingNumber)
		{
			if (DelayForClickCombo!= null)
			{
				StopCoroutine(DelayForClickCombo);
			}
			if (SwingCoroutine != null)
			{
				StopCoroutine(SwingCoroutine);
			}

			if (ClickCoroutine != null)
			{
				StopCoroutine(ClickCoroutine);
			}

			ComboPhase = 0;
			CanSwing = false;
			player.SwordMomentumScale = 0;
			SwingCoroutine = StartCoroutine(SwordSwingTiming());
			SwingNumber += 1;
			AnimatorSwingNumber = SwingNumber;
			player.GetSwordSwingDirection();
		}
	}

	//If disabled, reset
	protected void OnDisable()
	{
		ResetSwordAttack();
	}

	//Reset everything
	private void ResetSwordAttack()
	{
		if (DelayForClickCombo != null)
		{
			StopCoroutine(DelayForClickCombo);
		}
		if (SwingCoroutine != null)
		{
			StopCoroutine(SwingCoroutine);
		}

		if (ClickCoroutine != null)
		{
			StopCoroutine(ClickCoroutine);
		}
		animator.SetInteger("SwordAttackState", 0);
		ComboPhase = 0;
		SwingNumber = 0;
		CanDelayForCombo = false;
		CanSwing = true;
		AnimatorSwingNumber = 0;
		player.currentState = PlayerState.Idle;
		SwordThrust = 0;
	}

	//Sword swing timing
	private IEnumerator SwordSwingTiming()
	{
		CanDelayForCombo = true;
		player.currentState = PlayerState.Attack;
		ClickCoroutine = StartCoroutine(MouseClickDelay());
		yield return new WaitForSeconds(SwingTime);
		if (ComboPhase == 2)
		{
			SwordSwing();
		}
		else
		{
			CanSwing = true;
		}

		if (SwingNumber >= MaxSwingNumber && CanDelayForCombo)
		{
			CanDelayForCombo = false;
			DelayForClickCombo = StartCoroutine(DelayForNextCombo());
		}
		
		if (SwingNumber < MaxSwingNumber)
		{
			yield return new WaitForSeconds(.12f);
			AnimatorSwingNumber = 0;
			player.currentState = PlayerState.Idle;
			yield return new WaitForSeconds(.15f);
			ResetSwordAttack();
		}
		else
		{
			yield return new WaitForSeconds(.2f);
			AnimatorSwingNumber = 0;
			player.currentState = PlayerState.Idle;
			yield return new WaitForSeconds(.2f);
			if (ComboPhase == 4)
			{
				ComboPhase = 0;
				SwingNumber = 0;
				SwordSwing();
			}
			else
			{
				ResetSwordAttack();
			}
		}
	}

	//Handles the delay before the player can click to enable the next combo.
	private IEnumerator DelayForNextCombo()
	{
		yield return new WaitForSeconds(.35f);
		ComboPhase = 3;
	}

	private IEnumerator MouseClickDelay()
	{
		yield return new WaitForSeconds(SwingTime*.25f);
		ComboPhase = 1;
	}
}