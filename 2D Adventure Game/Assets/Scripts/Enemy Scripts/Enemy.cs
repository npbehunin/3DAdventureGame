using System.Collections;
using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine;
public enum EnemyState
{
	Idle, Walk, Target, Attack, Knocked, Dead
}
public class Enemy : MonoBehaviour
{
	public EnemyState currentState;

	public int Health;
	public int Damage;

	public float MoveSpeed;
	private float horizontalspeed;
	private float verticalspeed;

	public EquipWeapon WeaponEquipped;
	
	public Vector3 Position;
	void Start () 
	{
		
	}
	
	// Update is called once per frame
	void Update () 
	{
		if (Health <= 0)
		{
			gameObject.SetActive(false);
		}
	}
	
	void OnTriggerEnter2D(Collider2D col)
	{
		if (col.gameObject.CompareTag("WeaponHitbox"))
		{
			Damage = WeaponEquipped.WeaponDamage;
			Debug.Log("Hello");
			TakeDamage();
		}
	}

	void TakeDamage()
	{
		Health -= Damage;
	}
}

//To get the damage
