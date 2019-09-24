using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Manages a list of AI controllers and sets permissions and actions based on what each one is doing.
public class AIGroupManager : MonoBehaviour
{
    public GameObject[] Enemies;
    void Start()
    {
        
    }
    
    void Update()
    {
        
    }
}
//Assign "actions" which will send out an int signal based on the type of action to perform.
//The actions will run checks first to see if they can be performed.

//Examples:

//Raccoon group could randomly assign x amount of enemies to make melee attacks a priority. Once the x amount has been
//reached, the other raccoons won't perform a melee attack and will instead jump, throw rocks, or remain idle.

//Large enemy checks if a raccoon enemy exists in the group. If so, performs a special interaction with that enemy.

//Raccoon group checks if enemy position is within x distance. If so, special action x is set to true. If a raccoon is
//within 2m of another raccoon, set special action 3 to be true. (In this case, talking). Then the signal is sent back
//to the associated gameobject.