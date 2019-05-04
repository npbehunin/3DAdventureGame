using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneManage : MonoBehaviour
{

	public Scene loadedlevel;

	void Start () 
	{
		Scene loadedLevel = SceneManager.GetActiveScene();
	}
	
	void Update () 
	{
		
	}

	public void RestartScene(Scene loadedLevel)
	{
		SceneManager.LoadScene(loadedLevel.buildIndex);
	}
}
