using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwordMechanic : Weapon
{
	public bool SwordEquipped;
	public bool CanSwing;
	public bool CanClick;
	public bool Clicked;
	public bool CanClickForCombo;
	public bool ClickedForCombo;
	public bool ResetAttack;

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
		ResetAttack = false;
		CanClickForCombo = false;
		CanSwing = true;
		CanClick = false;
		AnimatorSwingNumber = 0;
		MaxSwingNumber = 3;
		SwingTime = .15f;
	}

	protected virtual void Update()
	{
		SwordEquipped = equipweapon.SwordEquipped;
		animator.SetInteger("SwordAttackState", AnimatorSwingNumber);
		//Debug.Log(CanClickForCombo);
		if (Input.GetMouseButtonDown(0))
		{
			if (SwordEquipped)
			{
				if (CanSwing)
				{
					SwordSwing();
				}

				if (CanClick)
				{
					CanClick = false;
					Clicked = true;
				}

				if (CanClickForCombo)
				{
					CanClickForCombo = false;
					ClickedForCombo = true;
				}
			}
		}

		if (ResetAttack && SwingNumber == 0)
		{
			ResetAttack = false;
			SwordSwing();
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
		Debug.Log("Resetting");
		SwingNumber = 0;
		CanClickForCombo = false;
		ResetAttack = false;
		CanSwing = true;
		CanClick = false;
		Clicked = false;
		AnimatorSwingNumber = 0;
		player.currentState = PlayerState.Idle;
	}

	private IEnumerator SwordSwingTiming()
	{
		player.currentState = PlayerState.Attack;
		ClickCoroutine = StartCoroutine(MouseClickDelay());
		yield return new WaitForSeconds(SwingTime);
		if (Clicked)
		{
			Clicked = false;
			SwordSwing();
		}
		else
		{
			CanClick = false;
			CanSwing = true;
		}

		if (SwingNumber >= MaxSwingNumber)
		{
			DelayForClickCombo = StartCoroutine(DelayForNextCombo());
		}

		yield return new WaitForSeconds(.3f);
		if (SwingNumber < MaxSwingNumber)
		{
			AnimatorSwingNumber = 0;
			player.currentState = PlayerState.Idle;
			yield return new WaitForSeconds(.2f);
			ResetSwordAttack();
		}
		else
		{
			if (ClickedForCombo)
			{
				ClickedForCombo = false;
				ResetAttack = true;
				SwingNumber = 0;
			}
			else
			{
				ResetSwordAttack();
			}
		}
	}

	private IEnumerator DelayForNextCombo()
	{
		yield return new WaitForSeconds(.1f);
		CanClickForCombo = true;
	}

	private IEnumerator MouseClickDelay()
	{
		yield return new WaitForSeconds(SwingTime*.75f);
		CanClick = true;
	}
}

//TO DO #1: Create a slight delay before the player can reset their combo.

//TO DO #2: Create a coroutine to allow the player to continue the combo, even after being out of the attack state for
//around .2f seconds.

//CanClick checks after the delay before letting the player click for the next attack.
//Clicked is true if the player clicks, then it is checked during the coroutine.
//CanSwing is for allowing the player to swing by themselves.