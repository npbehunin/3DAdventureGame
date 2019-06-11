using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum InteractDirection
{
	up, down, left, right, all
}
public class Interactable : MonoBehaviour
{
	protected bool CanInteract;
	private bool PlayerDirChecked, IsTriggered;
	public InteractDirection InteractDir;
	public Vector3Value PlayerDir;
	private Vector3 dir;

	protected virtual void Start()
	{
		InteractDir = InteractDirection.all; //Default
		CanInteract = true;
		CheckDir();
	}

	public void CheckDir()
	{
		switch (InteractDir)
		{
			case InteractDirection.up:
				dir = new Vector3(0, 1, 0);
				break;
			case InteractDirection.down:
				dir = new Vector3(0, -1, 0);
				break;
			case InteractDirection.left:
				dir = new Vector3(-1, 0, 0);
				break;
			case InteractDirection.right:
				dir = new Vector3(1, 0, 0);
				break;
		}
	}

	protected virtual void Update()
	{
		if (IsTriggered)
		{
			if (Input.GetKeyDown(KeyCode.E) && CanInteract && PlayerDirChecked)
			{
				Interact();
				CanInteract = false;
			}

			if (dir == PlayerDir.initialPos || InteractDir == InteractDirection.all)
			{
				PlayerDirChecked = true;
			}
			else
			{
				PlayerDirChecked = false;
			}
		}
	}

	void OnTriggerEnter2D(Collider2D col)
	{
		if (col.gameObject.CompareTag("Player"))
		{
			IsTriggered = true; //This allows rigidbody to sleep but still detect a "trigger"
		}
		else
		{
			IsTriggered = false;
		}
	}

	protected virtual void Interact()
	{
		//Do something
	}
}