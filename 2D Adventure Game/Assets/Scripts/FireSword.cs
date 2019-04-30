using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireSword : Sword {

	// Use this for initialization
	protected override void Start()
	{
		base.Start();
		Damage = 5;
	}

	void OnEnable()
	{
		Debug.Log("Fire Sword Active");
	}
	// Update is called once per frame
	protected override void Update()
	{
		
	}
}
