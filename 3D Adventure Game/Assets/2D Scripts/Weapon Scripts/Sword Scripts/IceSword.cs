using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class IceSword : SwordMechanic 
{
    protected override void Start()
    {
        base.Start();
        GetWeaponStats();
    }
    
    void OnEnable()
    {
        GetWeaponStats();
    }

    void GetWeaponStats()
    {
        WeaponDamage.initialValue = 5;
        MaxSwingNumber = 2;
       // SwingTimeDelay = .2f;
        SwingTime = .1f;
        //player.SwordMomentumSmooth = 2f;
        //player.SwordMomentumPower = 2f;
    }
}
