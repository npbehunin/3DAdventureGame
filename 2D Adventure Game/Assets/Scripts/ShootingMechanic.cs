using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShootingMechanic : MonoBehaviour
{
	private Ray ray;
	private RaycastHit hit;

	public bool BowEquipped;
	public bool canStartBowHold;
	public bool canShoot;
	
	private WeaponPhase phase;

	public float moveSpeed;

	private IEnumerator bowholdcheckcoroutine;
	private IEnumerator shootdelaycoroutine;
	
	public GameObject projectileType;
	public Projectile projectile;
	public PlayerRigidbodyMovementExperiment player;

	void Start ()
	{
		canShoot = true;
		canStartBowHold = true;
		phase = WeaponPhase.Inactive;
	}
	
	void Update ()
	{
		if (BowEquipped)
		{
			if (Input.GetMouseButton(0))
			{
				if (canStartBowHold)
				{
					bowholdcheckcoroutine = BowHoldCheck();
					StartCoroutine(bowholdcheckcoroutine);
					canStartBowHold = false;
				}
			}

			if (Input.GetMouseButtonUp(0))
			{
				if (canShoot)
				{
					GameObject arrow = Instantiate(projectileType, player.transform.position, Quaternion.identity);
					arrow.GetComponent<Projectile>().projectileSpeed = moveSpeed;
					phase = WeaponPhase.Phase1;
					StopCoroutine(bowholdcheckcoroutine);
					shootdelaycoroutine = CanShootDelay();
					StartCoroutine(shootdelaycoroutine);
				}
			}
			
		}
		if (phase == WeaponPhase.Phase1)
		{
			moveSpeed = 7f;
		}

		if (phase == WeaponPhase.Phase2)
		{
			moveSpeed = 10f;
		}

		if (phase == WeaponPhase.Phase3)
		{
			moveSpeed = 20f;
		}
	}

	private IEnumerator BowHoldCheck()
	{
		phase = WeaponPhase.Phase1;
		yield return new WaitForSeconds(.3f);
		phase = WeaponPhase.Phase2;
		yield return new WaitForSeconds(.3f);
		phase = WeaponPhase.Phase3;
	}

	private IEnumerator CanShootDelay()
	{
		canShoot = false;
		yield return new WaitForSeconds(.5f);
		canShoot = true;
		canStartBowHold = true;
	}
}
