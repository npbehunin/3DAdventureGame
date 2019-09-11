using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShootingMechanic3D : MonoBehaviour
{
	private Ray ray;
	private RaycastHit hit;

	public bool BowEquipped, canStartBowHold, canShoot;
	
	private WeaponPhase phase;

	public float moveSpeed;

	private IEnumerator bowholdcheckcoroutine, shootdelaycoroutine;

	public GameObject projectileType;

	public EquipWeapon equipweapon;

	public PlayerMovement player;
	public PlayerAnimation playerAnim;

	void Start ()
	{
		//base.Start();
		StartValues();
	}

	protected virtual void OnEnable()
	{
		StartValues();
	}

	void StartValues()
	{
		canShoot = true;
		canStartBowHold = true;
		phase = WeaponPhase.Inactive;
	}
	
	void Update ()
	{
		//base.Update();
		BowEquipped = equipweapon.BowEquipped;
		if (BowEquipped)
		{
			if (Input.GetMouseButton(0))
			{
				PlayerBowAnimation();
				if (canStartBowHold)
				{
					bowholdcheckcoroutine = BowHoldCheck();
					StartCoroutine(bowholdcheckcoroutine);
					canStartBowHold = false;
				}
			}

			if (Input.GetMouseButtonUp(0))
			{
				player.targetMode.CanTarget = false;
				player.currentState = PlayerState.Idle;

				if (canShoot)
				{
					GameObject arrow = Instantiate(projectileType, gameObject.transform.position, Quaternion.identity);
					arrow.GetComponent<Projectile>().projectileSpeed = moveSpeed;
					phase = WeaponPhase.Phase1;
					StopCoroutine(bowholdcheckcoroutine);
					shootdelaycoroutine = CanShootDelay();
					StartCoroutine(shootdelaycoroutine);
				}
			}
		}

		switch (phase)
		{
			case WeaponPhase.Phase1:
				moveSpeed = 7f;
				break;
			case WeaponPhase.Phase2:
				moveSpeed = 10f;
				break;
			case WeaponPhase.Phase3:
				moveSpeed = 20f;
				break;
			default:
				moveSpeed = 7f;
				break;
		}
	}

	void PlayerBowAnimation()
	{
		playerAnim.SetAnimState(AnimationState.Walk);
		playerAnim.AnimLookDirection();
		player.currentState = PlayerState.Walk;
		player.targetMode.CanTarget = true;
	}

	private IEnumerator BowHoldCheck()
	{
		phase = WeaponPhase.Phase1;
		yield return CustomTimer.Timer(.3f);
		phase = WeaponPhase.Phase2;
		yield return CustomTimer.Timer(.3f);
		phase = WeaponPhase.Phase3;
	}

	private IEnumerator CanShootDelay()
	{
		canShoot = false;
		yield return CustomTimer.Timer(.5f);
		canShoot = true;
		canStartBowHold = true;
	}
}
