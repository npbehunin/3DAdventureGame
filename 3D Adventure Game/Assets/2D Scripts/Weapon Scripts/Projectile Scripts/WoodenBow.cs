﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WoodenBow : ShootingMechanic {

	// Use this for initialization
	protected override void Start () 
	{
		base.Start();
		GetWeaponStats();
	}

	void OnEnable()
	{
		GetWeaponStats();
		base.OnEnable();
	}
	
	void GetWeaponStats()
	{
		WeaponDamage.initialValue = 2;
	}
}