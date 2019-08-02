using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters;
using UnityEngine;

public class Clothes : MonoBehaviour
{

	public Sprite[] Shirt;
	
	private int shirtSelection = 0;
	private SpriteRenderer spriteRenderer;
	
	// Use this for initialization
	void Start ()
	{
		spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
	}
	
	// Update is called once per frame
	void Update ()
	{
		if (Shirt[shirtSelection] != null)
		{
			spriteRenderer.sprite = Shirt[shirtSelection];
		}

		if (Input.GetKeyDown(KeyCode.O))
		{
			Debug.Log("Pressed O!");
			shirtSelection += 1;
		}
		
		if (Input.GetKeyDown(KeyCode.P))
		{
			shirtSelection -= 1;
		}

		if (shirtSelection > Shirt.Length - 1)
		{
			shirtSelection = 0;
		}

		if (shirtSelection < 0)
		{
			shirtSelection = Shirt.Length - 1;
		}
		Debug.Log(shirtSelection);
	}
}
