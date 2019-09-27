﻿using System.Collections;
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
        
                //STATE TRANSITIONS

        //1. Start in a default state. This state can be sitting (around a campfire), talking, patrolling, or sleeping.
        //(Maybe incorporate random movement, such as leaving a sitting state and going into patrolling or talking.)
    
        //2. Run detection checks through update:
        //Line of sight through the enemy's head joint, spread out over a specified angle.
        //"Audio" detection. (If the player is running by close or if an object lands nearby.)
    
        //3. Activate detection.
        //Transition into the "alerted" state.
        //If no eye contact is made with the player, let them investigate the area by moving to the sound point.
        //If eye contact is made, the enemy will alert other nearby enemies.
    
        //4. Running/Following.
        //Transition into the running state and move towards the player.
        //Once near the player, can randomly reposition themselves near the player (sidestepping).
        //Will wait for a random timer and remain in the idle state before attacking.
    
        //4. Attack the player.
        //Enemies with ranged weapons "bow and arrows" will start scoping.
        //Enemies will keep track of each other in the group. Enemies will find open spots near the player to attack,
        //and will attack at random intervals.
        //Any "targeted" enemies will prioritize attacking the player first.
    
        //5. Dying.
        //Dead enemies will remove themselves from the "group".
    
        //6. Stun.
        //Stunned enemies can't attack.
        //After leaving a stun, the enemy will attack faster (shorter coroutine).
    
        //7. Knocked.
        //Enemies will be knocked from specific attacks. (Ex: Third swing combo)
        //They will fly backwards a short distance opposite of the attack direction.
        //Enemies are invincible while knocked.
    
        //8. Bow and arrow (tower enemies)
        //Won't follow normal movement patterns of a sword enemy
        //Will stand in place and just shoot a bow and arrow.
        //Have farther player detection.
        //Can be placed up higher in a tower.
        //Can't be knocked.
        //Can die from their tower being knocked down.
    
        //CODE INTEGRATION
    
        //1. Enemy groups.
        //Manually put enemies into an array in the inspector that defines their "group".
        //The group keeps track of enemy distances and moves them to open areas around the player.
        //Enemies within the group can alert each other of the player if eye contact is made.
        //Enemy scouts can sound off a horn to alert the other enemies.
    
        //2. Weapon equips.
        //Script will check what type of weapon is equipped in its equip manager.
        //If bow and arrow, will set itself to the bow and arrow movement type.
        //If sword, does it likewise.
    
        //3. Pathfinding.
        //Enemy will use pathfinding if detected is true.
        //If line of sight isn't met, enemy will move to the last seen player spot.
        //If line of sight is still met, enemy will move until the height difference is close enough.
    
}