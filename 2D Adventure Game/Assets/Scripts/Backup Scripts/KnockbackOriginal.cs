using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KnockbackOriginal : MonoBehaviour {

	public float thrust;
	public float knockbackTime;

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
				enemy.GetComponent<Enemy>().currentState = EnemyState.Knocked;
				Debug.Log("Collision");
				Vector2 difference = enemy.transform.position - transform.position;
				difference = difference.normalized * thrust;
				enemy.AddForce(difference, ForceMode2D.Impulse);
				StartCoroutine(Knock(enemy));
			}
		}
	}

	private IEnumerator Knock(Rigidbody2D enemy)
	{
		if (enemy != null)
		{
			yield return new WaitForSeconds(knockbackTime);
			enemy.velocity = Vector2.zero;
			enemy.GetComponent<Enemy>().currentState = EnemyState.Idle;
		}
	}
}
