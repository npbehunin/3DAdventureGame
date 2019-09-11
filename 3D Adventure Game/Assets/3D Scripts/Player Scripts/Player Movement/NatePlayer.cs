using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KinematicCharacterController;
using KinematicCharacterController.Examples;
using KinematicCharacterController.Nate;

//Script that handles the Player's inputs.
namespace KinematicCharacterController.Nate
{
    public class NatePlayer : MonoBehaviour
    {
        //public ExampleCharacterCamera OrbitCamera;
       // public Transform CameraFollowPoint;
        public NateCharacterController Character;

       // private const string MouseXInput = "Mouse X";
       // private const string MouseYInput = "Mouse Y";
       // private const string MouseScrollInput = "Mouse ScrollWheel";
        private const string HorizontalInput = "Horizontal";
        private const string VerticalInput = "Vertical";
        
        private void Update()
        {
            //if (Input.GetMouseButtonDown(0))
            //{
            //    Cursor.lockState = CursorLockMode.Locked;
            //}
//
            //HandleCameraInput();
            HandleCharacterInput();
        }

        private void HandleCharacterInput()
        {
            PlayerCharacterInputs characterInputs = new PlayerCharacterInputs();

            // Build the CharacterInputs struct
            characterInputs.MoveAxisForward = Input.GetAxisRaw(VerticalInput);
            characterInputs.MoveAxisRight = Input.GetAxisRaw(HorizontalInput);
           // characterInputs.CameraRotation = OrbitCamera.Transform.rotation;
            //characterInputs.JumpDown = Input.GetKeyDown(KeyCode.Space);
            characterInputs.CrouchDown = Input.GetKey(KeyCode.LeftControl);
            characterInputs.CrouchUp = Input.GetKeyUp(KeyCode.LeftControl);
            characterInputs.SwordSwing = Input.GetMouseButtonDown(0);
            characterInputs.ToggleTargetingMode = Input.GetKeyDown(KeyCode.Space);

            // Apply inputs to character
            Character.SetInputs(ref characterInputs);
        }
    }
}