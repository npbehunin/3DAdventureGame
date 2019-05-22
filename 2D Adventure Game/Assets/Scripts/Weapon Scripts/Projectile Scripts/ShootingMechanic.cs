using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShootingMechanic : Weapon
{
	private Ray ray;
	private RaycastHit hit;

	public bool BowEquipped;
	public bool canStartBowHold;
	public bool canShoot;
	
	private WeaponPhase phase;

	public float moveSpeed;
	public float BowThrust;

	private IEnumerator bowholdcheckcoroutine;
	private IEnumerator shootdelaycoroutine;
	
	public GameObject projectileType;

	public EquipWeapon equipweapon;

	public PlayerMovement player;
	public Animator animator;

	protected virtual void Start ()
	{
		//Bowthrust
		BowThrust = 3f;
		KnockbackPower = BowThrust;
		//
		canShoot = true;
		canStartBowHold = true;
		phase = WeaponPhase.Inactive;
	}
	
	protected override void Update ()
	{
		base.Update();
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
				if (canShoot)
				{
					player.targetMode.CanTarget = false;
					animator.SetBool("IsChargingBow", false);
					player.currentState = PlayerState.Idle;
					
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
		animator.SetBool("IsChargingBow", true);
		player.currentState = PlayerState.Walk;
		player.targetMode.CanTarget = true;

		switch (player.targetMode.direction)
		{
			case AnimatorDirection.Up:
				animator.SetFloat("DirectionY", 1);
				animator.SetFloat("DirectionX", 0);
				animator.SetFloat("SpeedX", 0);
				animator.SetFloat("SpeedY", 1);
				break;
			case AnimatorDirection.Down:
				animator.SetFloat("DirectionY", -1);
				animator.SetFloat("DirectionX", 0);
				animator.SetFloat("SpeedX", 0);
				animator.SetFloat("SpeedY", -1);
				break;
			case AnimatorDirection.Left:
				animator.SetFloat("DirectionY", 0);
				animator.SetFloat("DirectionX", -1);
				animator.SetFloat("SpeedX", -1);
				animator.SetFloat("SpeedY", 0);
				break;
			case AnimatorDirection.Right:
				animator.SetFloat("DirectionY", 0);
				animator.SetFloat("DirectionX", 1);
				animator.SetFloat("SpeedX", 1);
				animator.SetFloat("SpeedY", 0);
				break;
			default:
				Debug.Log("No angle detected");
				break;
		}
		//Idea: Could make an animation script that controls the animations. Then the functions could just be called here.
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
