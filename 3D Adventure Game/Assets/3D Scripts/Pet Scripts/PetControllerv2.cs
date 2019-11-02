using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KinematicCharacterController;
using System;
using System.Diagnostics;
using KinematicCharacterController.Nate;
using Debug = UnityEngine.Debug;

//Movement controller for the fox.
namespace KinematicCharacterController.PetControllerv2
{
    public enum PetState //Character states (Walking, crouching, attacking, etc)
    {
        Default,
        Crouched,
        MoveToPlayer,
        FollowPath,
        SwordAttack,
        SpinAttack,
        RollAttack,
        Warp
    }

    public enum PetDefaultState
    {
        Run,
        Walk,
        Idle,
    }

    public enum PetCrouchedState
    {
        Run,
        Idle,
    }

    //Crouched should either be a bool or a sub state. 

    public class PetControllerv2 : MonoBehaviour, ICharacterController
    {
        public KinematicCharacterMotor Motor;

        [Header("Stable Movement")] public float MaxStableMoveSpeed = 10f;
        public float StableMovementSharpness = 15f;
        public float OrientationSharpness = 10f;

        [Header("Air Movement")] public float MaxAirMoveSpeed = 100f;
        public float AirAccelerationSpeed = 15f;
        public float Drag = 0.1f;

        [Header("Misc")] public List<Collider> IgnoredColliders = new List<Collider>();
        public bool OrientTowardsGravity = false;
        public Vector3 Gravity = new Vector3(0, -30f, 0);
        public Transform MeshRoot;

        public PetState CurrentPetState { get; private set; }
        public NateCharacterController player;
        public CharacterState NewCharacterState;

        private Collider[] _probedColliders = new Collider[8];

        private Vector3 _moveInputVector;
        private Vector3 _lookInputVector;
        private Vector3 _internalVelocityAdd = Vector3.zero;
        private Vector3 playerPosDelayed;
        private Vector3 transformPosition;
        private Vector3 targetDirection;

        private Vector3 deviationDir;

        private bool _isCrouching = false;
        private bool _playerIsCrouching;
        private bool _shouldBeCrouching = false;
        private bool _canCheckPathfinding = true;
        private bool _shouldForceDeviationCloser;

        private float moveInputDeviationValue;
        private float lastMoveInputDeviationValue;
        private float walkRadius;
        
        //Keep the state's movespeed stored and only change it during a state transition.
        private float stateMaxStableMoveSpeed;
        
        //The player's max stableMoveSpeed, updated every frame.
        private float playerMaxStableMoveSpeed;

        private Vector3 lastInnerNormal = Vector3.zero;
        private Vector3 lastOuterNormal = Vector3.zero;

        public UnitFollowNew pathfinding;
        public LayerMask WallLayerMask;

        //Enums
        public PetDefaultState currentDefaultState;
        public PetCrouchedState currentCrouchedState;

        //Coroutines
        private Coroutine StateTransitionDelayCoroutine;
        private bool canStartTransition = true;
        private Coroutine TestPlayerPositionDelayCoroutine;
        private Coroutine TestSetInitialMoveInputDelayCoroutine;
        private bool canStartMoveInputDelay = true;
        private Coroutine ChangeDeviationDirectionCoroutine;
        private bool canChangeDeviationDirection;
        //private Coroutine ForceDeviationChangeCoroutine;
        private bool _canForceDeviation;

        private void Start()
        {
            //Handle initial state
            TransitionToState(PetState.Default);

            // Assign the characterController to the motor
            Motor.CharacterController = this;

            playerPosDelayed = player.transform.position;
        }

        private void Update()
        {
            SetInputs(); //Temp
            transformPosition = Motor.transform.position;
            playerMaxStableMoveSpeed = player.MaxStableMoveSpeed;
            _playerIsCrouching = player._isCrouching;
        }

        /// <summary>
        /// Handles movement state transitions and enter/exit callbacks
        /// </summary>
        public void TransitionToState(PetState newState)
        {
            PetState tmpInitialState = CurrentPetState; //Get current state.
            OnStateExit(tmpInitialState, newState); //Do the OnStateExit stuff from current state to new state.
            CurrentPetState = newState; //Current state = new state.
            Debug.Log("Pet's state set to " + newState + ".");
            OnStateEnter(newState, tmpInitialState); //Do the OnStateEnter stuff to new state from the last state.
        }

        /// <summary>
        /// Event when entering a state
        /// </summary>
        public void OnStateEnter(PetState state, PetState fromState)
        {
            switch (state)
            {
                case PetState.Default:
                {
                    currentDefaultState = PetDefaultState.Idle;
                    break;
                }
                case PetState.Crouched:
                {
                    break;
                }
                case PetState.MoveToPlayer:
                case PetState.FollowPath:
                {
                    stateMaxStableMoveSpeed = playerMaxStableMoveSpeed * 1.15f;
                    break;
                }
            }
        }

        /// <summary>
        /// Event when exiting a state
        /// </summary>
        public void OnStateExit(PetState state, PetState toState)
        {
            switch (state)
            {
                case PetState.Default:
                {
                    switch (currentDefaultState)
                    {
                        case PetDefaultState.Run:
                        {
                            //Turn off deviation changing.
                            //Turn off speed changing.
                            canChangeDeviationDirection = false;
                            break;
                        }
                        case PetDefaultState.Walk:
                        {
                            //Turn off speed changing.
                            break;
                        }
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// This is called every frame by [NatePlayer] in order to tell the character what its inputs are
        /// </summary>
        //public void SetInputs(ref PlayerCharacterInputs inputs)
        public void SetInputs()
        {
            //Pet states
            switch (CurrentPetState)
            {
                case PetState.Default:
                {
                    //Check for obstacles and move away accordingly.
                    
                    //if (_playerIsCrouching)
                    //{
                    //    if (!_isCrouching)
                    //    {
                    //        _isCrouching = true;
                    //        Motor.SetCapsuleDimensions(0.5f, 1f, 0.25f); //Scales the hitbox.
                    //        MeshRoot.localScale = new Vector3(1f, 0.35f, 1.2f); //Scales the mesh root.
                    //    }
                    //}
                    //else
                    //{
                    //    _isCrouching = false;
                    //    Motor.SetCapsuleDimensions(0.5f, 1f, .25f); //Scales the hitbox.
                    //    MeshRoot.localScale = new Vector3(1f, .5f, 1.2f); //Scales the mesh root.
                    //} 
                    
                    //Default sub-states
                    switch (currentDefaultState)
                    {
                        case PetDefaultState.Run:
                        {
                            //Move according to deviation changes.
                            //Allow speed changes
                            //Lerp
                            break;
                        }
                        case PetDefaultState.Walk:
                        {
                            //If too far away from the player, move closer and remain close.
                            //Allow speed changes
                            //Lerp
                            break;
                        }
                        case PetDefaultState.Idle:
                        {
                            //Move input is zero.
                            break;
                        }
                    }
                    //If the player's distance is ever greater than the warp radius, teleport to the player
                    //(CharacterMotor.SetPosition())
                    
                    //---OLD CODE TO REFERENCE WHEN SETTING MOVE INPUT AND LOOK INPUT---
                    //---DETERMINE STATE TRANSITIONING IN BEFORE AND AFTER CHAR UPDATE (BELOW)---
                    //Vector3 playerPosition = player.Motor.transform.position;
                    //Vector3 playerDir = playerPosition - Motor.transform.position;
                    //float distanceToPlayer = playerDir.sqrMagnitude;
//
                    ////Get directions of the player.
                    //Vector3 playerCharUp = player.Motor.CharacterUp;
                    //Vector3 playerCharRight = player.Motor.CharacterRight;
                    //Vector3 playerCharLeft = -playerCharRight;
                    //Vector3 playerCharForward = player.Motor.CharacterForward;
                    //
                    ////Set left and right position based on the delayed position of the player;
                    //Vector3 rightPos = playerPosition + playerCharRight;
                    //Vector3 leftPos = playerPosition + playerCharLeft;;
                    //Vector3 rightDir = Vector3.ProjectOnPlane(rightPos - transformPosition, Motor.CharacterUp);
                    //Vector3 leftdir = Vector3.ProjectOnPlane(leftPos - transformPosition, Motor.CharacterUp);
                    //Vector3 playerSideDir = Vector3.ProjectOnPlane(playerDir, playerCharRight);
                    //
                    //float stopRadius = 1f; //Minimum distance before stopping.
                    //Vector3 playerVelocity = player.Motor.Velocity;
                    //
                    ////While outside the walk radius...
                    //if (distanceToPlayer > Mathf.Pow(walkRadius, 2))
                    //{
                    //    walkRadius = 2.8f;
                    //    //Move toward a point perpendicular to the player's forward direction that extends x distance.
                    //    //This allows for leniency when running back to the player.
                    //    if (rightDir.sqrMagnitude < leftdir.sqrMagnitude)
                    //    {
                    //        targetDirection = rightDir;
                    //    }
                    //    else
                    //    {
                    //        targetDirection = leftdir;
                    //    }
                    //    
                    //    Debug.Log("Moving directly to player");
                    //    _moveInputVector = Vector3.ProjectOnPlane(targetDirection.normalized, Motor.CharacterUp)
                    //        .normalized;
                    //    _lookInputVector = _moveInputVector;
                    //    MaxStableMoveSpeed = 8f;
                    //}
                    ////While inside the walk radius...
                    //else
                    //{
                    //    Vector3 projectedPlayerVelocity =
                    //        Vector3.ProjectOnPlane(playerVelocity, Motor.CharacterUp).normalized;
                    //    walkRadius = 3.75f; //3.2f
                    //    //Check if the pet is in front of the player. *FIX
                    //    float angleComparison = Vector3.Dot(projectedPlayerVelocity, playerDir.normalized);
                    //    
                    //    //If the pet is in front of the player, stop moving.
                    //    if (angleComparison <= -.35f)
                    //    {
                    //        //*Fix: Move this check separate from the checks below, then...
                    //        //Only set this to true if the player's velocity is very low AND in stop radius, then...
                    //        //While the player's velocity REMAINS low, wait for x amount of time while outside the
                    //        //angleComparison before returning to movement. This will create a delay AND prevent
                    //        //some jittery movement.
                    //        _moveInputVector = Vector3.zero;
                    //    }
                    //    //If the player's velocity is high...
                    //    if (playerVelocity.sqrMagnitude >= Mathf.Pow(4f, 2))
                    //    {
                    //        //Change speed depending how close the pet is to the side point.
                    //        if (playerSideDir.sqrMagnitude >= Mathf.Pow(2.5f, 2))
                    //        {
                    //            if (targetMoveSpeed < 8.5f)
                    //            {
                    //                targetMoveSpeed += 8f * Time.deltaTime;
                    //            }
                    //        }
                    //        else if (playerSideDir.sqrMagnitude < Mathf.Pow(1.5f, 2))
                    //        {
                    //            if (targetMoveSpeed > 7f)
                    //            {
                    //                targetMoveSpeed -= 8f * Time.deltaTime;
                    //            }
                    //        }
//
                    //        //In the case that the pet is inbetween both of the above checks, keep the moveSpeed
                    //        //at whatever targetMoveSpeed it was set to.
                    //        MaxStableMoveSpeed = targetMoveSpeed;
                    //    }
                    //    //If the player's velocity is within walking speed...
                    //    else if (playerVelocity.sqrMagnitude < Mathf.Pow(4f, 2) &&
                    //             playerVelocity.sqrMagnitude > Mathf.Pow(2.5f, 2))
                    //    {
                    //        //Enable walking.
                    //        MaxStableMoveSpeed = 3f;
                    //    }
                    //    //If the player's velocity is very low...
                    //    
                    //    //(Moved the distance to stopRadius check below the deviation checking.)
//
                    //    //Movement that occurs while the pet is following alongside the player.
                    //    Vector3 test = Vector3.ProjectOnPlane(playerDir, playerCharForward);
                    //    Vector3 playerFrontAndBackDir = Vector3.ProjectOnPlane(test, Motor.CharacterUp);
                    //    
                    //    //Allow deviation while the speed is above walking speed (Temporary)
                    //    if (playerVelocity.sqrMagnitude > Mathf.Pow(2.5f, 2))
                    //    {
                    //        //---DEVIATION MOVEMENT---
                    //        //If the player distance is near the walk radius...
                    //        if (distanceToPlayer > Mathf.Pow(3f, 2))
                    //        {
                    //            //Angle check for increasing moveSpeed. (Make sure to have it last shortly after coming in.)
                    //            //Check the moveSpeed angle.
                    //            if (angleComparison <= 1f && angleComparison > .5f)
                    //            {
                    //                if (MaxStableMoveSpeed < 8.5f)
                    //                {
                    //                    Debug.Log("Increasing Movespeed");
                    //                    MaxStableMoveSpeed += 8f * Time.deltaTime;
                    //                }
                    //            }
                    //            //Check the deviation angle
                    //            if (angleComparison <= .7f && angleComparison >= -.7f && _canForceDeviation)
                    //            {
                    //                _canForceDeviation = false;
                    //                canChangeDeviationDirection = true;
                    //                _shouldForceDeviationCloser = true;
                    //            }
                    //        }
                    //            
                    //        //Check if deviation can occur after its coroutine ends.
                    //        if (canChangeDeviationDirection)
                    //        {
                    //            canChangeDeviationDirection = false;
                    //            lastMoveInputDeviationValue = 0f;
                    //                
                    //            float randDeviateAmount = UnityEngine.Random.Range(.75f, .95f);
                    //            if (_shouldForceDeviationCloser)
                    //            {
                    //                _shouldForceDeviationCloser = false;
                    //                Debug.Log("Forcing move closer.");
                    //                deviationDir = Vector3.Slerp(playerFrontAndBackDir, playerCharForward, randDeviateAmount);
                    //            }
                    //            else
                    //            {
                    //                //If too close to the player, deviate away.
                    //                if (playerFrontAndBackDir.sqrMagnitude < Mathf.Pow(1.5f, 2))
                    //                {
                    //                    Debug.Log("Moving AWAY");
                    //                    deviationDir = Vector3.Slerp(-playerFrontAndBackDir, playerCharForward, randDeviateAmount);
                    //                }
                    //                //If too far away to the player, deviate closer.
                    //                else if (playerFrontAndBackDir.sqrMagnitude > Mathf.Pow(2.5f, 2))
                    //                {
                    //                    Debug.Log("Moving closer.");
                    //                    deviationDir = Vector3.Slerp(playerFrontAndBackDir, playerCharForward, randDeviateAmount);
                    //                }
                    //                //Otherwise, deviate anywhere.
                    //                else
                    //                {
                    //                    Debug.Log("Moving away anyway cuz middle");
                    //                    deviationDir = Vector3.Slerp(-playerFrontAndBackDir, playerCharForward, randDeviateAmount);
                    //                }
                    //            }
                    //            float randTime = UnityEngine.Random.Range(.85f, 1.5f);
                    //            StopActiveCoroutine(ChangeDeviationDirectionCoroutine);
                    //            ChangeDeviationDirectionCoroutine = StartCoroutine(ChangeDeviationDirection(randTime));
                    //        }
    //
                    //        //Smoothly (and randomly) move between deviation values.
                    //        if (lastMoveInputDeviationValue < 1f)
                    //        {
                    //            lastMoveInputDeviationValue += Time.deltaTime;
                    //        }
                    //        else
                    //        {
                    //            lastMoveInputDeviationValue -= Time.deltaTime;
                    //        }
    //
                    //        _moveInputVector = Vector3.Slerp(_moveInputVector, deviationDir, lastMoveInputDeviationValue).normalized;
                    //        Debug.DrawRay(playerPosition, Vector3.ClampMagnitude(-playerDir, 3f));
                    //    }
                    //    else if (playerVelocity.sqrMagnitude < Mathf.Pow(2.5f, 2))
                    //    {
                    //        //(Temporary) move forward until the pet is close to the sideDir.
                    //        MaxStableMoveSpeed = 3f;
//
                    //        if (playerSideDir.sqrMagnitude >= Mathf.Pow(2f, 2))
                    //        {
                    //            _moveInputVector = playerCharForward.normalized;
                    //        }
                    //        else
                    //        {
                    //            _moveInputVector = Vector3.zero;
                    //        }
                    //        
                    //        //Change this code below so it only runs after the pet enters the walk radius.
                    //        //(This code portion was previously checked above deviation.)
                    //        //if (distanceToPlayer > Mathf.Pow(stopRadius, 2))
                    //        //{
                    //        //    MaxStableMoveSpeed = 3f;
                    //        //}
                    //        //else
                    //        //{
                    //        //    //Stop moving.
                    //        //    _moveInputVector = Vector3.zero;
                    //        //}
                    //    }
                    //    _lookInputVector = _moveInputVector;
                    //    Debug.Log(playerVelocity.magnitude);
                    //}
                    
                    _lookInputVector = _moveInputVector;
                    break;
                }
            }
        }

        /// <summary>
        /// (Called by KinematicCharacterMotor during its update cycle)
        /// This is called before the character begins its movement update
        /// </summary>
        public void BeforeCharacterUpdate(float deltaTime)
        {
            switch (CurrentPetState)
            {
                case PetState.Default:
                {
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
            switch (CurrentPetState)
            {
                case PetState.Default:
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
                currentVelocity = Motor.GetDirectionTangentToSurface(currentVelocity, effectiveGroundNormal) *
                                  currentVelocityMagnitude;

                // Calculate target velocity
                Vector3 inputRight =
                    Vector3.Cross(_moveInputVector, Motor.CharacterUp); //Perpendicular from input and up direction.
                Vector3 reorientedInput = Vector3.Cross(effectiveGroundNormal, inputRight).normalized *
                                          _moveInputVector.magnitude; //Perpendicular from ground normal and previous.
                Vector3 targetMovementVelocity = reorientedInput * MaxStableMoveSpeed; //Multiply it by MoveSpeed.
                //The target velocity has nothing to do with accel.

                // Smooth movement Velocity
                currentVelocity = Vector3.Lerp(currentVelocity, targetMovementVelocity,
                    1f - Mathf.Exp(-StableMovementSharpness * deltaTime)); //Smaller each frame

            }
            // Air movement
            else //"If not grounded..."
            {
                // Add move input
                Vector3 addedVelocity = new Vector3();
                if (_moveInputVector.sqrMagnitude > 0f) //"If there's input..." (Faster than .magnitude)
                {
                    addedVelocity =
                        _moveInputVector * AirAccelerationSpeed * deltaTime; //Multiply by air acceleration (if any).

                    // Prevent air movement from making you move up steep sloped walls
                    if (Motor.GroundingStatus.FoundAnyGround)
                    {
                        Vector3 perpenticularObstructionNormal = Vector3
                            .Cross(Vector3.Cross(Motor.CharacterUp, Motor.GroundingStatus.GroundNormal),
                                Motor.CharacterUp).normalized;
                        addedVelocity = Vector3.ProjectOnPlane(addedVelocity, perpenticularObstructionNormal);
                    }

                    // Limit air movement from inputs to a certain maximum, without limiting the total air move speed from momentum, gravity or other forces
                    Vector3 resultingVelOnInputsPlane =
                        Vector3.ProjectOnPlane(currentVelocity + addedVelocity, Motor.CharacterUp);
                    Debug.DrawLine(transform.position, transform.position + resultingVelOnInputsPlane * 2, Color.red);

                    //"If the direction where we want to go has a magnitude greater than move speed AND the dot product angle is greater than 0..."
                    //(The dot product is the angle comparing the two magnitudes. Facing same direction = 1, opposite directions = -1, perpendicular = 0)
                    if (resultingVelOnInputsPlane.magnitude > MaxAirMoveSpeed &&
                        Vector3.Dot(_moveInputVector, resultingVelOnInputsPlane) >= MaxAirMoveSpeed + .4f
                    ) //Default: >= 0
                    {
                        addedVelocity = Vector3.zero;
                    }
                    else
                    {
                        Vector3 velOnInputsPlane = Vector3.ProjectOnPlane(currentVelocity, Motor.CharacterUp);
                        Vector3 clampedResultingVelOnInputsPlane =
                            Vector3.ClampMagnitude(resultingVelOnInputsPlane, MaxAirMoveSpeed);
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
                    Vector3 resultingVelOnInputsPlane =
                        Vector3.ProjectOnPlane(currentVelocity + addedVelocity,
                            Motor.CharacterUp); //Direction between the two velocities and laid perpendicular to CharacterUp
                    Vector3 velOnInputsPlane = Vector3.ProjectOnPlane(currentVelocity, Motor.CharacterUp); //Similar
                    Vector3 testClampedVelOnInputsPlane =
                        Vector3.ClampMagnitude(velOnInputsPlane, MaxAirMoveSpeed); //Prevents exponential addition.
                    Vector3 clampedResultingVelOnInputsPlane =
                        Vector3.ClampMagnitude(resultingVelOnInputsPlane,
                            MaxAirMoveSpeed); //Clamp the magnitude from adding both velocities.
                    Vector3 subtractedVelocity =
                        clampedResultingVelOnInputsPlane -
                        testClampedVelOnInputsPlane; //Take the added velocities and subtract the vector3 of the current velocity.
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
            Vector3 playerPosition = player.Motor.transform.position;

            switch (CurrentPetState)
            {
                case PetState.Default:
                {
                    //Transition to literally any other state we want.

                    switch (currentDefaultState)
                    {
                        case PetDefaultState.Run:
                        {
                            //Transition to walk if speed goes below x.
                            //Set crouched sub-state to crouchedRun.
                            break;
                        }
                        case PetDefaultState.Walk:
                        {
                            //Transition to run if speed goes above x, OR...
                            //Transition to idle if close enough.
                            //Set crouched sub-state to crouchedRun.
                            break;
                        }
                        case PetDefaultState.Idle:
                        {
                            //Transition to run once x distance is met OR player's velocity goes above x.
                            //Set crouched sub-state to crouchedIdle.
                            break;
                        }
                    }

                    break;
                }
                case PetState.Crouched:
                {
                    switch (currentCrouchedState)
                    {
                        case PetCrouchedState.Run:
                        {
                            //Set default sub-state to walk.
                            //Transition to crouchedIdle if close enough.
                            break;
                        }
                        case PetCrouchedState.Idle:
                        {
                            //Transition to crouchedRun if player goes above x distance or x velocity.
                            //Set default sub-state to idle.
                            break;
                        }
                    }
                    break;
                }
                case PetState.MoveToPlayer:
                {
                    //Transition to default if the pet re-enters the (now smaller) follow radius.
                    break;
                }
                case PetState.FollowPath:
                {
                    //Transition to MoveToPlayer if the pet re-enters line of sight of the player.
                    break;
                }
            }
            
            //Switch to default state if in midair
            if (!Motor.GroundingStatus.IsStableOnGround)
            {
                if (CurrentPetState != PetState.Default)
                {
                    StartCoroutine(TimeBeforeFallingStateCoroutine());
                }
            }
        }

        //Check line of sight to target.
        public bool LineOfSightToTarget(Vector3 playerPos)
        {
            //Check line of sight to the target.
            float maxDistance = 12f;

            //Vector3 adjustedPosition = targetPos + Motor.CharacterUp; //Check slightly above the transform position.
            Vector3 targetDirection = playerPos - Motor.Transform.position;

            //Debug.DrawLine(Motor.transform.position, playerPos);
            if (!Physics.Linecast(Motor.Transform.position, playerPos, WallLayerMask))
            {
                return true;
            }
            return false;
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
            switch (CurrentPetState)
            {
                case PetState.Default:
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

        public void StopActiveCoroutine(Coroutine coroutine)
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
            }
        }

        // COROUTINES

        private IEnumerator ChangeDeviationDirection(float time)
        {
            yield return CustomTimer.Timer(time);
            canChangeDeviationDirection = true;
        }
        private IEnumerator StateTransitionDelay(PetState petState, float time)
        {
            yield return CustomTimer.Timer(time);
            TransitionToState(petState);
        }

        private IEnumerator TimeBeforeFallingStateCoroutine()
        {
            yield return CustomTimer.Timer(.1f);
            TransitionToState(PetState.Default);
        }
    }
}

//OBSTACLE AVOIDANCE SYSTEM
//1. Avoiding walls.
//METHOD 1
    //Send out a bunch of rays from the player's position outwards towards the left and right side of the player's
        //velocity. (Kind of like a "whisker" shape.)
    //If any of the rays detects a wall, the pet will avoid moving on that side of the player.
    //If both sides detect a wall, the pet will move behind the player.
//(NOTE: Make sure the rays extend out from the middle of the player and ignore climbable slopes.)
//METHOD 2
    //Send out a ray from the pet's velocity.
    //If the ray intersects with a wall, lerp towards the player's direction until the ray isn't intersecting anymore.
        //(Going no further than the playerDir.) (If the ray is still blocked at playerDir, then followPath should
        //automatically take over and use pathfinding.)
//NOTES:
    //Under the assumption the player can be surrounded by walls on both sides, the pet would still move and look
    //directly at the player, causing the pet to move relatively straight.

//2. Avoiding ledges.
//METHOD 1
    //Send out a bunch of rays from the pet's position that extend downwards x distance until they detect ground.
    //If up to 2-3 adjacent rays don't detect any ground, the pet will avoid that area.
    //Store the distance from the ledge by storing the dropoff point the moment no ground is detected.
    //If the player is jumping off a ledge or the player's position is below the dropoff point, the pet will ignore
        //ledges.
    //(If we want the pet to jump when going off a ledge, get the distance from the ledge until x away and jump off.)
//METHOD 2
    //Just store the velocity ray from the pet's position and extend downwards x distance until it detects ground.
    //Run similar checks as before without needing multiple rays.

//3. Recognizing slopes.
//METHOD 1
    //Send a ray out from the pet's velocity and player's velocity.
    //If the contact point normal is within x degrees (can be walked on) and large compared to the pet's normal,
        //move to the player.
    //...OR, if the player's height compared to the pet's is too high (just under step height) move to the player.
    
//REMEMBER TO PONDER ABOUT ANY POTENTIAL ISSUES WITH THESE CHECKS.    

//SETTING THE MOVE INPUT
//These obstacle checks will run (or SHOULD be able to run) in any default state.
//Under the assumption the pet will only move while behind the player, we can override the moveInput and always lerp
    //from the current moveInput towards the player until there's no longer an obstacle.
//We could also set our own unique move input to tell the pet to not only move towards the player, but to mimic
    //movement (following instead of moving towards) until no obstacles are present. It would have a very similar
    //effect, but it could have a cleaner feel to it. I dunno.    
    
//IN A FEW WORDS...
//1. Send a ray out from the pet's velocity. Is there a wall? Move towards the player until there isn't a wall.
//2. Send a ray out from the pet's velocity. Is there ground detection? Move towards the player if ground can't be
    //detected.
//3. Send a ray out from the pet's velocity and player's velocity. Does each normal bounce in a similar direction? Is
    //the player too high compared to the pet? Move towards the player.
//Never move further than the direction of the player. 