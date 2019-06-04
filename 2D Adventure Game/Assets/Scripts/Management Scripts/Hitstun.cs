using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hitstun : MonoBehaviour
{
	public static bool HitStunEnabled;

	public static IEnumerator StartHitstun()
	{
		HitStunEnabled = true;
		yield return CustomTimer.Timer(.075f); //Hitstun time
		HitStunEnabled = false;
	}
}
