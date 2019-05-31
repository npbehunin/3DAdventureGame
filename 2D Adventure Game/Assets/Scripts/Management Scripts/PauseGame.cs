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

	void Update()
	{
		if (Input.GetKeyDown(KeyCode.E))
		{
			if (!IsPaused)
			{
				PauseTheGame();
			}
			else
			{
				UnpauseTheGame();
			}
		}
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
