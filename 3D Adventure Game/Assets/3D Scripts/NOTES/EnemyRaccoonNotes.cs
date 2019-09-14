using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyRaccoonNotes : MonoBehaviour
{
    //---SUMMARY---
    //The main enemy in the game.
    //Not necessarily a raccoon. The "bokoblin" enemy of breath of the wild.
    //Lots of personality.
    //Should have a memorable and fun design.
    
    //---MOVEMENT---
    //(IDLE)
    //Sit around campfires.
    //Patrol pathways.
    //Stand around talking.
    //Sleeping.
    
    //(ATTACKING)
    //Walk towards the player by using pathfinding.
    //Work together in "groups". Only ~2 enemies can attack at once. (Note: If an enemy is targeted, that enemy should
        //attack first)
    //Swing melee weapons.

    //(BACK TO IDLE)
    //Wait until the player is no longer within line of sight.
    //Wait for a brief moment.
    //Use pathfinding back to their "home" points.
    
    //---VARIATION---
    //Available in 3 color tiers. Each color matches the difficulty of the enemy.
    //Each enemy difficulty adds another ability. Ex: Sword spin, sword slam, etc.
    //Enemies will carry swords that can vary. Each sword that they use have a very small chance of being dropped. This
        //adds variation without requiring much additional coding.
    //Enemies can also have a "rare" animation where they flail with their sword or something, just to add interest.
    //The enemy can also "scout" an area inside of A tower. While in this tower, they use a bow and arrow to shoot
        //the player. This tower can be knocked down when the player attacks the base or uses targeting with their bow
        //and arrow.
}
