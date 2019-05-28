using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hitstun : MonoBehaviour
{
	public static bool HitStunEnabled;

	public static IEnumerator StartHitstun()
	{
		HitStunEnabled = true;
		yield return CustomTimer.Timer(.5f); //Hitstun time
		HitStunEnabled = false;
	}
	
	//ISSUE: The player and enemy that are hitstunned don't have their timers stopped because the timer only checks
	//if the entire game is paused.
}
