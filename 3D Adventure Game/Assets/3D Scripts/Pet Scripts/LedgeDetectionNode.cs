using System.Collections;
using System.Collections.Generic;
using KinematicCharacterController;
using KinematicCharacterController.PetControllerv3;
using UnityEngine;

public class LedgeDetectionNode : MonoBehaviour
{
    //Character settings
    [Header("Character settings")]
    public float maxStepHeight = .55f; //Step height
    public float maxGroundAngle = 50f;
    public LayerMask colliderMask;
    public KinematicCharacterMotor Motor;
    private Vector3 transformPosition;
    
    //Ledge detection
    [Header("Ledge detection")]
    public bool CheckLedges;
    public float distanceToLedge = .65f;
    private Vector3[] ledgeNodeVectorArray;
    private bool[] ledgeNodeBoolArray;
    private bool _ledgeDetected;
    private Vector3 directionToLedge;

    //Wall detection
    [Header("Wall detection")]
    public bool CheckWalls;
    private bool _wallDetected;
    private Vector3 wallNormal;
    
    //Slope detection
    [Header("Slope detection")]
    public bool CheckSlopes;
    private bool _slopeDetected;
    void Update()
    {
        transformPosition = Motor.transform.position;
        if (CheckLedges)
        {
            LedgeDetection();
        }

        if (CheckWalls)
        {
            WallDetection();
        }

        if (CheckSlopes)
        {
            SlopeDetection();
        }
    }
    
    private void LedgeDetection()
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
                horizontalVectorArray[0] = Vector3.ClampMagnitude(transformPosition + Vector3.forward, distanceToLedge);
                verticalVectorArray[0] = transformPosition + horizontalVectorArray[0];
                backwardsVectorArray[0] =
                    verticalVectorArray[0] + Vector3.ClampMagnitude(Vector3.down, maxStepHeight);
                secondBackwardsVectorArray[0] = verticalVectorArray[0] + Vector3.ClampMagnitude(Vector3.down, maxStepHeight / 2);
            }
            else
            {
                horizontalVectorArray[i] = (Vector3.ClampMagnitude(Vector3.Slerp(horizontalVectorArray[i - 1],
                    Vector3.Cross(horizontalVectorArray[i - 1], Vector3.up), .5f), distanceToLedge));
                verticalVectorArray[i] = transformPosition + horizontalVectorArray[i];
                backwardsVectorArray[i] =
                    verticalVectorArray[i] + Vector3.ClampMagnitude(Vector3.down, maxStepHeight);
                secondBackwardsVectorArray[i] = verticalVectorArray[i] + Vector3.ClampMagnitude(Vector3.down, maxStepHeight / 2);
            }
            
            

            //Test if the vertical raycast doesn't detect ground within step height.
            RaycastHit hit;
            RaycastHit hit2;
            Color detectionColor = new Color();

            //Send out a raycast away from the pet, then straight down, then back to the pet.
            bool horizontalDetection = Physics.Raycast(transformPosition, horizontalVectorArray[i],
                    distanceToLedge, colliderMask);
            bool verticalDetection = Physics.Raycast(verticalVectorArray[i], Vector3.down,
                    maxStepHeight, colliderMask);
            bool backwardsDetection = Physics.Raycast(backwardsVectorArray[i], -horizontalVectorArray[i],
                out hit, distanceToLedge, colliderMask);
            bool secondBackwardsDetection = Physics.Raycast(secondBackwardsVectorArray[i], -horizontalVectorArray[i],
                out hit2, distanceToLedge, colliderMask);

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
                        float angle = Vector3.Angle(Motor.CharacterUp, hit.normal);
                        if (angle < maxGroundAngle)
                        {
                            angle1Allowed = true;
                        }
                    }
                    
                    if (secondBackwardsDetection)
                    {
                        float angle = Vector3.Angle(Motor.CharacterUp, hit2.normal);
                        if (angle < maxGroundAngle)
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
                Debug.DrawRay(transformPosition, Vector3.ClampMagnitude(horizontalVectorArray[i], distanceToLedge), Color.green);
            }
            else
            {
                Debug.DrawRay(transformPosition, Vector3.ClampMagnitude(horizontalVectorArray[i], distanceToLedge), Color.yellow);
            }
            
            Debug.DrawRay(verticalVectorArray[i], Vector3.down * maxStepHeight, detectionColor); //Vertical raycast
            Debug.DrawRay(backwardsVectorArray[i], -horizontalVectorArray[i], detectionColor); //Backwards raycast
            Debug.DrawRay(secondBackwardsVectorArray[i], -horizontalVectorArray[i], detectionColor); //Second backwards raycast
        }
    }

    private void WallDetection()
    {
        
    }

    private void SlopeDetection()
    {
        
    }
}

//TO DO:
//1. Have the ledge detection return a vector3 that points from the transformPosition to the average direction

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
