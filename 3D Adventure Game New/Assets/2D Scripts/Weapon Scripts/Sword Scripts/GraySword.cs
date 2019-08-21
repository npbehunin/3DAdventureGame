using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GraySword : SwordMechanic
{
    protected override void Start()
    {
        base.Start();
        GraySwordStats();
    }
    
    void OnEnable()
    {
        //GetWeaponStats();
    }

    void GraySwordStats()
    {
        WeaponDamage.initialValue = 1;
        MaxSwingNumber = 3;
        //SwingTimeDelay = .3f;
        SwingTime = .25f;
    }
}
