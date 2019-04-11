using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters;
using UnityEngine;

public class Knockback : MonoBehaviour
{

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
			
		}
	}
}

//OnCollisionEnter won't allow us to get the component for some reason. Only works with ontriggerenter.
//If we want to make use of the knock ienumerator, we need to set the enemy's linear drag to 0 before addforce.