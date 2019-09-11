using System.Collections;
using System.Collections.Generic;
using KinematicCharacterController.Nate;
using UnityEngine;

public class TestSwordCoroutine: MonoBehaviour
{
	public bool SwordEquipped, CanDelayForCombo;

	public int ComboPhase, SwingNumber, AnimatorSwingNumber, MaxSwingNumber;

	public float SwingTime;
	public BoolValue CanSwing;

	private Coroutine SwingCoroutine, ClickCoroutine, DelayForClickCombo;

	public Animator animator;
	public NateCharacterController controller;

	void Start()
	{
		ComboPhase = 0;
		CanSwing.initialBool = true;
		CanDelayForCombo = false;
		AnimatorSwingNumber = 0;
		MaxSwingNumber = 3;
		SwingTime = .25f; //.25 default
		SwordEquipped = true; //Temporary! Fix when EquipWeapon3D is set up.
	}

	void Update()
	{
		//animator.SetInteger("SwordAttack", AnimatorSwingNumber);
	}

	//Runs when mouse button is pressed in Character Controller
	public void StartSwordCoroutine()
	{
		Debug.Log(AnimatorSwingNumber);
		if (SwordEquipped)
		{
			if (CanSwing.initialBool)
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

			//playerCanDecel.initialBool = true;
			controller.RunSwordSwingMovement(); //EEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEE
			ComboPhase = 0;
			CanSwing.initialBool = false;
			SwingCoroutine = StartCoroutine(SwordSwingTiming());
			SwingNumber += 1;
			AnimatorSwingNumber = SwingNumber;
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
		ComboPhase = 0;
		SwingNumber = 0;
		AnimatorSwingNumber = 0;
		CanDelayForCombo = false;
		CanSwing.initialBool = true;
		controller.TransitionToState(CharacterState.Default);
	}

	//Sword swing timing
	private IEnumerator SwordSwingTiming()
	{
		CanDelayForCombo = true;
		controller.TransitionToState(CharacterState.SwordAttack);
		ClickCoroutine = StartCoroutine(MouseClickDelay());
		yield return CustomTimer.Timer(SwingTime);
		if (ComboPhase == 2)
		{
			SwordSwing();
		}
		else
		{
			CanSwing.initialBool = true;
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
			controller.TransitionToState(CharacterState.Default);
			yield return CustomTimer.Timer(.15f);
			ResetSwordAttack();
		}
		else
		{
			yield return CustomTimer.Timer(.2f);
			AnimatorSwingNumber = 0;
			controller.TransitionToState(CharacterState.Default);
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

//Known issues:
//If the hitstun lasts longer than the sword swing, the player's last state will be put into the attack state and the
//player can do nothing.