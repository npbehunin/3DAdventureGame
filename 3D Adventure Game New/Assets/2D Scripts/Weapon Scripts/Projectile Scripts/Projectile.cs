using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Analytics;

public class Projectile : Weapon
{
	private Ray ray;
	private RaycastHit2D hit;

	public float projectileSpeed;

	private Vector3 movementVector;
	private Vector3 targetpos;
	private Vector3 targetDir;
	private Vector2 mousePos2D;

	public bool UsingMouse;
	public Vector3 target;

	protected override void Start()
	{
		StartCoroutine(Destroy());
		if (UsingMouse)
		{
			ray = Camera.main.ScreenPointToRay(Input.mousePosition); //Converts mouse position to units on camera.
			mousePos2D.Set(ray.origin.x, ray.origin.y);
			RaycastHit2D hit = Physics2D.Raycast(mousePos2D, ray.direction);

			if (hit)
			{
				//targetpos and movement vector set
				targetpos.Set(hit.point.x, hit.point.y, 0);
				movementVector = (targetpos - transform.position).normalized * projectileSpeed;

				//rotation
				Vector3 vectorToTarget = targetpos - transform.position;
				float angle = Mathf.Atan2(vectorToTarget.y, vectorToTarget.x) * Mathf.Rad2Deg;
				Quaternion q = Quaternion.AngleAxis(angle, Vector3.forward);
				transform.rotation = q;
			}
		}
		else
		{
			//if (target != null)
			{
				targetpos = target;
				movementVector = (targetpos - transform.position).normalized * projectileSpeed;
				Debug.Log(movementVector);
			}
			//StartCoroutine(Destroy());
		}
	}

	protected override void Update()
	{
		transform.position += movementVector * Time.deltaTime;
		//Debug.Log(projectileSpeed);
	}

	IEnumerator Destroy()
	{
		yield return CustomTimer.Timer(1f);
		Destroy(gameObject);
	}
}

