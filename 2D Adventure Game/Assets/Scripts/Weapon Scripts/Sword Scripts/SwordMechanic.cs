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
	
	public Animator animator;
	public PlayerMovement player;

	private Coroutine SwingCoroutine;
	private Coroutine ClickCoroutine;
	private Coroutine DelayForClickCombo;

	public EquipWeapon equipweapon;

	protected virtual void Start()
	{
		ComboPhase = 0;
		CanSwing = true;
		CanDelayForCombo = false;
		AnimatorSwingNumber = 0;
		MaxSwingNumber = 3;
		SwingTime = .15f;
	}

	protected virtual void Update()
	{
		SwordEquipped = equipweapon.SwordEquipped;
		animator.SetInteger("SwordAttackState", AnimatorSwingNumber);
		//Debug.Log(CanClickForCombo);
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

	void SwordSwing()
	{
		//Debug.Log("Ran the sword swing function");
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

	protected void OnDisable()
	{
		ResetSwordAttack();
	}

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
		//Debug.Log("Resetting");
		ComboPhase = 0;
		SwingNumber = 0;
		CanDelayForCombo = false;
		CanSwing = true;
		AnimatorSwingNumber = 0;
		player.currentState = PlayerState.Idle;
	}

	private IEnumerator SwordSwingTiming()
	{
		//Debug.Log("Ran");
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

	private IEnumerator DelayForNextCombo()
	{
		//Handles the delay before the player can click to enable the next combo.
		yield return new WaitForSeconds(.35f);
		ComboPhase = 3;
	}

	private IEnumerator MouseClickDelay()
	{
		yield return new WaitForSeconds(SwingTime*.25f);
		ComboPhase = 1;
	}
}

//TO DO: 5/9/19

//#1 Figure out a smoother transition between end of sword swing and going back to run. Feels very abrubt and fast right now.
//(The player slows down for each sword swing, but then snaps back immediately to running speed after it's over.)

