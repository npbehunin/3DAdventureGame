using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public enum EnemyState
{
	Idle, Walk, Target, Attack, Knocked
}
public class Enemy : MonoBehaviour
{
	public EnemyState currentState;

	public int health;
	public string enemyName;
	public int attack;

	public float thrust;
	public float MoveSpeed;
	private float horizontalspeed;
	private float verticalspeed;
	
	public Vector3 position;
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
