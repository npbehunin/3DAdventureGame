using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//These functions create acceleration between input and output velocity. (Input should be the target velocity, and
//output is the current velocity.)

//This is an alternative to the smooth velocity calculation on the default character controller script, but this
//method of acceleration feels more "stiff", especially when making sharp turns, and that's because the acceleration
//is very linear compared to the other controller which has a very fast initial accel and decel, and it slows down
//as it reaches the target.
public class CharacterControllerTestAccel : MonoBehaviour
{
    public float MaxStableMoveSpeed;
    public float _accelerationSpeed;

    //Get acceleration for each value of the smoothed velocity.
    private Vector3 GetSmoothVelocity(Vector3 inputVelocity, Vector3 outputVelocity, float deltaTime)
    {
        return new Vector3(CalculateAccel(inputVelocity.x, outputVelocity.x, deltaTime),
            CalculateAccel(inputVelocity.y, outputVelocity.y, deltaTime),
            CalculateAccel(inputVelocity.z, outputVelocity.z, deltaTime));
    }

    //Calculate acceleration between two floats.
    private float CalculateAccel(float inputX, float outputX, float deltaTime)
    {
        //outputX ~= currentVelocity.
        float percent = (MaxStableMoveSpeed * _accelerationSpeed);
        float lerpValue = new float();

        if (outputX < inputX)
        {
            lerpValue = percent / (inputX - outputX) * deltaTime;
            outputX = Mathf.Lerp(outputX, inputX, lerpValue);
        }

        if (outputX > inputX)
        {
            lerpValue = 1 - (percent / (outputX - inputX) * deltaTime);
            outputX = Mathf.Lerp(inputX, outputX, lerpValue);
        }

        if (lerpValue > 1)
            lerpValue = 1;
        if (lerpValue < 0)
            lerpValue = 0;

        return outputX;
    }
    
    //Use this in UpdateVelocity:
    //currentVelocity = GetSmoothVelocity(targetMovementVelocity, currentVelocity, deltaTime);
    
    //Lerp between each individual part of the input and output velocity. For example, "If outputX > inputX, do a lerp
    //between outputX and inputX, at a lerp rate of acceleration. Then opposite if outputX < inputX. Then do the same for each
    //value.
    //(This SHOULD work correctly, just like a Vector3.Lerp, but it allows the function to run checks to see if "value" is
    //greater than/less than "value"
    //This replaces the "sharpness" of the player so acceleration and deceleration doesn't account for magnitude.
}
