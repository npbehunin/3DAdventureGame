using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FastBow : ShootingMechanic {

	// Use this for initialization
	protected override void Start () 
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
		Damage = 5;
	}
}
