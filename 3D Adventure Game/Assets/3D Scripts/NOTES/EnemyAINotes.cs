using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAINotes
{
    //-----MOVEMENT TYPES-----
    
    //REMEMBER! IT'S MORE IMPORTANT TO HAVE FEW ENEMIES WITH FLESHED OUT AI THAN MORE ENEMIES!
    //Breath of the wild has VERY few enemies, but they're very solid.
    
    //Breath of the Wild enemy movement types:
    //1. Bokoblin. Very simple ground AI. Mostly swings swords, but can throw rocks and shoot projectiles.
    //2. Moblin. Larger, slower, but do more damage with larger weapons
    //3. Lizalfos. Deals more ranged attacks. Faster. Sneakier.
    //4. Chuchu. Appears without warning. Can have different elemental effects.
    //5. Lynel. The "minibosses" of the open plains.
    //6. Octorok. Chill in the water, where the player has a hard time attacking. Can die by arrows or parrying.
    //7. Wizzrobe. Hard to hit, move erratically,  and spam annoying moves.
    
    //Think of good enemy types similar to those:
    //1. Grounded AI. "Bokoblin" movement and acting.
    //2. Larger grounded AI. Slower and clumsier, but more dangerous.
    //3. Grounded "sneaky" AI. Deals more ranged damage.
    //4. Slimes. Dumb and slow, but can jump quickly.
    //5. Sentries. Long range, covers areas that make it hard to reach.
    //6. "Trap" enemies. Match the environment, and can pop out without warning.
    
    //-----PLAYER DETECTION AND PATHFINDING-----

    //1: Enemy will check the angle of the current motor.forward direction and compare it to the actual linecast
    //direction of the player. (This linecast checks actual eye contact with the player) If the angle isn't within x
    //degrees (maybe 85 or so?) the enemy will remain idle. Once the angle is within that range, then detected is true.

    //While the player is detected...

    //2: The enemy will start pathfinding. Once the enemy reaches the end of the path and still doesn't have direct line
    //of sight of the player, the enemy will wait for a moment before returning to its home position.
    
    //3: If the player cannot be reached, the enemy will throw things. (If throwing is possible)
    
    //(Think breath of the wild enemies)
    
    //4: The enemy will need to know when to not follow pathfinding and instead go directly towards the player. This is
    //because the enemy will walk towards the path point where the player is, but the player can move away before
    //pathfinding updates the positions again.
    
    //TO CHECK HEIGHT...
    //(To calculate height, we can take the direction between the player's transform and the enemy's transform and lay
    //it flat (VectorToPlane). Then, we take the player's up and down direction and return the point where the flat
    //direction and the player's vertical direction intersect. Then we can simply run a check to see how high that
    //point is to the player.)
    
    //-----ENEMY VARIATION-----
    
    //To add variation without too much extra work, include harder "tiers" for a few of the more interesting AI.
    //For example, for the "bokoblin" type, a harder tier does more damage and performs a new attack.
    //A sentry could shoot fire instead of projectiles.
    //A slime could explode upon dying.
    
    //At least 1 or 2 new enemy types should appear per temple.
    //At least 1 new miniboss per temple.
    //1 real boss per temple.
}
