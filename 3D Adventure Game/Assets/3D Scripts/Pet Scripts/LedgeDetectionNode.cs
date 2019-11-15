using System.Collections;
using System.Collections.Generic;
using KinematicCharacterController.PetControllerv3;
using UnityEngine;

public class LedgeDetectionNode : MonoBehaviour
{
    //Ledge detection testing
    private Vector3[] ledgeNodeVectorArray;
    private bool[] ledgeNodeBoolArray;
    public float horizontalDistance = .65f;
    public float stepHeight = .55f; //Step height
    public float maxAngle = 50f;
    public LayerMask colliderMask;
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
        Vector3[] backwardsVectorArray = new Vector3[8];
        Vector3[] secondBackwardsVectorArray = new Vector3[8];
        ledgeNodeBoolArray = new bool[8];

        //Set the directions the raycasts should extend out.
        for (int i = 0; i < horizontalVectorArray.Length; i++)
        {
            //Send out a direction
            if (i == 0)
            {
                horizontalVectorArray[0] = Vector3.ClampMagnitude(petPosition + Vector3.forward, horizontalDistance);
                verticalVectorArray[0] = petPosition + horizontalVectorArray[0];
                backwardsVectorArray[0] =
                    verticalVectorArray[0] + Vector3.ClampMagnitude(Vector3.down, stepHeight);
                secondBackwardsVectorArray[0] = verticalVectorArray[0] + Vector3.ClampMagnitude(Vector3.down, stepHeight / 2);
            }
            else
            {
                horizontalVectorArray[i] = (Vector3.ClampMagnitude(Vector3.Slerp(horizontalVectorArray[i - 1],
                    Vector3.Cross(horizontalVectorArray[i - 1], Vector3.up), .5f), horizontalDistance));
                verticalVectorArray[i] = petPosition + horizontalVectorArray[i];
                backwardsVectorArray[i] =
                    verticalVectorArray[i] + Vector3.ClampMagnitude(Vector3.down, stepHeight);
                secondBackwardsVectorArray[i] = verticalVectorArray[i] + Vector3.ClampMagnitude(Vector3.down, stepHeight / 2);
            }
            
            

            //Test if the vertical raycast doesn't detect ground within step height.
            RaycastHit hit;
            RaycastHit hit2;
            Color detectionColor = new Color();

            //Send out a raycast away from the pet, then straight down, then back to the pet.
            bool horizontalDetection = Physics.Raycast(petPosition, horizontalVectorArray[i],
                    horizontalDistance, colliderMask);
            bool verticalDetection = Physics.Raycast(verticalVectorArray[i], Vector3.down,
                    stepHeight, colliderMask);
            bool backwardsDetection = Physics.Raycast(backwardsVectorArray[i], -horizontalVectorArray[i],
                out hit, horizontalDistance, colliderMask);
            bool secondBackwardsDetection = Physics.Raycast(secondBackwardsVectorArray[i], -horizontalVectorArray[i],
                out hit2, horizontalDistance, colliderMask);

            //If the horizontal or vertical ray detect something, return true. Otherwise, check the backwards raycast's
                //collision normal. If it's within the max angle, return true.
            if (!horizontalDetection)
            {
                if (!verticalDetection)
                {
                    bool angle1Allowed = false;
                    bool angle2Allowed = false;
                    if (backwardsDetection)
                    {
                        float angle = Vector3.Angle(pet.Motor.CharacterUp, hit.normal);
                        if (angle < maxAngle)
                        {
                            angle1Allowed = true;
                        }
                    }
                    
                    if (secondBackwardsDetection)
                    {
                        float angle = Vector3.Angle(pet.Motor.CharacterUp, hit2.normal);
                        if (angle < maxAngle)
                        {
                            angle1Allowed = true;
                        };
                    }

                    if (angle1Allowed || angle2Allowed)
                    {
                        ledgeNodeBoolArray[i] = true;
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
                Debug.DrawRay(petPosition, Vector3.ClampMagnitude(horizontalVectorArray[i], horizontalDistance), Color.green);
            }
            else
            {
                Debug.DrawRay(petPosition, Vector3.ClampMagnitude(horizontalVectorArray[i], horizontalDistance), Color.yellow);
            }
            
            Debug.DrawRay(verticalVectorArray[i], Vector3.down * stepHeight, detectionColor); //Vertical raycast
            Debug.DrawRay(backwardsVectorArray[i], -horizontalVectorArray[i], detectionColor); //Backwards raycast
            Debug.DrawRay(secondBackwardsVectorArray[i], -horizontalVectorArray[i], detectionColor); //Second backwards raycast
        }
    }
}

//SMALL ISSUES:
//Both raycasts will still return false in the case that the pet is hanging off the edge on a downward 
    //slope (because the height of the pet is higher than the actual step height of the ledge).

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
