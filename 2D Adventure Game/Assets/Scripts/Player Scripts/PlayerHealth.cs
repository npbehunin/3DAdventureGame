using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
	public int Health;
	public int EnemyDamage;
	public bool IsInvincible;

	public float InvincibilityTime;

	public SceneManage manager;
	
	void Start ()
	{
		Health = 10;
		IsInvincible = false;
		InvincibilityTime = 2;
	}
	
	void Update () 
	{
		if (Health <= 0)
		{
			StartCoroutine(PlayerDeath());
		}
	}

	void OnCollisionEnter2D(Collision2D col)
	{
		if (col.gameObject.CompareTag("Enemy"))
		{
			if (IsInvincible == false)
			{
				EnemyDamage = col.gameObject.GetComponent<Enemy>().Damage;
				TakeDamage();
			}
		}
	}

	void TakeDamage()
	{
		Health -= EnemyDamage;
		Debug.Log(Health);
		StartCoroutine(Invincibility());
	}

	private IEnumerator Invincibility()
	{
		IsInvincible = true;
		yield return new WaitForSeconds(InvincibilityTime);
		IsInvincible = false;
	}

	private IEnumerator PlayerDeath()
	{
		IsInvincible = true;
		if (gameObject.activeSelf)
		{
			Debug.Log("Player has died!");
			yield return new WaitForSeconds(1.5f);
			manager.RestartScene(manager.loadedlevel);
		}
	}
}
