using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldLayerNotes : MonoBehaviour {

	//When we start building the world, by default most objects should be some sort of a wall/slide layer.
	//This way, we can specifically define certain areas as ground/walkable.
	
	//We want to do this so our pathfinding script (which checks for layers that aren't ground) will correctly recognize
	//objects that are a part of an unwalkable layer.
	
	//Layer INFO:
	
	//Ground: Areas the player can walk on. (The player can technically walk on any layer unless we specify something to happen.)
	//Wall: Obstacles. The player can still technically walk on walls, but the pathfinding algorithm will return it as unwalkable.
	//Slides: Areas the player/pet should slide down.
}
