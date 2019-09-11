using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PetAnimation : MonoBehaviour
{

	public Animator animator;

	public void SetAttack(bool check)
	{
		animator.SetBool("Attack", check);
	}
}
