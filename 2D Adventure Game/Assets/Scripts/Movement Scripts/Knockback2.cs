using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters;
using UnityEngine;

public class Knockback2 : MonoBehaviour
{
	public float thrust;
	public float knockbackTime;
	public GameObject Player;
	public GameObject enemy;
	public Vector3 PlayerDirection;

	public EnemyState enemystate;

	public bool IsPlayer, IsArrow;
	public Rigidbody2D EnemyRB;
	public Animator EnemyAnimator;
	public Animator PlayerAnimator;

	private Coroutine KnockCoroutine;
	
	public bool HitPaused, CanFreeze;

	public CameraFollowPlayer camera;

	void Start ()
	{
		HitPaused = false;
		CanFreeze = true;
	}
	
	void Update () 
	{
		if (Player != null)
		{
			Player.GetComponent<PlayerMovement>().GetSwordSwingDirection();
			PlayerDirection = Player.GetComponent<PlayerMovement>().test;
		}

		if (IsPlayer)
		{
			if (PlayerDirection != Vector3.zero)
			{
				thrust = 8;
			}
			else
			{
				thrust = 0;
			}
		}

		if (IsArrow)
		{
			thrust = 8;
		}
	}
	
	void Knocked()
	{
		enemystate = EnemyState.Knocked;
		if (EnemyRB != null)
		{
			if (KnockCoroutine != null)
			{
				StopCoroutine(KnockCoroutine);
			}
			Debug.Log("Knocked");
			StartCoroutine(camera.SwordShake(.2f));
			//Change this BS
			//And make sure to run it through update so it applies over time
			Vector2 difference = EnemyRB.transform.position - transform.position;
			difference = difference.normalized * thrust;
			EnemyRB.AddForce(difference, ForceMode2D.Impulse);
			KnockCoroutine = StartCoroutine(ResetKnock(EnemyRB));
		}
	}

	private void OnTriggerEnter2D(Collider2D other) 
	{
		if (other.gameObject.CompareTag("Enemy"))
		{
			enemy = other.gameObject;
			EnemyRB = enemy.GetComponent<Rigidbody2D>();
			enemystate = enemy.GetComponent<Enemy>().currentState;
			//enemystate = EnemyState.Paused;
			Knocked();
			//StartCoroutine(Freeze(.1f));
		}
	}

	private IEnumerator ResetKnock(Rigidbody2D enemyrb)
	{
		if (enemyrb != null)
		{
			yield return new WaitForSeconds(knockbackTime);
			enemyrb.velocity = Vector2.zero;
			enemy.GetComponent<Enemy>().currentState = EnemyState.Idle;
		}
	}

	//Coroutine to freeze the object
//	IEnumerator Freeze(float Seconds)
//	{
	//	if (EnemyRB != null)
	//	{
		//	EnemyRB.isKinematic = true;
		//}

	//	if (EnemyAnimator != null)
	//	{
		//	EnemyAnimator.enabled = false;
	//	}

	//	Player.GetComponent<Rigidbody2D>().isKinematic = true;
	//	//This is where we freeze the player and prevent them from doing any input
	//	PlayerAnimator.enabled = false;

	//	HitPaused = true;
	//	Debug.Log("Paused...");
	//	yield return new WaitForSeconds(Seconds);
	//	Debug.Log("Unpaused");
	//	HitPaused = false;
	//	if (EnemyRB != null)
	//	{
	//		EnemyRB.isKinematic = false;
	//	}

	//	if (EnemyAnimator != null)
	//	{
	//		EnemyAnimator.enabled = true;
	//	}
	//	Player.GetComponent<Rigidbody2D>().isKinematic = false;
		//Bring back player input
	//	PlayerAnimator.enabled = true;
	//	Knocked();
//	}
}

//To DOOOOOOO

//Change the knockback from addforce to our own manual values so it's consistent.