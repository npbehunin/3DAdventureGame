using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EquipWeapon : MonoBehaviour {

	public Sword[] SwordTypes;

	private bool WeaponEquipped;
	private bool SwordEquipped;

	private int SwordIndex;

	void Start ()
	{
		WeaponEquipped = true;
		SwordEquipped = true;
	}
	
	// Update is called once per frame
	void Update () 
	{
		if (WeaponEquipped)
		{
			if (SwordEquipped)
			{
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
				if (Input.GetKey(KeyCode.Alpha1))
				{
					SwordIndex = 0;
				}
				
				if (Input.GetKey(KeyCode.Alpha2))
				{
					SwordIndex = 1;
				}
			}
		}
	}
}
