using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwordTest : Weapon
{
    public int SwingNumber;
    public int AnimatorSwingNumber;
    public int MaxSwingNumber;
    public int MousePhase;
    
    public bool CanSwing;
    public bool SwordEquipped;

    public float SwingTime;
    
    public Animator animator;
    public PlayerMovement player;

    private Coroutine SwingCoroutine;
    private Coroutine ClickCoroutine;
    private Coroutine DelayForClickCombo;
    
    public EquipWeapon equipweapon;
    
    protected virtual void Start()
    {
        SwingNumber = 1;
        CanSwing = true;
        SwingTime = .15f;
    }
    
    protected virtual void Update()
    {
        SwordEquipped = equipweapon.SwordEquipped;
        animator.SetInteger("SwordAttackState", AnimatorSwingNumber);
        Debug.Log(MousePhase);
        if (Input.GetMouseButtonDown(0))
        {
            if (SwordEquipped)
            {
                if (CanSwing)
                {
                    SwordSwing();
                }

                if (MousePhase == 1)
                {
                    MousePhase = 2;
                }

                if (MousePhase == 3)
                {
                    SwingNumber = 0;
                    MousePhase = 4;
                }

                if (MousePhase == 4)
                {
                    MousePhase = 5;
                }
            }
        }
    }
    
    void SwordSwing()
    {
        //Debug.Log("Ran the sword swing function");
        if (SwingNumber < MaxSwingNumber)
        {
            CanSwing = false;
            player.SwordMomentumScale = 0;
            SwingCoroutine = StartCoroutine(SwordSwingTiming());
            SwingNumber += 1;
            AnimatorSwingNumber = SwingNumber;
            player.GetSwordSwingDirection();
            
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
        }
    }
    
    
    
    
    
    
    //Coroutine-------------------------------
    private IEnumerator SwordSwingTiming()
    {
        player.currentState = PlayerState.Attack;
        ClickCoroutine = StartCoroutine(MouseClickDelay());
        yield return new WaitForSeconds(SwingTime);
        if (SwingNumber < MaxSwingNumber)
        {
            if (MousePhase == 2)
            {
                SwordSwing();
            }
            else
            {
                CanSwing = true;
            }
        }
        else
        {
            DelayForClickCombo = StartCoroutine(EndDelay()); 
        }
        yield return new WaitForSeconds(.3f);
        ResetSwordAttack();
        if (MousePhase == 5)
        {
            SwordSwing();
        }
        yield return new WaitForSeconds(.2f);
        SwingNumber = 0;
    }
    //Coroutine-------------------------------
    
    
    
    
    
    
    protected void OnDisable()
    {
        ResetSwordAttack();
        SwingNumber = 0;
        
        if (SwingCoroutine != null)
        {
            StopCoroutine(SwingCoroutine);
        }
    }
    
    private void ResetSwordAttack()
    {
       if (DelayForClickCombo != null)
        {
            StopCoroutine(DelayForClickCombo);
        }
        if (ClickCoroutine != null)
        {
            StopCoroutine(ClickCoroutine);
        }
        animator.SetInteger("SwordAttackState", 0);
        Debug.Log("Resetting");
        CanSwing = true;
        AnimatorSwingNumber = 0;
        player.currentState = PlayerState.Idle;
    }
    
    private IEnumerator MouseClickDelay()
    {
        yield return new WaitForSeconds(SwingTime*.75f);
        MousePhase = 1;
    }

    private IEnumerator EndDelay()
    {
        yield return new WaitForSeconds(.2f);
        MousePhase = 3;
    }
}