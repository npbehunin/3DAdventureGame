using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JellyAttackHitbox : MonoBehaviour
{
	public BoolValue PlayerCollision, JellyBounce;

	void OnTriggerStay2D(Collider2D col)
	{
		if (col.gameObject.CompareTag("Player"))
		{
			PlayerCollision.initialBool = true;
			JellyBounce.initialBool = true;
		}
		else
		{
			PlayerCollision.initialBool = false;
			JellyBounce.initialBool = false;
		}
	}
}
