using System.Collections;
using System.Collections.Generic;
using KinematicCharacterController.Nate;
using UnityEngine;

public class NewTempSwordCoroutine : MonoBehaviour
{
    public bool SwordEquipped;
    public bool CanSwing;
    public bool DelayHasPassed;
    public bool MouseHasBeenClicked;

    public int SwingNumber;
    public int MaxSwingNumber;
    
    public NateCharacterController controller;

    private Coroutine SwingTimerCoroutine;
    private Coroutine DelayBetweenClicksCoroutine;
    private Coroutine DelayBetweenComboResetCoroutine;
    
    // Start is called before the first frame update
    void Start()
    {
        SwordEquipped = true;
        CanSwing = true;
        MaxSwingNumber = 3;
        DelayHasPassed = true;
    }
    
    //Check if the player can swing and if the mouse has been pressed.
    void Update()
    {
        if (CanSwing && MouseHasBeenClicked)
        {
            if (DelayBetweenComboResetCoroutine != null)
            {
                StopCoroutine(DelayBetweenComboResetCoroutine);
            }

            if (controller._tempSwordCoroutine != null)
            {
                StopCoroutine(controller._tempSwordCoroutine);
            }

            CanSwing = false;
            MouseHasBeenClicked = false;

            controller.TransitionToState(CharacterState.SwordAttack);
            SwingNumber += 1;
            controller.RunSwordSwingMovement();
            DelayBetweenClicksCoroutine = StartCoroutine(DelayBetweenClick(.175f));
            SwingTimerCoroutine = StartCoroutine(SwingTimer(.35f));
        }
    }

    //Runs when mouse has been pressed.
    public void StartSwordSwing()
    {
        if (SwordEquipped)
        {
            if (SwingNumber < MaxSwingNumber)
            {
                if (DelayHasPassed)
                {
                    MouseHasBeenClicked = true;
                    DelayHasPassed = false;
                }
            }
        }
    }

    void ResetCombo()
    {
        SwingNumber = 0;
    }

    //Time between swings
    private IEnumerator SwingTimer(float time)
    {
        yield return CustomTimer.Timer(time);
        CanSwing = true;
        controller.TransitionToState(CharacterState.Default);
        DelayBetweenComboResetCoroutine = StartCoroutine(DelayBetweenComboReset(.25f));
    }

    //Delay before the player can click to swing again.
    private IEnumerator DelayBetweenClick(float time)
    {
        //Debug.Log("Player can click.");
        yield return CustomTimer.Timer(time);
        DelayHasPassed = true;
    }

    //Delay at the end of the swing before resetting the combo
    private IEnumerator DelayBetweenComboReset(float time)
    {
        yield return CustomTimer.Timer(time);
        ResetCombo();
    }
}
