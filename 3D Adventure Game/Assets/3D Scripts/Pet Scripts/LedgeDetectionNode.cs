using System.Collections;
using System.Collections.Generic;
using KinematicCharacterController.PetControllerv3;
using UnityEngine;

public class LedgeDetectionNode : MonoBehaviour
{
    //Ledge detection testing
    private Vector3[] ledgeNodeVectorArray;
    private bool[] ledgeNodeBoolArray;
    public float horizontalDetectionDistance = .65f;
    public float minHitDistance = .65f;
    public float maxHitDistance = 1.75f; //1.75f
    public LayerMask groundMask;
    public LayerMask wallMask;
    public PetControllerv3 pet;
    public Vector3 petPosition;

    void Update()
    {
        TestLedgeDetection();
        petPosition = pet.Motor.transform.position;
    }
    
    private void TestLedgeDetection()
    {
        //Send raycasts out.
        int raycastCount = 8;
        Vector3[] horizontalVectorArray = new Vector3[8];
        Vector3[] verticalVectorArray = new Vector3[8];
        ledgeNodeBoolArray = new bool[8];
        
        //Set the first vector.
        
        
        //Set the directions the raycasts should extend out.
        for (int i = 0; i < horizontalVectorArray.Length; i++)
        {
            //Send out a direction
            if (i == 0)
            {
                horizontalVectorArray[0] = Vector3.ClampMagnitude((petPosition + Vector3.forward), horizontalDetectionDistance);
                verticalVectorArray[0] = petPosition + horizontalVectorArray[0];
            }
            else
            {
                horizontalVectorArray[i] = Vector3.ClampMagnitude(Vector3.Slerp(horizontalVectorArray[i - 1],
                    Vector3.Cross(horizontalVectorArray[i - 1], Vector3.up), .5f), horizontalDetectionDistance);
                verticalVectorArray[i] = petPosition + horizontalVectorArray[i];
            }

            Vector3 normalFromCollider = pet.Motor.GroundingStatus.GroundNormal.normalized;
            float targetHitDistanceLerp = LerpValueFromGroundNormal(normalFromCollider);
            float targetHitDistance = Mathf.Lerp(minHitDistance, maxHitDistance, targetHitDistanceLerp);

            //NOW, take the targetHitDistance (which changes ALL the lengths of the verticalVectors depending on the
                //angle of the ground normal) and apply a new distance individually depending on the dot product of the
                //direction the normal is facing compared to the horizontalVector.)
            Vector3 projectedGroundNormal = Vector3.ProjectOnPlane(normalFromCollider, pet.Motor.CharacterUp).normalized;
            Vector3 projectedHorizontalVector =
                Vector3.ProjectOnPlane(horizontalVectorArray[i], pet.Motor.CharacterUp).normalized;
            float normalToVectorComparison = Mathf.Abs(Vector3.Dot(projectedGroundNormal, projectedHorizontalVector));
            //Closer to 0 = shorter distance, closer to 1 = longer.
            float newHitDistance = Mathf.Lerp(minHitDistance, targetHitDistance, normalToVectorComparison); //Temp

            //Test if the vertical raycast doesn't detect ground within step height.
            RaycastHit horizontalHitWall;
            RaycastHit horizontalHitGround;
            RaycastHit verticalHit;
            RaycastHit groundHit;
            RaycastHit extraVerticalHit;
            Color detectionColor = new Color();

            //Check to make sure there's no wall or ground detection horizontally before checking vertically.
                //(Prevents checking a vertical raycast inside the ground or in wall.)
            bool horizontalDetection = Physics.Raycast(petPosition, horizontalVectorArray[i], 
                                           out horizontalHitGround, horizontalDetectionDistance, groundMask) || Physics.Raycast(petPosition, horizontalVectorArray[i], 
                                           out horizontalHitWall, horizontalDetectionDistance, wallMask);
            bool verticalDetection = Physics.Raycast(verticalVectorArray[i], Vector3.down,
                    out verticalHit, newHitDistance, groundMask);
            if (!horizontalDetection)
            {
                if (!verticalDetection)
                {
                    //Check if there's ground below, and if so, get a lerp value from its normal.
                    if (Physics.Raycast(verticalVectorArray[i], Vector3.down,
                        out groundHit, Mathf.Infinity, groundMask))
                    {
                        float extraRaycastLerp = LerpValueFromGroundNormal(groundHit.normal.normalized);
                        //Debug.Log(extraRaycastLerp);
                        float extraDistanceCheck = Mathf.Lerp(newHitDistance, newHitDistance + 1f, extraRaycastLerp);
                        //Debug.Log(extraDistanceCheck);
                        Debug.DrawRay(verticalVectorArray[i], Vector3.down * (extraDistanceCheck), Color.blue); //Extra vertical raycast

                        //If the ground is detected within its allowed extra distance...
                        if (Physics.Raycast(verticalVectorArray[i], Vector3.down,
                            out extraVerticalHit, extraDistanceCheck, groundMask))
                        {
                            //Ground is within its extra distance.
                            ledgeNodeBoolArray[i] = true;
                        }
                        else
                        {
                            //Ground isn't within its extra distance.
                            ledgeNodeBoolArray[i] = false;
                            Debug.Log("False");
                        }
                    }
                    else
                    {
                        //No ground detected at all.
                        ledgeNodeBoolArray[i] = false;
                    }
                }
                else
                {
                    //Ground was detected vertically. True.
                    ledgeNodeBoolArray[i] = true;
                }
            }
            else
            {
                //Ground or wall was detected horizontally. True.
                ledgeNodeBoolArray[i] = true;
            }
            
            detectionColor = ledgeNodeBoolArray[i] ? Color.yellow : Color.red;

                //Draw the raycasts
            if (i == 0)
            {
                Debug.DrawRay(petPosition, Vector3.ClampMagnitude(horizontalVectorArray[i], horizontalDetectionDistance), Color.green);
            }
            else
            {
                Debug.DrawRay(petPosition, Vector3.ClampMagnitude(horizontalVectorArray[i], horizontalDetectionDistance), Color.yellow);
            }
            
            Debug.DrawRay(verticalVectorArray[i], Vector3.down * newHitDistance, detectionColor); //Vertical raycast
            //Debug.Log(verticalVectorArray[i].magnitude);
        }
        //Debug.DrawRay(petPosition, Vector3.ProjectOnPlane(pet.Motor.GroundingStatus.GroundNormal, pet.Motor.CharacterUp), Color.blue);
    }

    private float LerpValueFromGroundNormal(Vector3 groundNormal)
    {
        //Calculate a hitDistance based on the ground normal and up direction.
        Vector3 upDir = pet.Motor.CharacterUp;
        float comparison = Mathf.Abs(Vector3.Dot(groundNormal, upDir));
        //Debug.Log(comparison);
            
        //Can go up to ~.65 (?) for the dot product. (If max ground angle is 50)
        float difference = 1 - comparison;
        float lerpValue = new float();

        //If difference is 0, set to 0. A dot product of ~.65 (or a difference of .35) will return 1.
        //anything past 1 doesn't matter if the value is being used for a lerp.
        lerpValue = difference != 0 ? difference / .35f : 0;
        return lerpValue;
    }
}

//CURRENT ISSUES (TO DO):
//The raycasts won't reach approaching downward slopes. (Because the pet is technically on flat ground, making the rays small.)
//The normal of the ground changes back to straight while moving over a slope. (Again, making the rays small.)

//TO DO: LEDGE DETECTION
//1. Add a second vertical raycast that extends past the first one until it hits ground.
    //Get the normal of the ground up until the sliding point (50-90 degrees)
    //Create an float for the extra distance allowed during the check. This distance gets bigger depending on the normal
    //of the ground.
    //"If the first vertical raycast is false, send the second. If the second extra distance collides, return true.

//TO DO: WALL DETECTION
//1. Send a raycast forward from the pet's velocity up just past stepping height. If it hits a wall, return true for the pet.
//2. Send a raycast forward from the player's velocity (longer) up just past stepping height. If it hits a wall, return true for the player.
//3. Check both. If the player's is false but the pet's is true, the pet will move towards the player.

//TO DO: SLOPE DETECTION
//1. Send a raycast forward from the pet's velocity up just below stepping height. Send a second raycast down at the
    //end of the first raycast and check the ground normal.
//2. Similar method for the player.
//3. Check the distance between the pet and player raycast hitpoint. If the distance becomes too big, move towards
    //the player.

//OPTIMIZATION
//We can remove the different types of raycastHits and just use one hit that changes for each check.
//We could try combining the yellow (first) raycast with the second (blue) raycast.
