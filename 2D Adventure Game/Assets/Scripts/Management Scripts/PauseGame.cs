using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseGame : MonoBehaviour
{

	public static bool IsPaused;
	
	void Start ()
	{
		IsPaused = false;
	}

	public static void PauseTheGame()
	{
		IsPaused = true;
		//Anything checking for ispaused will be paused.
	}

	public static void UnpauseTheGame()
	{
		IsPaused = false;
	}
}
