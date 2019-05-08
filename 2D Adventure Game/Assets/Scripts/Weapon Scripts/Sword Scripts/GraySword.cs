using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GraySword : SwordMechanic
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
        Damage = 1;
        MaxSwingNumber = 3;
       // SwingTimeDelay = .3f;
        SwingTime = .15f;
        player.SwordMomentumSmooth = 4f;
        player.SwordMomentumPower = 1f;
    }
}
