using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sword : Weapon3D
{
	public int MaxSwingNumber;
	public float SwingTime;
	
	//(If any of these are left empty, the default effect will play.)
	//Swing sound effect
	//Collision sound effect
	//Trail effect
	//Collision effect
	//Aura effect
	
	//The effect and sound managers will access the EquipWeapon3D index to get these.

	void OnTriggerEnter(Collider col)
	{
		//Use this for sword collision
	}
}
