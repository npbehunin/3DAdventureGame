using System.Collections;
using System.Collections.Generic;
using KinematicCharacterController.PetControllerv3;
using UnityEngine;

public class LedgeDetectionNode : MonoBehaviour
{
    //Ledge detection testing
    private Vector3[] ledgeNodeVectorArray;
    private bool[] ledgeNodeBoolArray;
    public float horizontalDetectionDistance;
    public float minHitDistance = .65f;
    public float maxHitDistance = 1.75f;
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
            //Calculate a hitDistance based on the ground normal and up direction.
            Vector3 groundNormal = pet.Motor.GroundingStatus.GroundNormal.normalized;
            Vector3 upDir = pet.Motor.CharacterUp;
            float comparison = Mathf.Abs(Vector3.Dot(groundNormal, upDir));
            Debug.Log(comparison);
            //Can go up to .65 (?) for the dot product. (If max ground angle is 50)
            float percentage = 
            float newHitDistance = Mathf.Lerp(minHitDistance, maxHitDistance,)
            
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

            //Test if the vertical raycast doesn't detect ground within step height.
            RaycastHit horizontalHitWall;
            RaycastHit horizontalHitGround;
            RaycastHit verticalHit;
            Color detectionColor = new Color();

            //Check to make sure there's no wall or ground detection horizontally before checking vertically.
                //(Prevents checking a vertical raycast inside the ground or in wall.)
            bool horizontalDetection = Physics.Raycast(petPosition, horizontalVectorArray[i], 
                                           out horizontalHitGround, .65f, groundMask) || Physics.Raycast(petPosition, horizontalVectorArray[i], out horizontalHitWall, horizontalDetectionDistance, wallMask);
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
            Debug.DrawRay(petPosition, Vector3.ClampMagnitude(horizontalVectorArray[i], horizontalDetectionDistance), Color.yellow);
            Debug.DrawRay(verticalVectorArray[i], Vector3.ClampMagnitude(Vector3.down, newHitDistance), detectionColor);
        }
    }
}

//TO DO:
// Change the length of the rays to reflect the 
