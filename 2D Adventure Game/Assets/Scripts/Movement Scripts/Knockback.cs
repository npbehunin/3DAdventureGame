using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters;
using UnityEngine;

public class Knockback : MonoBehaviour
{
	public float thrust;
	public float knockbackTime;

	private Coroutine KnockCoroutine;

	void Start () 
	{
		
	}
	
	void Update () 
	{
		
	}

	private void OnTriggerEnter2D(Collider2D other)
	{
		if (other.gameObject.CompareTag("Enemy"))
		{
			Rigidbody2D enemy = other.GetComponent<Rigidbody2D>();
				if(enemy != null)
				{
					ResetKnock(enemy);
					if (KnockCoroutine != null)
					{
						StopCoroutine(KnockCoroutine);
					}
					enemy.GetComponent<Enemy>().currentState = EnemyState.Knocked;
					Vector2 difference = enemy.transform.position - transform.position;
					difference = difference.normalized * thrust;
					enemy.AddForce(difference, ForceMode2D.Impulse);
					KnockCoroutine = StartCoroutine(Knock(enemy));
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

//Decide if we want to put it on player or enemy. There will be 2 states, "STUNNED" and "KNOCKED" for each type of hit.
//This script could getcomponent the enemy's float called thrust and use that as the knockback BUT it might be better
//to have it on the enemy itself in case the player hits multiple types of enemies at once.