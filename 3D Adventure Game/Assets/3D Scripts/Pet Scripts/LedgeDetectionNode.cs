using System.Collections;
using System.Collections.Generic;
using KinematicCharacterController.PetControllerv3;
using UnityEngine;

public class LedgeDetectionNode : MonoBehaviour
{
    //Ledge detection testing
    private Vector3[] ledgeNodeVectorArray;
    private bool[] ledgeNodeBoolArray;
    public float hitDistance = .65f;
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
                horizontalVectorArray[0] = Vector3.ClampMagnitude((petPosition + Vector3.forward), hitDistance);
                verticalVectorArray[0] = petPosition + horizontalVectorArray[0];
            }
            else
            {
                horizontalVectorArray[i] = Vector3.ClampMagnitude(Vector3.Slerp(horizontalVectorArray[i - 1],
                    Vector3.Cross(horizontalVectorArray[i - 1], Vector3.up), .5f), hitDistance);
                verticalVectorArray[i] = petPosition + horizontalVectorArray[i];
            }

            //Test if the vertical raycast doesn't detect ground within step height.
            RaycastHit horizontalHitWall;
            RaycastHit horizontalHitGround;
            RaycastHit verticalHit;
            Color detectionColor = new Color();
            
            bool horizontalDetection = Physics.Raycast(petPosition, horizontalVectorArray[i], 
                out horizontalHitGround, hitDistance, groundMask) || Physics.Raycast(petPosition, horizontalVectorArray[i], out horizontalHitWall, hitDistance, wallMask);
            bool verticalDetection = Physics.Raycast(verticalVectorArray[i], Vector3.down,
                out verticalHit, hitDistance, groundMask);
            
            //Check to make sure there's no wall or ground detection horizontally before checking vertically.
                //(Prevents checking a vertical raycast inside the ground or in wall.)
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
            Debug.DrawRay(petPosition, Vector3.ClampMagnitude(horizontalVectorArray[i], hitDistance), Color.yellow);
            Debug.DrawRay(verticalVectorArray[i], Vector3.ClampMagnitude(Vector3.down, hitDistance), detectionColor);
        }
    }
}

//TO DO:
// Change the length of the rays to reflect the 
