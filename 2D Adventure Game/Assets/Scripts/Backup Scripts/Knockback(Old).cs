using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters;
using UnityEngine;

public class KnockbackOld : MonoBehaviour
{
	public float thrust;
	public float knockbackTime;
	public GameObject Player;
	public Vector3 PlayerDirection;

	public bool IsPlayer, IsArrow;
	public Rigidbody2D enemy;

	private Coroutine KnockCoroutine;

	public HitPause hitpause;

	void Start ()
	{
		if (Player != null)
		{
			
		}
	}
	
	void Update () 
	{
		if (Player != null)
		{
			Player.GetComponent<PlayerMovement>().GetSwordSwingDirection();
			PlayerDirection = PlayerMovement.test;
		}

		if (IsPlayer)
		{
			if (PlayerDirection != Vector3.zero)
			{
				thrust = 4;
			}
			else
			{
				thrust = 0;
			}
		}

		if (IsArrow)
		{
			thrust = 4;
		}
		//Debug.Log(PlayerDirection);

		//if (enemy != null && enemy.GetComponent<Enemy>().currentState == EnemyState.Paused && !hitpause.HitPaused)
		{
			Debug.Log("Knocked");
			Knocked();
		}
	}

	void Knocked()
	{
		enemy.GetComponent<Enemy>().currentState = EnemyState.Knocked;
		if (enemy != null)
		{
			ResetKnock(enemy);
			if (KnockCoroutine != null)
			{
				StopCoroutine(KnockCoroutine);
			}
			Vector2 difference = enemy.transform.position - transform.position;
			difference = difference.normalized * thrust;
			enemy.AddForce(difference, ForceMode2D.Impulse);
			KnockCoroutine = StartCoroutine(Knock(enemy));
		}
	}

	private void OnTriggerEnter2D(Collider2D other) 
	{
		if (other.gameObject.CompareTag("Enemy"))
		{
			enemy = other.GetComponent<Rigidbody2D>();
			//if (enemy.GetComponent<Enemy>().currentState != EnemyState.Paused && enemy.GetComponent<Enemy>().currentState != EnemyState.Knocked)
			{
				hitpause = enemy.GetComponent<HitPause>();
				if (hitpause != null)
				{
					//hitpause.StartFreeze(.3f);
				}

				//enemy.GetComponent<Enemy>().currentState = EnemyState.Paused;
			}
		}
	}

	private void ResetKnock(Rigidbody2D enemy)
	{
		enemy.velocity = Vector2.zero;
		enemy.GetComponent<Enemy>().currentState = EnemyState.Idle;
	}

	private IEnumerator Knock(Rigidbody2D enemy)
	{
		if (enemy != null)
		{
			yield return new WaitForSeconds(knockbackTime);
			ResetKnock(enemy);
		}
	}
}