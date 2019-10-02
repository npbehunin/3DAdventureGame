using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KinematicCharacterController;
using KinematicCharacterController.Examples;
using KinematicCharacterController.Nate;
using KinematicCharacterController.Raccoonv2;

//Script that handles the Player's inputs.
namespace KinematicCharacterController.Nate
{
    public class RaccoonInputs : MonoBehaviour
    {
        public RaccoonControllerv2 Character;
//
        //private const string HorizontalInput = "Horizontal";
        //private const string VerticalInput = "Vertical";
        
        private void Update()
        {
            HandleCharacterInput();
        }

        private void HandleCharacterInput()
        {
            RaccoonCharacterInputs characterInputs = new RaccoonCharacterInputs();

            // Build the CharacterInputs struct
            //characterInputs.MoveAxisForward = 1f;
            //characterInputs.MoveAxisRight = Input.GetAxisRaw(HorizontalInput);

            // Apply inputs to character
            Character.SetInputs(ref characterInputs);
        }
    }
}

//(Ignoring this script for now until needed.)