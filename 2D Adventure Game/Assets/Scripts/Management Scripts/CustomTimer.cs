using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomTimer : MonoBehaviour
{
	//This script works similarly to WaitForSeconds inside a coroutine. However, if the Paused bool is set to true,
	//the coroutine will wait until it is unpaused to continue counting down.
	
	public static IEnumerator Timer(float time)
	{
		while (true)
		{
			if (!PauseGame.IsPaused)
			{
				time -= Time.deltaTime;
			}

			if (time <= 0)
			{
				yield break; //"yield break" returns nothing and stops the coroutine. "break" would simply stop the while loop.
			}
			yield return null; //Runs again the next frame. Unity checks if coroutines returns null. If so, it runs it again.
		}	
	}
}
