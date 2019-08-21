using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EquipWeapon : MonoBehaviour
{
	public SwordMechanic[] SwordTypes;
	public ShootingMechanic[] BowTypes;
	public Projectile projectile;

	public bool WeaponEquipped;
	public bool SwordEquipped;
	public bool BowEquipped;
	public bool BombEquipped;

	public int SwordIndex;
	public int BowIndex;

	void Start()
	{
		WeaponEquipped = true;
		SwordEquipped = true;
		BowEquipped = false;
		DisableWeapons();
	}

	void Update()
	{
		//GetDamage();
		if (WeaponEquipped)
		{
			if (SwordEquipped)
			{
				if (BowTypes[BowIndex] != null)
				{
					BowTypes[BowIndex].gameObject.SetActive(false);
				}

				if (Input.GetKey(KeyCode.Alpha1))
				{
					SwordIndex = 0;
				}

				if (Input.GetKey(KeyCode.Alpha2))
				{
					SwordIndex = 1;
				}

				if (Input.GetKey(KeyCode.Alpha3))
				{
					SwordIndex = 2;
				}
				
				for (int i = 0; i < SwordTypes.Length; i++)
				{
					if (i != SwordIndex)
					{
						SwordTypes[i].gameObject.SetActive(false);
					}
					else
					{
						SwordTypes[i].gameObject.SetActive(true);
					}
				}
			}

			if (BowEquipped)
			{
				if (SwordTypes[SwordIndex] != null)
				{
					SwordTypes[SwordIndex].gameObject.SetActive(false);
				}
				
				if (Input.GetKey(KeyCode.Alpha4))
				{
					BowIndex = 0;
				}

				if (Input.GetKey(KeyCode.Alpha5))
				{
					BowIndex = 1;
				}

				for (int i = 0; i < BowTypes.Length; i++)
				{
					if (i != BowIndex)
					{
						BowTypes[i].gameObject.SetActive(false);
					}
					else
					{
						BowTypes[i].gameObject.SetActive(true);
					}
				}
			}
			
			if (Input.GetKeyDown(KeyCode.Space))
			{
				if (BowEquipped)
				{
					BowEquipped = false;
					SwordEquipped = true;
				}
				else if (SwordEquipped)
				{
					SwordEquipped = false;
					BowEquipped = true;
				}
				else
				{
					BombEquipped = true;
				}
			}
		}
		else
		{
			DisableWeapons();
		}
	}

	void DisableWeapons()
	{
		foreach (var t in SwordTypes)
		{
			t.gameObject.SetActive(false);
		}
		foreach (var t in BowTypes)
		{
			t.gameObject.SetActive(false);
		}
	}
	
	//void GetDamage()
	//{
	//	if (SwordEquipped)
	//	{
	//		WeaponDamage = SwordTypes[SwordIndex].gameObject.GetComponent<Weapon>().Damage;
	//	}
//
	//	if (BowEquipped)
	//	{
	//		WeaponDamage = BowTypes[BowIndex].gameObject.GetComponent<Weapon>().Damage;
	//	}
	//	//Debug.Log(WeaponDamage);
	//}
}
