using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Boo.Lang;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Experimental.UIElements;

public class Projectile : MonoBehaviour
{
	private Ray ray;
	private RaycastHit2D hit;

	public float moveSpeed = 2f;

	private Vector3 movementVector;
	private Vector3 targetpos;
	private Vector3 targetDir;
	private Vector2 mousePos2D;

	public LayerMask ShootingArea;

	void Start()
	{
		ray = Camera.main.ScreenPointToRay(Input.mousePosition); //Converts mouse position to units on camera.
		mousePos2D.Set(ray.origin.x, ray.origin.y);
		RaycastHit2D hit = Physics2D.Raycast(mousePos2D, ray.direction, ShootingArea);

		if (hit)
		{
			Debug.Log("eehhh");
			//targetpos and movement vector set
			targetpos.Set(hit.point.x, hit.point.y, 0);
			movementVector = (targetpos - transform.position).normalized * moveSpeed;

			//rotation
			Vector3 vectorToTarget = targetpos - transform.position;
			float angle = Mathf.Atan2(vectorToTarget.y, vectorToTarget.x) * Mathf.Rad2Deg;
			Quaternion q = Quaternion.AngleAxis(angle, Vector3.forward);
			transform.rotation = q;

			StartCoroutine(Destroy());
		}
	}

	void Update()
	{
		transform.position += movementVector * Time.deltaTime;
	}

	IEnumerator Destroy()
	{
		yield return new WaitForSeconds(1);
		Destroy(gameObject);
	}
}

