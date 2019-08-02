using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hitstun : MonoBehaviour
{
	//Currently not using this script! Right now everything is handled with signals. (7/10/19)
	
	public static bool HitStunEnabled;

	public static IEnumerator StartHitstun()
	{
		HitStunEnabled = true;
		yield return CustomTimer.Timer(.075f); //Hitstun time
		HitStunEnabled = false;
	}
}
