using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters;
using UnityEngine;

public class Knockback : MonoBehaviour
{
	public float KnockbackPower;
	public float knockbackTime;

	private Coroutine KnockCoroutine;
	public CameraFollowPlayer camera;

	void Start()
	{
		knockbackTime = 1f;
	}

	void Update()
	{
		KnockbackPower = Weapon.KnockbackPower;
	}

	public void Knocked(Rigidbody2D rb, Vector3 col)
	{
		if (rb != null)
		{
			if (KnockCoroutine != null)
			{
				StopCoroutine(KnockCoroutine);
			}

			Debug.Log("Knocked");
			//camera.Knocked = true;
			Vector3 difference = rb.transform.position - col;
			camera.StartSwordShake(.3f, difference.normalized);
			difference = difference.normalized * KnockbackPower;
			rb.AddForce(difference, ForceMode2D.Impulse);
			KnockCoroutine = StartCoroutine(ResetKnock(rb));
		}
	}

	private IEnumerator ResetKnock(Rigidbody2D rb)
	{
		if (rb != null)
		{
			rb.gameObject.GetComponent<Enemy>().currentState = EnemyState.Knocked;
			yield return new WaitForSeconds(knockbackTime);
			rb.velocity = Vector2.zero;
			rb.gameObject.GetComponent<Enemy>().currentState = EnemyState.Idle;
		}
	}
}