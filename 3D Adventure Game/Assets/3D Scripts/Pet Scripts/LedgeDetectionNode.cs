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
    public float maxHitDistance = 10f; //1.75f
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
            
            //Calculate a hitDistance based on the ground normal and up direction.
            Vector3 groundNormal = pet.Motor.GroundingStatus.GroundNormal.normalized;
            Vector3 upDir = pet.Motor.CharacterUp;
            float comparison = Mathf.Abs(Vector3.Dot(groundNormal, upDir));
            //Debug.Log(comparison);
            
            //Can go up to .65 (?) for the dot product. (If max ground angle is 50)
            float difference = 1 - comparison;
            float percentage = new float();

            //If difference is 0, set to 0.
            percentage = difference != 0 ? difference / .35f : 0;

            float targetHitDistance = Mathf.Lerp(minHitDistance, maxHitDistance, percentage);

            //NOW, do some calculation to apply this lerp to a NEW lerp that changes depending on the angle of the 
            //horizontal raycast compared to the normal.
            Vector3 projectedGroundNormal = Vector3.ProjectOnPlane(groundNormal, pet.Motor.CharacterUp).normalized;
            Vector3 projectedHorizontalVector =
                Vector3.ProjectOnPlane(horizontalVectorArray[i], pet.Motor.CharacterUp).normalized;
            float normalToVectorComparison = Mathf.Abs(Vector3.Dot(projectedGroundNormal, projectedHorizontalVector));
            //Closer to 0 = shorter distance, closer to 1 = longer.
            float newHitDistance = Mathf.Lerp(minHitDistance, targetHitDistance, normalToVectorComparison); //Temp
            //Debug.Log("Array " + i + "'s distance is " + newHitDistance + ".");

            //Test if the vertical raycast doesn't detect ground within step height.
            RaycastHit horizontalHitWall;
            RaycastHit horizontalHitGround;
            RaycastHit verticalHit;
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
                  ledgeNodeBoolArray[i] = false;
                  detectionColor = Color.red;
                }
                else
                {
                    ledgeNodeBoolArray[i] = true;
                    detectionColor = Color.yellow;
                }
            }
            else
            {
                ledgeNodeBoolArray[i] = true;
                detectionColor = Color.yellow;
            }

            //Draw the raycasts
            if (i == 0)
            {
                Debug.DrawRay(petPosition, Vector3.ClampMagnitude(horizontalVectorArray[i], horizontalDetectionDistance), Color.green);
            }
            else
            {
                Debug.DrawRay(petPosition, Vector3.ClampMagnitude(horizontalVectorArray[i], horizontalDetectionDistance), Color.yellow);
            }
            
            Debug.DrawRay(verticalVectorArray[i], Vector3.down * newHitDistance, detectionColor);
            //Debug.Log(verticalVectorArray[i].magnitude);
        }
        Debug.DrawRay(petPosition, Vector3.ProjectOnPlane(pet.Motor.GroundingStatus.GroundNormal, pet.Motor.CharacterUp), Color.blue);
    }
}

//CURRENT ISSUES (TO DO):
//The raycasts won't reach approaching downward slopes. (Because the pet is technically on flat ground, making the rays small.)
//The normal of the ground changes back to straight while moving over a slope. (Again, making the rays small.)

//IDEAS:
//1. Run a second vertical raycast that extends slightly farther and looks for a ground normal. If it detects a slope,
    //could allow the first vertical raycast to count as true anyways.
//2. 
