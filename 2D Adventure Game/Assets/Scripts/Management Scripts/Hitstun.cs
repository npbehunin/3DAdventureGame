using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hitstun : MonoBehaviour
{
	public static bool HitStunEnabled;
	
	public static IEnumerator HitStunCoroutine()
	{
		HitStunEnabled = true;
		yield return CustomTimer.Timer(.2f); //Hitstun time
		HitStunEnabled = false;
	}
}
