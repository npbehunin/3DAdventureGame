using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireSword : SwordMechanic 
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
        Damage = 10;
        MaxSwingNumber = 5;
        SwingTimeDelay = .3f;
        SwingTime = .8f;
        player.SwordMomentumSmooth = 3f;
        player.SwordMomentumPower = 5f;
    }
}
