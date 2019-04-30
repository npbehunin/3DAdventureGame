using System.Collections;
using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine;
public enum EnemyState
{
	Idle, Walk, Target, Attack, Knocked
}
public class Enemy : MonoBehaviour
{
	public EnemyState currentState;

	public int Health;
	public int Damage;
	public string EnemyName;
	public int Attack;

	public float Thrust;
	public float MoveSpeed;
	private float horizontalspeed;
	private float verticalspeed;
	
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
	
	void OnTriggerEnter(Collider col)
	{
		Damage = col.GetComponentInChildren<Weapon>().Damage;
		if (col.gameObject.CompareTag("WeaponHitbox"))
		{
			Debug.Log(Damage);
			TakeDamage();
		}
	}

	void TakeDamage()
	{
		Health -= Damage;
	}
}
