using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KinematicCharacterController;
using System;

namespace KinematicCharacterController.Nate
{
    public enum CharacterState //Character states (Walking, crouching, attacking, etc)
    {
        Default, 
        Walk, 
        Crouched, 
        SwordAttack,
        Slide,
        BowAttack,
        SpinAttack,
        RollAttack,
    }

    public enum SwordAttackState
    {
        SwingStart,
        SwingEnd
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
        public bool SwordSwing;
        public bool ToggleTargetingMode;
    }

    public struct AICharacterInputs
    {
        public Vector3 MoveVector;
        public Vector3 LookVector;
    }

    public class NateCharacterController : MonoBehaviour, ICharacterController
    {
        public KinematicCharacterMotor Motor;
        public NewTempSwordCoroutine swordCoroutineScript;

        [Header("Stable Movement")] public float MaxStableMoveSpeed = 10f;
        public float StableMovementSharpness = 15f;
        public float OrientationSharpness = 10f;
        public OrientationMethod OrientationMethod = OrientationMethod.TowardsCamera;

        [Header("Air Movement")] public float MaxAirMoveSpeed = 100f;
        public float AirAccelerationSpeed = 15f;
        public float Drag = 0.1f;

        //[Header("Jumping")] public bool AllowJumpingWhenSliding = false;
        //public float JumpUpSpeed = 10f;
        //public float JumpScalableForwardSpeed = 10f;
        //public float JumpPreGroundingGraceTime = 0f;
        //public float JumpPostGroundingGraceTime = 0f;

        [Header("Misc")] public List<Collider> IgnoredColliders = new List<Collider>();
        public bool OrientTowardsGravity = false;
        //public BoolValue TargetingModeActive;
        public CameraFollowPlayer camera; //Change to target mode manager later
        public Vector3 Gravity = new Vector3(0, -30f, 0);
        public Transform MeshRoot;
        public Transform CameraFollowPoint;

        public CharacterState CurrentCharacterState { get; private set; }
        public SwordAttackState CurrentSwordAttackState;

        private Collider[] _probedColliders = new Collider[8];
        public Vector3 _moveInputVector;

        private Vector3 _lookInputVector;

        //private Vector3 _targetVector;
        private bool _jumpRequested = false;
        private bool _jumpConsumed = false;
        private bool _jumpedThisFrame = false;
        private float _timeSinceJumpRequested = Mathf.Infinity;
        private float _timeSinceLastAbleToJump = 0f;
        public float _accelerationSpeed = .1f;
        public float _decelerationSpeed = .1f;
        private float testLerp = 0f;
        private Vector3 _internalVelocityAdd = Vector3.zero;
        private bool _shouldBeCrouching = false;
        public bool _isCrouching = false;
        
        public Coroutine _tempSwordCoroutine;

        private void Start()
        {
            //Handle initial state
            TransitionToState(CharacterState.Default);

            // Assign the characterController to the motor
            Motor.CharacterController = this;
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
                    _isCrouching = false; //Reset
                    Motor.SetCapsuleDimensions(0.5f, 2f, 1f);
                    MeshRoot.localScale = new Vector3(1f, 1f, 1f);
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
            Vector3 moveInputVector =
                Vector3.ClampMagnitude(new Vector3(inputs.MoveAxisRight, 0f, inputs.MoveAxisForward), 1f);

            // Calculate camera direction and rotation on the character plane
            Vector3 cameraPlanarDirection =
                Vector3.ProjectOnPlane(inputs.CameraRotation * Vector3.forward, Motor.CharacterUp).normalized;
            if (cameraPlanarDirection.sqrMagnitude == 0f)
            {
                cameraPlanarDirection = Vector3.ProjectOnPlane(inputs.CameraRotation * Vector3.up, Motor.CharacterUp)
                    .normalized;
            }
            
            Quaternion cameraPlanarRotation = Quaternion.LookRotation(cameraPlanarDirection, Motor.CharacterUp);

            //Inputs while grounded
            if (Motor.GroundingStatus.IsStableOnGround)
            {
                if (inputs.SwordSwing) //Temp
                {
                    swordCoroutineScript.StartSwordSwing(); //Temporary, to start coroutine for canSwing check.
                }

                if (inputs.ToggleTargetingMode) //Temp target mode activation
                {
                    if (!camera.TargetingModeActive)
                    {
                        camera.TargetingModeActive = true;
                    }
                    else
                    {
                        camera.TargetingModeActive = false;
                    }
                }
            }
            else
            {
                camera.TargetingModeActive = false;
            }

            switch (CurrentCharacterState)
            {
                //Default inputs
                case CharacterState.Default:
                {
                    _moveInputVector = cameraPlanarRotation * moveInputVector;
                    StableMovementSharpness = 7f;

                    switch (OrientationMethod)
                    {
                        case OrientationMethod.TowardsMovement:
                            _lookInputVector = _moveInputVector.normalized;
                            break;
                    }
                    
                    //Transition to crouching
                    if (inputs.CrouchDown && Motor.GroundingStatus.IsStableOnGround)
                    {
                        _shouldBeCrouching = true;

                        if (!_isCrouching)
                        {
                            _isCrouching = true;
                            Motor.SetCapsuleDimensions(0.5f, 1f, 0.5f); //Scales the hitbox.
                            MeshRoot.localScale = new Vector3(1f, 0.5f, 1f); //Scales the mesh root.
                            TransitionToState(CharacterState.Crouched);
                        }
                    }

                    break;
                }
                //Crouched inputs
                case CharacterState.Crouched:
                {
                    _moveInputVector = cameraPlanarRotation * moveInputVector;
                    StableMovementSharpness = 7f;

                    switch (OrientationMethod)
                    {
                        case OrientationMethod.TowardsMovement:
                            _lookInputVector = _moveInputVector.normalized;
                            break;
                    }

                    // Stop crouching
                    if (inputs.CrouchUp)
                    {
                        _shouldBeCrouching = false;
                    }

                    break;
                }
                //Sword attack inputs
                case CharacterState.SwordAttack: //Temp
                {
                    _moveInputVector = Vector3.ClampMagnitude(Motor.CharacterForward, 1f);

                    switch (CurrentSwordAttackState)
                    {
                        case SwordAttackState.SwingStart:
                        {
                            StableMovementSharpness = 10f;
                            break;
                        }
                        case SwordAttackState.SwingEnd:
                        {
                            StableMovementSharpness = 10f;
                            break;
                        }
                    }
                    break;
                }
                
            }
        }

        public void RunSwordSwingMovement()
        {
            //Debug.Log("Hi");
            _tempSwordCoroutine = StartCoroutine(SwordMovementCoroutine(.5f));
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
            switch (CurrentCharacterState)
            {
                case CharacterState.Default:
                {
                    MaxStableMoveSpeed = 7f;
                    break;
                }
                case CharacterState.Crouched:
                {
                    MaxStableMoveSpeed = 3f;
                    break;
                }
                case CharacterState.SwordAttack:
                {
                    switch (CurrentSwordAttackState)
                    {
                        case SwordAttackState.SwingStart:
                        {
                            MaxStableMoveSpeed = 10f;
                            break;
                        }
                        case SwordAttackState.SwingEnd:
                        {
                            MaxStableMoveSpeed = 0f;
                            break;
                        }
                    }
                    
                    break;
                }
                case CharacterState.Slide:
                {
                    //At high velocities, the MaxSlopeAngle will decrease until 0, causing the player to slide on everything and even go off ramps.
                    //Could also just set MaxStableSlopeAngle to 0 until magnitude is under a certain point.
                    float velocitySlidingPoint = 15f;
                    float maxAngle = 60f;
                    if (Motor.Velocity.magnitude > velocitySlidingPoint)
                    {
                        float percentage = Motor.Velocity.magnitude / 35f;
                        Motor.MaxStableSlopeAngle = Mathf.Lerp(maxAngle, 0, percentage);
                    }
                    else
                    {
                        Motor.MaxStableSlopeAngle = maxAngle;
                    }

                    break;
                }
            }
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
                case CharacterState.Crouched:
                case CharacterState.SwordAttack:
                case CharacterState.BowAttack:
                {
                    if (camera.TargetingModeActive)
                    {
                        //Look towards the target.
                        Vector3 targetDir = Vector3.ProjectOnPlane((camera.target.position - transform.position).normalized, Motor.CharacterUp);
                        Vector3 reorientedTargetDir = Vector3.Slerp(Motor.CharacterForward, targetDir,1 - Mathf.Exp(-OrientationSharpness * deltaTime)).normalized;

                        currentRotation = Quaternion.LookRotation(reorientedTargetDir, Motor.CharacterUp);
                    }
                    else
                    {
                        if (_lookInputVector.sqrMagnitude > 0f && OrientationSharpness > 0f)
                        {
                            // Smoothly interpolate from current to target look direction
                            Vector3 smoothedLookInputDirection = Vector3.Slerp(Motor.CharacterForward, _lookInputVector,
                                1 - Mathf.Exp(-OrientationSharpness * deltaTime)).normalized;

                            // Set the current rotation (which will be used by the KinematicCharacterMotor)
                            currentRotation = Quaternion.LookRotation(smoothedLookInputDirection, Motor.CharacterUp);
                        }

                        if (OrientTowardsGravity)
                        {
                            // Rotate from current up to invert gravity
                            currentRotation = Quaternion.FromToRotation((currentRotation * Vector3.up), -Gravity) *
                                              currentRotation;
                        }
                    }
                    break;
                }
                case CharacterState.Slide:
                {
                    //currentRotation = Quaternion.FromToRotation(Motor.GroundingStatus.GroundPoint, Motor.CharacterUp);
                    break;
                }

            }
        }

        /// <summary>
        /// (Called by KinematicCharacterMotor during its update cycle)
        /// This is where you tell your character what its velocity should be right now. 
        /// This is the ONLY place where you can set the character's velocity
        /// </summary>
        public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            // Ground movement
            if (Motor.GroundingStatus.IsStableOnGround) //"If grounded..."
            {
                float currentVelocityMagnitude = currentVelocity.magnitude; //Get magnitude (length)

                Vector3 effectiveGroundNormal = Motor.GroundingStatus.GroundNormal; //Get ground normal
                if (currentVelocityMagnitude > 0f && Motor.GroundingStatus.SnappingPrevented
                ) //"If length > 0 and snapping is prevented..."
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
                //The target velocity has nothing to do with accel.

                // Smooth movement Velocity
                currentVelocity = Vector3.Lerp(currentVelocity, targetMovementVelocity, 1f - Mathf.Exp(-StableMovementSharpness * deltaTime)); //Smaller each frame

            }
            // Air movement
            else //"If not grounded..."
            {
                // Add move input
                Vector3 addedVelocity = new Vector3();
                if (_moveInputVector.sqrMagnitude > 0f) //"If there's input..." (Faster than .magnitude)
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
                    Debug.DrawLine(transform.position, transform.position + resultingVelOnInputsPlane * 2, Color.red);

                    //"If the direction where we want to go has a magnitude greater than move speed AND the dot product angle is greater than 0..."
                    //(The dot product is the angle comparing the two magnitudes. Facing same direction = 1, opposite directions = -1, perpendicular = 0)
                    if (resultingVelOnInputsPlane.magnitude > MaxAirMoveSpeed && Vector3.Dot(_moveInputVector, resultingVelOnInputsPlane) >= MaxAirMoveSpeed + .4f
                    ) //Default: >= 0
                    {
                        addedVelocity = Vector3.zero;
                    }
                    else
                    {
                        Vector3 velOnInputsPlane = Vector3.ProjectOnPlane(currentVelocity, Motor.CharacterUp);
                        Vector3 clampedResultingVelOnInputsPlane = Vector3.ClampMagnitude(resultingVelOnInputsPlane, MaxAirMoveSpeed);
                        addedVelocity = clampedResultingVelOnInputsPlane - velOnInputsPlane;
                    }

                    currentVelocity += addedVelocity;
                }
                else //If not grounded AND there isn't input...
                {
                    //This area causes the subtracted velocity to be negative when switching from sword to default state near the ground.
                    //Slow down the player in the air.
                    //Debug.Log("Doing the thing");
                    addedVelocity = currentVelocity * 2f * deltaTime;
                    Vector3 resultingVelOnInputsPlane = Vector3.ProjectOnPlane(currentVelocity + addedVelocity, Motor.CharacterUp); //Direction between the two velocities and laid perpendicular to CharacterUp
                    Vector3 velOnInputsPlane = Vector3.ProjectOnPlane(currentVelocity, Motor.CharacterUp); //Similar
                    Vector3 testClampedVelOnInputsPlane = Vector3.ClampMagnitude(velOnInputsPlane, MaxAirMoveSpeed); //Prevents exponential addition.
                    Vector3 clampedResultingVelOnInputsPlane = Vector3.ClampMagnitude(resultingVelOnInputsPlane, MaxAirMoveSpeed); //Clamp the magnitude from adding both velocities.
                    Vector3 subtractedVelocity = clampedResultingVelOnInputsPlane - testClampedVelOnInputsPlane; //Take the added velocities and subtract the vector3 of the current velocity.
                    //Debug.Log(clampedResultingVelOnInputsPlane + " - " + testClampedVelOnInputsPlane);

                    currentVelocity -= subtractedVelocity; //Subtract!
                }

                // Gravity (added vector3)
                currentVelocity += Gravity * deltaTime;

                // Drag (multiplied float)
                currentVelocity *= (1f / (1f + (Drag * deltaTime)));

                //Debug.Log(currentVelocity);
            }
        }

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
                    break;
                }
                case CharacterState.Crouched:
                {
                    // Handle uncrouching
                    if (_isCrouching && !_shouldBeCrouching
                    ) //If currently crouching and there's no more crouch input...
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
                            TransitionToState(CharacterState.Default);
                        }
                    }

                    break;
                }
            }

            //Switch to default state if in midair
            if (!Motor.GroundingStatus.IsStableOnGround)
            {
                if (CurrentCharacterState != CharacterState.Default)
                {
                    //_timeBeforeFallingStateCoroutine = StartCoroutine(TimeBeforeFallingStateCoroutine());
                    StartCoroutine(TimeBeforeFallingStateCoroutine());
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

        public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint,
            ref HitStabilityReport hitStabilityReport)
        {
        }

        public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint,
            ref HitStabilityReport hitStabilityReport)
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

        public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint,
            Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
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

        // COROUTINES

        private IEnumerator SwordMovementCoroutine(float time) //Temp
        {
            float timeStart = time * .35f;
            float timeEnd = timeStart - time;
            CurrentSwordAttackState = SwordAttackState.SwingStart; //Start swing movement
            yield return CustomTimer.Timer(timeStart);
            CurrentSwordAttackState = SwordAttackState.SwingEnd; //End swing movement
            yield return CustomTimer.Timer(timeEnd);
            //canSwing.initialBool = true;
            //TransitionToState(CharacterState.Default);
        }

        private IEnumerator TimeBeforeFallingStateCoroutine()
        {
            yield return CustomTimer.Timer(.1f);
            TransitionToState(CharacterState.Default);
        }

}
}
//TO FIX:
//Player needs to be forced out of crouch while falling.

//TO DO:
//1. Implement states.

//ISSUES:
//Depending on the height and the direction the player is facing, sometimes the player will gain a bit of velocity after touching the ground. Seems to be a
//very specific case where the shortest block in our scene causes this after jumping off. (It doesn't seem to be an issue if we don't use jumping at all)