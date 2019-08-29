﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KinematicCharacterController;
using System;

namespace KinematicCharacterController.Nate
{
    public enum CharacterState
    {
        Default,
    }

    public enum OrientationMethod
    {
        TowardsCamera,
        TowardsMovement,
    }

    public struct PlayerCharacterInputs
    {
        public float MoveAxisForward;
        public float MoveAxisRight;
        public Quaternion CameraRotation;
        public bool JumpDown;
        public bool CrouchDown;
        public bool CrouchUp;
    }

    public struct AICharacterInputs
    {
        public Vector3 MoveVector;
        public Vector3 LookVector;
    }

    public class NateCharacterController : MonoBehaviour, ICharacterController
    {
        public KinematicCharacterMotor Motor;

        [Header("Stable Movement")]
        public float MaxStableMoveSpeed = 10f;
        public float StableMovementSharpness = 15f;
        public float OrientationSharpness = 10f;
        public OrientationMethod OrientationMethod = OrientationMethod.TowardsCamera;

        [Header("Air Movement")]
        public float MaxAirMoveSpeed = 100f;
        public float AirAccelerationSpeed = 15f;
        public float Drag = 0.1f;

        [Header("Jumping")]
        public bool AllowJumpingWhenSliding = false;
        public float JumpUpSpeed = 10f;
        public float JumpScalableForwardSpeed = 10f;
        public float JumpPreGroundingGraceTime = 0f;
        public float JumpPostGroundingGraceTime = 0f;

        [Header("Misc")]
        public List<Collider> IgnoredColliders = new List<Collider>();
        public bool OrientTowardsGravity = false;
        public Vector3 Gravity = new Vector3(0, -30f, 0);
        public Transform MeshRoot;
        public Transform CameraFollowPoint;

        public CharacterState CurrentCharacterState { get; private set; }

        private Collider[] _probedColliders = new Collider[8];
        private Vector3 _moveInputVector;
        private Vector3 _lookInputVector;
        //private Vector3 _targetVector;
        private bool _jumpRequested = false;
        private bool _jumpConsumed = false;
        private bool _jumpedThisFrame = false;
        private float _timeSinceJumpRequested = Mathf.Infinity;
        private float _timeSinceLastAbleToJump = 0f;
        public float _accelerationSpeed = .95f;
        public float _decelerationSpeed = .95f;
        private Vector3 _internalVelocityAdd = Vector3.zero;
        private bool _shouldBeCrouching = false;
        private bool _isCrouching = false;

        private Vector3 lastInnerNormal = Vector3.zero;
        private Vector3 lastOuterNormal = Vector3.zero;

        private void Start()
        {
            //Handle initial state
            TransitionToState(CharacterState.Default);

            // Assign the characterController to the motor
            Motor.CharacterController = this;
        }

        private void Update()
        {
           // CalculateAccel()
        }

        /// <summary>
        /// Handles movement state transitions and enter/exit callbacks
        /// </summary>
        public void TransitionToState(CharacterState newState)
        {
            CharacterState tmpInitialState = CurrentCharacterState; //Get current state.
            OnStateExit(tmpInitialState, newState); //Do the OnStateExit stuff from current state to new state.
            CurrentCharacterState = newState; //Current state = new state.
            OnStateEnter(newState, tmpInitialState); //Do the OnStateEnter stuff to new state from the last state.
        }

        /// <summary>
        /// Event when entering a state
        /// </summary>
        public void OnStateEnter(CharacterState state, CharacterState fromState)
        {
            switch (state)
            {
                case CharacterState.Default:
                    {
                        break;
                    }
            }
        }

        /// <summary>
        /// Event when exiting a state
        /// </summary>
        public void OnStateExit(CharacterState state, CharacterState toState)
        {
            switch (state)
            {
                case CharacterState.Default:
                    {
                        break;
                    }
            }
        }

        /// <summary>
        /// This is called every frame by [NatePlayer] in order to tell the character what its inputs are
        /// </summary>
        public void SetInputs(ref PlayerCharacterInputs inputs)
        {
            // Clamp input (in this case we just do it by 1f)
            Vector3 moveInputVector = Vector3.ClampMagnitude(new Vector3(inputs.MoveAxisRight, 0f, inputs.MoveAxisForward), 1f);

            // Calculate camera direction and rotation on the character plane
            Vector3 cameraPlanarDirection = Vector3.ProjectOnPlane(inputs.CameraRotation * Vector3.forward, Motor.CharacterUp).normalized;
            if (cameraPlanarDirection.sqrMagnitude == 0f)
            {
                cameraPlanarDirection = Vector3.ProjectOnPlane(inputs.CameraRotation * Vector3.up, Motor.CharacterUp).normalized;
            }
            Quaternion cameraPlanarRotation = Quaternion.LookRotation(cameraPlanarDirection, Motor.CharacterUp);

            switch (CurrentCharacterState)
            {
                case CharacterState.Default:
                    {
                        // Move and look inputs
                        _moveInputVector = cameraPlanarRotation * moveInputVector;

                        switch (OrientationMethod)
                        {
                            case OrientationMethod.TowardsCamera:
                                _lookInputVector = cameraPlanarDirection;
                                break;
                            case OrientationMethod.TowardsMovement:
                                _lookInputVector = _moveInputVector.normalized;
                                break;
                        }

                        // Jumping input
                        //if (inputs.JumpDown)
                        //{
                        //    _timeSinceJumpRequested = 0f;
                        //    _jumpRequested = true;
                        //}

                        // Crouching input
                        if (inputs.CrouchDown)
                        {
                            _shouldBeCrouching = true;

                            if (!_isCrouching)
                            {
                                _isCrouching = true;
                                Motor.SetCapsuleDimensions(0.5f, 1f, 0.5f); //Scales the hitbox.
                                MeshRoot.localScale = new Vector3(1f, 0.5f, 1f); //Scales the mesh root.
                            }
                        }
                        else if (inputs.CrouchUp)
                        {
                            _shouldBeCrouching = false;
                        }

                        break;
                    }
            }
        }

        /// <summary>
        /// This is called every frame by the AI script in order to tell the character what its inputs are
        /// </summary>
        public void SetInputs(ref AICharacterInputs inputs) //AI
        {
            _moveInputVector = inputs.MoveVector;
            _lookInputVector = inputs.LookVector;
        }

        /// <summary>
        /// (Called by KinematicCharacterMotor during its update cycle)
        /// This is called before the character begins its movement update
        /// </summary>
        public void BeforeCharacterUpdate(float deltaTime)
        {
        }

        /// <summary>
        /// (Called by KinematicCharacterMotor during its update cycle)
        /// This is where you tell your character what its rotation should be right now. 
        /// This is the ONLY place where you should set the character's rotation
        /// </summary>
        public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            switch (CurrentCharacterState)
            {
                case CharacterState.Default:
                    {
                        if (_lookInputVector.sqrMagnitude > 0f && OrientationSharpness > 0f)
                        {
                            // Smoothly interpolate from current to target look direction
                            Vector3 smoothedLookInputDirection = Vector3.Slerp(Motor.CharacterForward, _lookInputVector, 1 - Mathf.Exp(-OrientationSharpness * deltaTime)).normalized;

                            // Set the current rotation (which will be used by the KinematicCharacterMotor)
                            currentRotation = Quaternion.LookRotation(smoothedLookInputDirection, Motor.CharacterUp);
                        }
                        if (OrientTowardsGravity)
                        {
                            // Rotate from current up to invert gravity
                            currentRotation = Quaternion.FromToRotation((currentRotation * Vector3.up), -Gravity) * currentRotation;
                        }
                        break;
                    }
            }
        }

        //Get acceleration for each value of the smoothed velocity.
        private Vector3 GetSmoothVelocity(Vector3 inputVelocity, Vector3 outputVelocity)
        {
            return new Vector3(CalculateAccel(inputVelocity.x, outputVelocity.x), CalculateAccel(inputVelocity.y, outputVelocity.y),
                CalculateAccel(inputVelocity.z, outputVelocity.z));
        }
        
        //Calculate acceleration between two floats.
        private float CalculateAccel(float inputX, float outputX)
        {
            float lerpValue = 0;
            if (outputX < inputX)
            {
                lerpValue += _accelerationSpeed;
            }

            if (outputX > inputX)
            {
                lerpValue -= _decelerationSpeed;
            }
            outputX = Mathf.Lerp(outputX, inputX, lerpValue);
            return outputX;
        }
        
        //Lerp between each individual part of the input and output velocity. For example, "If outputX > inputX, do a lerp
        //between outputX and inputX, at a lerp rate of acceleration. Then opposite if outputX < inputX. Then do the same for each
        //value.
        //(This SHOULD work correctly, just like a Vector3.Lerp, but it allows the function to run checks to see if "value" is
        //greater than/less than "value"
        //This replaces the "sharpness" of the player so acceleration and deceleration doesn't account for magnitude.

        /// <summary>
        /// (Called by KinematicCharacterMotor during its update cycle)
        /// This is where you tell your character what its velocity should be right now. 
        /// This is the ONLY place where you can set the character's velocity
        /// </summary>
        public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            Vector3 targetVelocity = new Vector3();
            switch (CurrentCharacterState)
            {
                case CharacterState.Default: //MOST OF THE STUFF BELOW CAN BE USED FOR MULTIPLE STATES!
                    {
                        // Ground movement
                        if (Motor.GroundingStatus.IsStableOnGround) //"If grounded..."
                        {
                            float currentVelocityMagnitude = currentVelocity.magnitude; //Get magnitude (length)

                            Vector3 effectiveGroundNormal = Motor.GroundingStatus.GroundNormal; //Get ground normal
                            if (currentVelocityMagnitude > 0f && Motor.GroundingStatus.SnappingPrevented) //"If length > 0 and snapping is prevented..."
                            {
                                // Take the normal from where we're coming from
                                Vector3 groundPointToCharacter = Motor.TransientPosition - Motor.GroundingStatus.GroundPoint;
                                if (Vector3.Dot(currentVelocity, groundPointToCharacter) >= 0f)
                                {
                                    effectiveGroundNormal = Motor.GroundingStatus.OuterGroundNormal;
                                }
                                else
                                {
                                    effectiveGroundNormal = Motor.GroundingStatus.InnerGroundNormal;
                                }
                            }

                            // Reorient velocity on slope
                            currentVelocity = Motor.GetDirectionTangentToSurface(currentVelocity, effectiveGroundNormal) * currentVelocityMagnitude;
                            // Calculate target velocity
                            Vector3 inputRight = Vector3.Cross(_moveInputVector, Motor.CharacterUp); //Perpendicular from input and up direction.
                            Vector3 reorientedInput = Vector3.Cross(effectiveGroundNormal, inputRight).normalized * _moveInputVector.magnitude; //Perpendicular from ground normal and previous.
                            Vector3 targetMovementVelocity = reorientedInput * MaxStableMoveSpeed; //Multiply it by MoveSpeed.
                            
                            // Smooth movement Velocity
                            currentVelocity = Vector3.Lerp(currentVelocity, targetMovementVelocity, 1f - Mathf.Exp(-StableMovementSharpness * deltaTime)); //Smooth?
                            
                            Debug.DrawLine(transform.position, transform.position + currentVelocity);
                            
                            //I FOUND A PROBLEM
                            //Above, the currentvelocity is doing a lerp between currentVelocity and our targetVelocity to apply smooth movement. HOWEVER, it will always
                            //start very fast and end slow. At the beginning of the player's acceleration it accelerates fast and slows down when it's closer to its maxSpeed. THEN it does the
                            //SAME THING when slowing down. It starts slowing down FAST, then goes slow again when reaching the targetVelocity of 0.
                            //Removing the lerp and setting the currentVelocity to just be equal to the target results in no acceleration.
                        }
                        // Air movement
                        else //"If not grounded..."
                        {
                            // Add move input
                            Vector3 addedVelocity = new Vector3();
                            if (_moveInputVector.sqrMagnitude > 0f) //"If there's input..."
                            {
                                addedVelocity = _moveInputVector * AirAccelerationSpeed * deltaTime; //Multiply by air acceleration (if any).

                                // Prevent air movement from making you move up steep sloped walls
                                if (Motor.GroundingStatus.FoundAnyGround)
                                {
                                    Vector3 perpenticularObstructionNormal = Vector3.Cross(Vector3.Cross(Motor.CharacterUp, Motor.GroundingStatus.GroundNormal), Motor.CharacterUp).normalized;
                                    addedVelocity = Vector3.ProjectOnPlane(addedVelocity, perpenticularObstructionNormal);
                                }

                                // Limit air movement from inputs to a certain maximum, without limiting the total air move speed from momentum, gravity or other forces
                                Vector3 resultingVelOnInputsPlane = Vector3.ProjectOnPlane(currentVelocity + addedVelocity, Motor.CharacterUp);
                                Debug.DrawLine(transform.position, transform.position + resultingVelOnInputsPlane*2, Color.red);
                                
                                //"If the direction where we want to go has a magnitude greater than move speed AND the dot product angle is greater than 0..."
                                //(The dot product is the angle comparing the two magnitudes. Facing same direction = 1, opposite directions = -1, perpendicular = 0)
                                //if(resultingVelOnInputsPlane.magnitude > MaxAirMoveSpeed && Vector3.Dot(_moveInputVector, resultingVelOnInputsPlane) >= 0f)
                                if(resultingVelOnInputsPlane.magnitude > MaxAirMoveSpeed && Vector3.Dot(_moveInputVector, resultingVelOnInputsPlane) >= MaxAirMoveSpeed + .4f)
                                {
                                    addedVelocity = Vector3.zero;
                                }
                                else
                                {
                                    Vector3 velOnInputsPlane = Vector3.ProjectOnPlane(currentVelocity, Motor.CharacterUp);
                                    Vector3 clampedResultingVelOnInputsPlane = Vector3.ClampMagnitude(resultingVelOnInputsPlane, MaxAirMoveSpeed);
                                    addedVelocity = clampedResultingVelOnInputsPlane - velOnInputsPlane;
                                }
                                //The problem occurs during the >= 0f check. We can definitely change directions from left to right because changing that direction returns a value of
                                //-1. However, smaller changes like going from left to up returns a value of ~.5, which is still greater than 0. In that case, addedVelocity is still 0.
                                
                                currentVelocity += addedVelocity;
                            }
                            else //If not grounded AND there isn't input...
                            {
                                //Slow down the player in the air.
                                addedVelocity = currentVelocity * 2f * deltaTime;
                                Vector3 resultingVelOnInputsPlane = Vector3.ProjectOnPlane(currentVelocity + addedVelocity, Motor.CharacterUp); //Vector3 on the ground resulting from adding both velocities.
                                Vector3 velOnInputsPlane = Vector3.ProjectOnPlane(currentVelocity, Motor.CharacterUp); //Vector3 on the ground from just our current velocity.
                                Vector3 clampedResultingVelOnInputsPlane = Vector3.ClampMagnitude(resultingVelOnInputsPlane, MaxAirMoveSpeed); //Clamp the magnitude from adding both velocities.
                                Vector3 subtractedVelocity = clampedResultingVelOnInputsPlane - velOnInputsPlane; //Take the added velocities and subtract the vector3 of the current velocity.
                                currentVelocity -= subtractedVelocity; //Subtract!
                            }

                            // Gravity (added vector3)
                            currentVelocity += Gravity * deltaTime;

                            // Drag (multiplied float)
                            currentVelocity *= (1f / (1f + (Drag * deltaTime)));
                        }

                        //targetVelocity = GetSmoothVelocity(currentVelocity, targetVelocity);

                        // Handle jumping
                        //_jumpedThisFrame = false;
                        //_timeSinceJumpRequested += deltaTime;
                        //if (_jumpRequested)
                        //{
                        //    // See if we actually are allowed to jump
                        //    if (!_jumpConsumed && ((AllowJumpingWhenSliding ? Motor.GroundingStatus.FoundAnyGround : Motor.GroundingStatus.IsStableOnGround) || _timeSinceLastAbleToJump <= JumpPostGroundingGraceTime))
                        //    {
                        //        // Calculate jump direction before ungrounding
                        //        Vector3 jumpDirection = Motor.CharacterUp;
                        //        if (Motor.GroundingStatus.FoundAnyGround && !Motor.GroundingStatus.IsStableOnGround)
                        //        {
                        //            jumpDirection = Motor.GroundingStatus.GroundNormal;
                        //        }
//
                        //        // Makes the character skip ground probing/snapping on its next update. 
                        //        // If this line weren't here, the character would remain snapped to the ground when trying to jump. Try commenting this line out and see.
                        //        Motor.ForceUnground();
//
                        //        // Add to the return velocity and reset jump state
                        //        currentVelocity += (jumpDirection * JumpUpSpeed) - Vector3.Project(currentVelocity, Motor.CharacterUp);
                        //        currentVelocity += (_moveInputVector * JumpScalableForwardSpeed);
                        //        _jumpRequested = false;
                        //        _jumpConsumed = true;
                        //        _jumpedThisFrame = true;
                        //    }
                        //}
//
                        //// Take into account additive velocity
                        //if (_internalVelocityAdd.sqrMagnitude > 0f)
                        //{
                        //    currentVelocity += _internalVelocityAdd;
                        //    _internalVelocityAdd = Vector3.zero;
                        //}
                        break;
                    }
            }
        }
        //public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        //{
        //    Vector3 targetVelocity = new Vector3();
        //    switch (CurrentCharacterState)
        //    {
        //        case CharacterState.Default: //MOST OF THE STUFF BELOW CAN BE USED FOR MULTIPLE STATES!
        //            {
        //                // Ground movement
        //                if (Motor.GroundingStatus.IsStableOnGround) //"If grounded..."
        //                {
        //                    float currentVelocityMagnitude = currentVelocity.magnitude; //Get magnitude (length)
//
        //                    Vector3 effectiveGroundNormal = Motor.GroundingStatus.GroundNormal; //Get ground normal
        //                    if (currentVelocityMagnitude > 0f && Motor.GroundingStatus.SnappingPrevented) //"If length > 0 and snapping is prevented..."
        //                    {
        //                        // Take the normal from where we're coming from
        //                        Vector3 groundPointToCharacter = Motor.TransientPosition - Motor.GroundingStatus.GroundPoint;
        //                        if (Vector3.Dot(currentVelocity, groundPointToCharacter) >= 0f)
        //                        {
        //                            effectiveGroundNormal = Motor.GroundingStatus.OuterGroundNormal;
        //                        }
        //                        else
        //                        {
        //                            effectiveGroundNormal = Motor.GroundingStatus.InnerGroundNormal;
        //                        }
        //                    }
//
        //                    // Reorient velocity on slope
        //                    currentVelocity = Motor.GetDirectionTangentToSurface(currentVelocity, effectiveGroundNormal) * currentVelocityMagnitude;
//
        //                    // Calculate target velocity
        //                    Vector3 inputRight = Vector3.Cross(_moveInputVector, Motor.CharacterUp); //Perpendicular from input and up direction.
        //                    Vector3 reorientedInput = Vector3.Cross(effectiveGroundNormal, inputRight).normalized * _moveInputVector.magnitude; //Perpendicular from ground normal and previous.
        //                    Vector3 targetMovementVelocity = reorientedInput * MaxStableMoveSpeed; //Multiply it by MoveSpeed.
//
        //                    // Smooth movement Velocity
        //                    currentVelocity = Vector3.Lerp(currentVelocity, targetMovementVelocity, 1f - Mathf.Exp(-StableMovementSharpness * deltaTime)); //Smooth?
        //                }
        //                // Air movement
        //                else //"If not grounded..."
        //                {
        //                    // Add move input
        //                    Vector3 addedVelocity = new Vector3();
        //                    if (_moveInputVector.sqrMagnitude > 0f) //"If there's input..."
        //                    {
        //                        addedVelocity = _moveInputVector * AirAccelerationSpeed * deltaTime; //Multiply by air acceleration (if any).
//
        //                        // Prevent air movement from making you move up steep sloped walls
        //                        if (Motor.GroundingStatus.FoundAnyGround)
        //                        {
        //                            Vector3 perpenticularObstructionNormal = Vector3.Cross(Vector3.Cross(Motor.CharacterUp, Motor.GroundingStatus.GroundNormal), Motor.CharacterUp).normalized;
        //                            addedVelocity = Vector3.ProjectOnPlane(addedVelocity, perpenticularObstructionNormal);
        //                        }
//
        //                        // Limit air movement from inputs to a certain maximum, without limiting the total air move speed from momentum, gravity or other forces
        //                        Vector3 resultingVelOnInputsPlane = Vector3.ProjectOnPlane(currentVelocity + addedVelocity, Motor.CharacterUp);
        //                        Debug.DrawLine(transform.position, transform.position + resultingVelOnInputsPlane*2, Color.red);
        //                        
        //                        //"If the direction where we want to go has a magnitude greater than move speed AND the dot product angle is greater than 0..."
        //                        //(The dot product is the angle comparing the two magnitudes. Facing same direction = 1, opposite directions = -1, perpendicular = 0)
        //                        //if(resultingVelOnInputsPlane.magnitude > MaxAirMoveSpeed && Vector3.Dot(_moveInputVector, resultingVelOnInputsPlane) >= 0f)
        //                        if(resultingVelOnInputsPlane.magnitude > MaxAirMoveSpeed && Vector3.Dot(_moveInputVector, resultingVelOnInputsPlane) >= MaxAirMoveSpeed + .4f)
        //                        {
        //                            addedVelocity = Vector3.zero;
        //                        }
        //                        else
        //                        {
        //                            Vector3 velOnInputsPlane = Vector3.ProjectOnPlane(currentVelocity, Motor.CharacterUp);
        //                            Vector3 clampedResultingVelOnInputsPlane = Vector3.ClampMagnitude(resultingVelOnInputsPlane, MaxAirMoveSpeed);
        //                            addedVelocity = clampedResultingVelOnInputsPlane - velOnInputsPlane;
        //                        }
        //                        //The problem occurs during the >= 0f check. We can definitely change directions from left to right because changing that direction returns a value of
        //                        //-1. However, smaller changes like going from left to up returns a value of ~.5, which is still greater than 0. In that case, addedVelocity is still 0.
        //                        
        //                        currentVelocity += addedVelocity;
        //                    }
        //                    else //If not grounded AND there isn't input...
        //                    {
        //                        //Slow down the player in the air.
        //                        addedVelocity = currentVelocity * 2f * deltaTime;
        //                        Vector3 resultingVelOnInputsPlane = Vector3.ProjectOnPlane(currentVelocity + addedVelocity, Motor.CharacterUp); //Vector3 on the ground resulting from adding both velocities.
        //                        Vector3 velOnInputsPlane = Vector3.ProjectOnPlane(currentVelocity, Motor.CharacterUp); //Vector3 on the ground from just our current velocity.
        //                        Vector3 clampedResultingVelOnInputsPlane = Vector3.ClampMagnitude(resultingVelOnInputsPlane, MaxAirMoveSpeed); //Clamp the magnitude from adding both velocities.
        //                        Vector3 subtractedVelocity = clampedResultingVelOnInputsPlane - velOnInputsPlane; //Take the added velocities and subtract the vector3 of the current velocity.
        //                        currentVelocity -= subtractedVelocity; //Subtract!
        //                    }
//
        //                    // Gravity (added vector3)
        //                    currentVelocity += Gravity * deltaTime;
//
        //                    // Drag (multiplied float)
        //                    currentVelocity *= (1f / (1f + (Drag * deltaTime)));
        //                }
//
        //                // Handle jumping
        //                //_jumpedThisFrame = false;
        //                //_timeSinceJumpRequested += deltaTime;
        //                //if (_jumpRequested)
        //                //{
        //                //    // See if we actually are allowed to jump
        //                //    if (!_jumpConsumed && ((AllowJumpingWhenSliding ? Motor.GroundingStatus.FoundAnyGround : Motor.GroundingStatus.IsStableOnGround) || _timeSinceLastAbleToJump <= JumpPostGroundingGraceTime))
        //                //    {
        //                //        // Calculate jump direction before ungrounding
        //                //        Vector3 jumpDirection = Motor.CharacterUp;
        //                //        if (Motor.GroundingStatus.FoundAnyGround && !Motor.GroundingStatus.IsStableOnGround)
        //                //        {
        //                //            jumpDirection = Motor.GroundingStatus.GroundNormal;
        //                //        }
////
        //                //        // Makes the character skip ground probing/snapping on its next update. 
        //                //        // If this line weren't here, the character would remain snapped to the ground when trying to jump. Try commenting this line out and see.
        //                //        Motor.ForceUnground();
////
        //                //        // Add to the return velocity and reset jump state
        //                //        currentVelocity += (jumpDirection * JumpUpSpeed) - Vector3.Project(currentVelocity, Motor.CharacterUp);
        //                //        currentVelocity += (_moveInputVector * JumpScalableForwardSpeed);
        //                //        _jumpRequested = false;
        //                //        _jumpConsumed = true;
        //                //        _jumpedThisFrame = true;
        //                //    }
        //                //}
////
        //                //// Take into account additive velocity
        //                //if (_internalVelocityAdd.sqrMagnitude > 0f)
        //                //{
        //                //    currentVelocity += _internalVelocityAdd;
        //                //    _internalVelocityAdd = Vector3.zero;
        //                //}
        //                break;
        //            }
        //    }
        //}

        /// <summary>
        /// (Called by KinematicCharacterMotor during its update cycle)
        /// This is called after the character has finished its movement update
        /// </summary>
        public void AfterCharacterUpdate(float deltaTime)
        {
            switch (CurrentCharacterState)
            {
                case CharacterState.Default:
                    {
                        // Handle jump-related values
                        {
                            // Handle jumping pre-ground grace period
                            if (_jumpRequested && _timeSinceJumpRequested > JumpPreGroundingGraceTime)
                            {
                                _jumpRequested = false;
                            }

                            if (AllowJumpingWhenSliding ? Motor.GroundingStatus.FoundAnyGround : Motor.GroundingStatus.IsStableOnGround)
                            {
                                // If we're on a ground surface, reset jumping values
                                if (!_jumpedThisFrame)
                                {
                                    _jumpConsumed = false;
                                }
                                _timeSinceLastAbleToJump = 0f;
                            }
                            else
                            {
                                // Keep track of time since we were last able to jump (for grace period)
                                _timeSinceLastAbleToJump += deltaTime;
                            }
                        }

                        // Handle uncrouching
                        if (_isCrouching && !_shouldBeCrouching)
                        {
                            // Do an overlap test with the character's standing height to see if there are any obstructions
                            Motor.SetCapsuleDimensions(0.5f, 2f, 1f);
                            if (Motor.CharacterOverlap(
                                Motor.TransientPosition,
                                Motor.TransientRotation,
                                _probedColliders,
                                Motor.CollidableLayers,
                                QueryTriggerInteraction.Ignore) > 0)
                            {
                                // If obstructions, just stick to crouching dimensions
                                Motor.SetCapsuleDimensions(0.5f, 1f, 0.5f);
                            }
                            else
                            {
                                // If no obstructions, uncrouch
                                MeshRoot.localScale = new Vector3(1f, 1f, 1f);
                                _isCrouching = false;
                            }
                        }
                        break;
                    }
            }
        }

        public void PostGroundingUpdate(float deltaTime)
        {
            // Handle landing and leaving ground
            if (Motor.GroundingStatus.IsStableOnGround && !Motor.LastGroundingStatus.IsStableOnGround)
            {
                OnLanded();
            }
            else if (!Motor.GroundingStatus.IsStableOnGround && Motor.LastGroundingStatus.IsStableOnGround)
            {
                OnLeaveStableGround();
            }
        }

        public bool IsColliderValidForCollisions(Collider coll)
        {
            if (IgnoredColliders.Count == 0)
            {
                return true;
            }

            if (IgnoredColliders.Contains(coll))
            {
                return false;
            }
            return true;
        }

        public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
        }

        public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
        }

        public void AddVelocity(Vector3 velocity)
        {
            switch (CurrentCharacterState)
            {
                case CharacterState.Default:
                    {
                        _internalVelocityAdd += velocity;
                        break;
                    }
            }
        }

        public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
        {
        }

        protected void OnLanded()
        {
        }

        protected void OnLeaveStableGround()
        {
        }

        public void OnDiscreteCollisionDetected(Collider hitCollider)
        {
        }
    }
}
//TO FIX:
//(Currently nothing)

//TO DO:
//1. Implement acceleration. Currently the "acceleration" of the character is controlled by StableMovementSharpness. (Higher value = more "stickiness")
//Acceleration currently seems faster than deceleration, and increasing the sharpness REALLY makes deceleration slow. Experiment with making the sharpness
//lower during acceleration and higher during deceleration.

//2. Start implementing states. For our Walk, Run, and Idle states, each will simply implement the same "Default" state. To enter the walk state,
//just add a new switch case that handles movement speed. "If [shift] is held and current state is "Default", set MoveSpeed to [number] and transition to walk state."
//For our sword state, check if mouse has been pressed. If so, transition to sword state and start a coroutine to go back to idle. In "UpdateVelocity", add a velocity
//until it reaches maxspeed. Once it does, don't add anymore velocity. THEN, instead of telling it to accelerate and slow down manually, just adjust the acceleration and
//deceleration values (higher accel speed and lower decel).



//NOTES ON MOVEMENT/STATE CHANGES
//To transition to different states, we can actually use a similar check in our normal player script to handle when the transitions occur. This script
//simply includes enter and exit events that we can include. In a way, "TransitionToState" is quite literally "ChangeState" in our old script.

//OTHER NOTES
//Depending on the height and the direction the player is facing, sometimes the player will gain a bit of velocity after touching the ground. Seems to be a
//very specific case where the shortest block in our scene causes this after jumping off. (It doesn't seem to be an issue if we don't use jumping at all)