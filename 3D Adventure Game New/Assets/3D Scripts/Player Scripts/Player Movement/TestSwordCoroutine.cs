using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestSwordCoroutine: MonoBehaviour
{
	public bool SwordEquipped, CanSwing, CanDelayForCombo;

	public int ComboPhase, SwingNumber, MaxSwingNumber;

	public float SwingTime;
	public BoolValue playerCanDecel;

	private Coroutine SwingCoroutine, ClickCoroutine, DelayForClickCombo;

	void Start()
	{
		ComboPhase = 0;
		CanSwing = true;
		CanDelayForCombo = false;
		MaxSwingNumber = 3;
		SwingTime = .25f; //.25 default
		SwordEquipped = true; //Temporary! Fix when EquipWeapon3D is set up.
	}

	void Update()
	{
		
	}

	//Runs when mouse button is pressed in Character Controller
	void StartSwordCoroutine()
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

			playerCanDecel.initialBool = true;
			ComboPhase = 0;
			CanSwing = false;
			SwingCoroutine = StartCoroutine(SwordSwingTiming());
			SwingNumber += 1;
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
		CanDelayForCombo = false;
		CanSwing = true;
		//Set attack state to false
	}

	//Sword swing timing
	private IEnumerator SwordSwingTiming()
	{
		CanDelayForCombo = true;
		//Set attack state to true.
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
			//Set attack state to false
			yield return CustomTimer.Timer(.15f);
			ResetSwordAttack();
		}
		else
		{
			yield return CustomTimer.Timer(.2f);
			//Set attack state to false
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