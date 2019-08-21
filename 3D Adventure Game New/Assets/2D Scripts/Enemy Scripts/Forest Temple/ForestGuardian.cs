using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ForestGuardian : MonoBehaviour {

	//Forest guardian does similar attacks to the final boss
	//If player enters in chase range, the enemy can summon rocks and thorns. (Stomps foot for rocks)
	//If the player enters its close range, the enemy can swing his arm.
	
	//Check distance
	//If met, start a coroutine
	//Coroutine sets a delay between each attack at a random interval
	//The coroutine will also set one of the two bools to be true at random
	//It will also set the arm swing to the random bool, but will only set it if the player is close enough
	//A seperate rock script will be created. When instantiated, it will play the animation and eventuall have
	//the hitbox.
	//The enemy will stop moving when using its attacks.
	//Once the coroutine stops, enemy continues walking. Time must pass before the next attack.
	//Enemy will constantly slowly follow the player.
	
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
