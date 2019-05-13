using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwordTest : Weapon
{
	
	//This is the old sword mechanic script from 5/9/19.
	
	public bool SwordEquipped;
	public bool CanSwing;
	public bool CanClick;
	public bool Clicked;
	public bool CanClickForCombo;
	public bool ClickedForCombo;
	public bool ResetAttack;
	public bool CanDelayForCombo;

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
		Clicked = false;
		CanSwing = true;
		CanClick = false;
		CanDelayForCombo = false;
		ClickedForCombo = false;
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

				if (CanClick)
				{
					CanClick = false;
					Clicked = true;
				}

				if (CanClickForCombo)
				{
					Debug.Log("Clicked for combo.");
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
		CanDelayForCombo = false;
		ResetAttack = false;
		CanSwing = true;
		CanClick = false;
		Clicked = false;
		AnimatorSwingNumber = 0;
		player.currentState = PlayerState.Idle;
	}

	private IEnumerator SwordSwingTiming()
	{
		Debug.Log("Ran");
		CanDelayForCombo = true;
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

		if (SwingNumber >= MaxSwingNumber && CanDelayForCombo)
		{
			CanDelayForCombo = false;
			DelayForClickCombo = StartCoroutine(DelayForNextCombo());
		}

		yield return new WaitForSeconds(.12f);
		if (SwingNumber < MaxSwingNumber)
		{
			AnimatorSwingNumber = 0;
			player.currentState = PlayerState.Idle;
			yield return new WaitForSeconds(.15f);
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
		Debug.Log("Player can now click for combo.");
	}

	private IEnumerator MouseClickDelay()
	{
		yield return new WaitForSeconds(SwingTime*.25f);
		CanClick = true;
	}
}
