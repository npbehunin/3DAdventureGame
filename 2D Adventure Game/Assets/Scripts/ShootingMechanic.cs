using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShootingMechanic : MonoBehaviour
{
	private Ray ray;
	private RaycastHit hit;

	public bool BowEquipped;

	public GameObject projectile;
	public PlayerRigidbodyMovementExperiment player;

	void Start () 
	{
		
	}
	
	void Update ()
	{
		if (Input.GetMouseButtonDown(0))
		{
			if (BowEquipped)
			{
				GameObject obj = Instantiate(projectile, player.transform.position, Quaternion.identity);
			}
		}
	}
}
