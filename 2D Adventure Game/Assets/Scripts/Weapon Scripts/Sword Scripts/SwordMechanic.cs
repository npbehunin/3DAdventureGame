using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwordMechanic : Weapon
{
	public bool SwordEquipped, CanSwing, CanDelayForCombo;

	public int ComboPhase, SwingNumber, AnimatorSwingNumber, MaxSwingNumber;

	public float SwingTime;
	public FloatValue SwordMomentumScale;

	public PlayerAnimation playerAnim;
	public PlayerMovement player;

	private Coroutine SwingCoroutine, ClickCoroutine, DelayForClickCombo;

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
		playerAnim.SetAnimSwordAttack(AnimatorSwingNumber);
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
	}

	//Enables the next sword swing
	void SwordSwing()
	{
		Enemy.CanCollide = true; //Enemy can collide
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
			SwordMomentumScale.initialValue = 0;
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
		playerAnim.SetAnimSwordAttack(0);
		ComboPhase = 0;
		SwingNumber = 0;
		CanDelayForCombo = false;
		CanSwing = true;
		AnimatorSwingNumber = 0;
		Enemy.CanCollide = true; //Enemy can collide
		player.currentState = PlayerState.Idle;
	}

	//Sword swing timing
	private IEnumerator SwordSwingTiming()
	{
		CanDelayForCombo = true;
		player.currentState = PlayerState.Attack;
		ClickCoroutine = StartCoroutine(MouseClickDelay());
		yield return CustomTimer.Timer(SwingTime);
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
			yield return CustomTimer.Timer(.12f);
			AnimatorSwingNumber = 0;
			player.currentState = PlayerState.Idle;
			yield return CustomTimer.Timer(.15f);
			ResetSwordAttack();
		}
		else
		{
			yield return CustomTimer.Timer(.2f);
			AnimatorSwingNumber = 0;
			player.currentState = PlayerState.Idle;
			yield return CustomTimer.Timer(.2f);
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

	private IEnumerator DelayForNextCombo()
	{
		yield return CustomTimer.Timer(.35f);
		ComboPhase = 3;
	}

	private IEnumerator MouseClickDelay()
	{
		yield return CustomTimer.Timer(SwingTime*.25f);
		ComboPhase = 1;
	}
}