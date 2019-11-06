using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KinematicCharacterController;
using System;
using KinematicCharacterController.Nate;

//Movement controller for the fox.

namespace KinematicCharacterController.PetControllerv3
{
    public enum PetState //Character states (Walking, crouching, attacking, etc)
    {
        Default,
        MoveStraightToPlayer,
        FollowPath,
        Crouched,
        SwordAttack,
        SpinAttack,
        RollAttack,
    }

    public enum PetDefaultState
    {
        Idle,
        Walk,
        Run
    }

    public enum PetDeviationState
    {
        Away,
        Closer,
        Mimic,
        Random,
        DistanceBased,
    }

    public class PetControllerv3 : MonoBehaviour, ICharacterController
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
        private Vector3 tempPlayerDir;
        private Vector3 checkedPlayerDir;

        //Player Vectors
        private Vector3 playerPosition;
        private Vector3 playerDir;
        private Vector3 playerVelocity;
        private Vector3 rightDir;
        private Vector3 leftDir;
        private Vector3 playerSideDir;
        private float distanceToPlayer;
        
        private bool _shouldBeCrouching = false;
        private bool _isCrouching = false;
        private bool _cannotUncrouch = false;
        private bool _canCheckPathfinding = true;
        private bool _canSetFollowPoint;
        private bool _canGetLastMoveInput;
        private bool _shouldForceDeviationCloser;
        private bool _canAdjustSpeed;
        private bool _canAllowMovementDeviation;
        private bool _shouldSlowDownInFrontOfPlayer;
        //private bool _canResetDeviationMovement;

        private float moveInputDeviationValue;
        private float lastMoveInputDeviationValue;
        private float walkRadius;
        private float targetMoveSpeed;
        private float playerMoveSpeed;
        private float randDeviateAmount = 1f;
        private float testLerpToZero = 0f;

        private Vector3 lastInnerNormal = Vector3.zero;
        private Vector3 lastOuterNormal = Vector3.zero;

        public UnitFollowNew pathfinding;
        public LayerMask WallLayerMask;

        //Enums
        public PetDefaultState currentDefaultState;
        public PetDeviationState currentDeviationState;

        //Coroutines
        private Coroutine StateTransitionDelayCoroutine;
        private bool canStartTransition = true;
        private Coroutine TestPlayerPositionDelayCoroutine;
        private Coroutine TestSetInitialMoveInputDelayCoroutine;
        private bool canStartMoveInputDelay = true;
        private Coroutine ChangeDeviationDirectionCoroutine;
        private bool canChangeDeviationDirection;
        private Coroutine ForceDeviationChangeCoroutine;
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
            GetPlayerValues();
            transformPosition = Motor.transform.position;
            playerMoveSpeed = player.MaxStableMoveSpeed;
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

        //public void GetPlayerState()

        /// <summary>
        /// Event when entering a state
        /// </summary>
        public void OnStateEnter(PetState state, PetState fromState)
        {
            switch (state)
            {
                case PetState.Default:
                {
                    canChangeDeviationDirection = true;
                    _canForceDeviation = true;
                    MaxStableMoveSpeed = 7f;
                    _canSetFollowPoint = true;
                    currentDefaultState = PetDefaultState.Idle;
                    break;
                }
                case PetState.Crouched:
                {
                    MaxStableMoveSpeed = 3f;
                    _canSetFollowPoint = true;
                    currentDefaultState = PetDefaultState.Idle;
                    StableMovementSharpness = 7f;

                    //Set dimensions and scale
                    Motor.SetCapsuleDimensions(0.5f, 1f, 0.25f); //Scales the hitbox.
                    MeshRoot.localScale = new Vector3(1f, 0.35f, 1.2f); //Scales the mesh root.
                    break;
                }
                case PetState.MoveStraightToPlayer:
                {
                    walkRadius = 2.8f;
                    MaxStableMoveSpeed = 8f;
                    break;
                }
            }

            targetMoveSpeed = MaxStableMoveSpeed;
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
                    MaxStableMoveSpeed = 7f;
                    StopActiveCoroutine(ForceDeviationChangeCoroutine);
                    break;
                }
                case PetState.Crouched:
                {
                    MaxStableMoveSpeed = 3f;
                    StopActiveCoroutine(ForceDeviationChangeCoroutine);
                    Motor.SetCapsuleDimensions(0.5f, 1f, .25f); //Scales the hitbox.
                    MeshRoot.localScale = new Vector3(1f, .5f, 1.2f); //Scales the mesh root.
                    break;
                }
                case PetState.FollowPath:
                {
                    _canCheckPathfinding = true;
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
            //Bool checks for allowing speed adjustments, deviation, or slowing down while in front of the player.
            Vector3 projectedPlayerVelocity =
                Vector3.ProjectOnPlane(playerVelocity, Motor.CharacterUp).normalized;
            walkRadius = 3.75f; //3.2f
            //Check if the pet is in front of the player. *FIX
            float angleComparison = Vector3.Dot(projectedPlayerVelocity, playerDir.normalized);
            
            switch (CurrentPetState)
            {
                case PetState.MoveStraightToPlayer:
                {
                    //Move toward a point perpendicular to the player's forward direction that extends x distance.
                    //This allows for leniency when running back to the player.
                    if (rightDir.sqrMagnitude < leftDir.sqrMagnitude)
                    {
                        targetDirection = rightDir;
                    }
                    else
                    {
                        targetDirection = leftDir;
                    }
                    
                    _moveInputVector = Vector3.ProjectOnPlane(targetDirection.normalized, Motor.CharacterUp)
                        .normalized;
                    break;
                }
                case PetState.Default:
                {
                    _shouldSlowDownInFrontOfPlayer = true;
                    switch (currentDefaultState)
                    {
                        case PetDefaultState.Run:
                        {
                            MaxStableMoveSpeed = 7f;
                            //_canAllowMovementDeviation = true;
                            _canAdjustSpeed = true;
                            if (canChangeDeviationDirection) //Reset this anytime we want a new direction.
                            {
                                canChangeDeviationDirection = false;
                                ResetDeviationDir();
                                randDeviateAmount = UnityEngine.Random.Range(.75f, .95f);
                                float randTime = UnityEngine.Random.Range(.85f, 1.5f);
                                StopActiveCoroutine(ChangeDeviationDirectionCoroutine);
                                ChangeDeviationDirectionCoroutine = StartCoroutine(ChangeDeviationDirection(randTime));
                                
                                //Set a distance that only gets set from the start
                                Vector3 test = Vector3.ProjectOnPlane(tempPlayerDir, playerVelocity);
                                checkedPlayerDir = Vector3.ProjectOnPlane(test, Motor.CharacterUp);
                            }
                            
                            //If too close to the player, deviate away.
                            if (checkedPlayerDir.sqrMagnitude < Mathf.Pow(1f, 2))
                            {
                                _moveInputVector = DeviationDirectionInput(PetDeviationState.Away, _moveInputVector,
                                    randDeviateAmount);
                            }
                            //If too far away to the player, deviate closer.
                            else if (checkedPlayerDir.sqrMagnitude > Mathf.Pow(2.5f, 2))
                            {
                                _moveInputVector = DeviationDirectionInput(PetDeviationState.Closer, _moveInputVector,
                                    randDeviateAmount);
                            }
                            //Otherwise, deviate closer (for now).
                            else
                            {
                                _moveInputVector = DeviationDirectionInput(PetDeviationState.Closer, _moveInputVector,
                                    randDeviateAmount);
                            }
                            
                            if (distanceToPlayer > Mathf.Pow(3f, 2))
                            {
                                //Check the deviation angle
                                if (angleComparison <= .7f && angleComparison >= -.7f && _canForceDeviation)
                                {
                                    _canForceDeviation = false;
                                    _moveInputVector = DeviationDirectionInput(PetDeviationState.Closer, _moveInputVector,
                                        randDeviateAmount);
                                }
                                else
                                {
                                    _canForceDeviation = true;
                                }
                            }
                            else
                            {
                                _canForceDeviation = true;
                            }
                            
                            break;
                        }
                        case PetDefaultState.Walk:
                        {
                            MaxStableMoveSpeed = 3f;
                            //_canAllowMovementDeviation = false;
                            _canAdjustSpeed = false; //Eventually change once moveSpeed is correctly adjusted
                            _moveInputVector = playerVelocity.normalized;
                            break;
                        }
                        case PetDefaultState.Idle:
                        {
                            //_canAllowMovementDeviation = false;
                            _canAdjustSpeed = false;
                            _moveInputVector = Vector3.zero;
                            break;
                        }
                    }
                    break;
                }
                case PetState.Crouched:
                {
                    switch (currentDefaultState)
                    {
                        case PetDefaultState.Walk:
                        {
                            _moveInputVector = playerVelocity.normalized;
                            //Move straight, but with deviation checks.
                            break;
                        }
                        case PetDefaultState.Idle:
                        {
                            _moveInputVector = Vector3.zero;
                            break;
                        }
                    }
                     
                    break;
                }
                case PetState.FollowPath:
                {
                    //Vector3 pathPosition = pathfinding.targetPathPosition;
                    //_moveInputVector = Vector3.ProjectOnPlane(pathPosition, Motor.CharacterUp);
                    //_lookInputVector = _moveInputVector;
                    break;
                }
            }

            //Adjusting speed.
            if (_canAdjustSpeed)
            {
                //Change speed depending how close the pet is to the side point.
                if (playerSideDir.sqrMagnitude >= Mathf.Pow(2.5f, 2))
                {
                    if (targetMoveSpeed < 8.5f)
                    {
                        targetMoveSpeed += 8f * Time.deltaTime;
                    }
                }
                else if (playerSideDir.sqrMagnitude < Mathf.Pow(1.5f, 2))
                {
                    if (targetMoveSpeed > 7f)
                    {
                        targetMoveSpeed -= 8f * Time.deltaTime;
                    }
                }
                            
                //Angle check for increasing moveSpeed. (Make sure to have it last shortly after coming in.)
                //Check the moveSpeed angle.
                if (distanceToPlayer > Mathf.Pow(3f, 2))
                {
                    if (angleComparison <= 1f && angleComparison > .5f)
                    {
                        if (targetMoveSpeed < 8.5f)
                        {
                            Debug.Log("Increasing Movespeed");
                           targetMoveSpeed += 8f * Time.deltaTime;
                        }
                    }
                }
                //In the case that the pet is inbetween both of the above checks, keep the moveSpeed
                //at whatever targetMoveSpeed it was set to.
                //MaxStableMoveSpeed = targetMoveSpeed;
            }

            //MoveInput with deviation.
            if (_canAllowMovementDeviation) //(REMOVE THIS)
            {
                //---DEVIATION MOVEMENT---
                //If the player distance is near the walk radius...

                            
                //GOAL: Update the deviationDir, BUT keep the pet moving towards one side.
                //Check if deviation can occur after its coroutine ends.
                //if (canChangeDeviationDirection)
                //{
                    ////_canGetLastMoveInput = true;
                    //canChangeDeviationDirection = false;
                    //lastMoveInputDeviationValue = 0f;
                    //tempPlayerDir = playerDir; //Store the side of the player that we're on.
                    //            
                    //if (_shouldForceDeviationCloser)
                    //{
                    //    _shouldForceDeviationCloser = false;
                    //    Debug.Log("Forcing move closer.");
                    //    //deviationDir = Vector3.Slerp(playerFrontAndBackDir, playerCharForward, randDeviateAmount);
                    //    currentDeviationState = PetDeviationState.Closer;
                    //    ForceDeviationChangeCoroutine = StartCoroutine(ForceDeviationChange());
                    //}
                    //else
                    //{
                    //    //Direction that checks how far the pet is to the left and right side of the player.
                    //    Vector3 test = Vector3.ProjectOnPlane(playerDir, playerVelocity);
                    //    Vector3 playerFrontAndBackDir = Vector3.ProjectOnPlane(test, Motor.CharacterUp);
                    //                
                    //    //If too close to the player, deviate away.
                    //    if (playerFrontAndBackDir.sqrMagnitude < Mathf.Pow(1f, 2))
                    //    {
                    //        Debug.Log("Moving AWAY");
                    //        currentDeviationState = PetDeviationState.Away;
                    //    }
                    //    //If too far away to the player, deviate closer.
                    //    else if (playerFrontAndBackDir.sqrMagnitude > Mathf.Pow(2.5f, 2))
                    //    {
                    //        Debug.Log("Moving closer.");
                    //        currentDeviationState = PetDeviationState.Closer;
                    //    }
                    //    //Otherwise, deviate anywhere.
                    //    else
                    //    {
                    //        Debug.Log("Moving closer (Run state)");
                    //        currentDeviationState = PetDeviationState.Random;
                    //    }
                    //}
                    //}
                
                

                //This state checking allows the deviationDir to update the player's direction
                //each frame while still maintaining the same direction until the deviation changes.
                //switch (currentDeviationState)
                //{
                //    case PetDeviationState.Away:
                //    {
                //        deviationDir = Vector3.Slerp(-playerFrontAndBackDirNew, playerVelocity, randDeviateAmount);
                //        break;
                //    }
                //    case PetDeviationState.Closer:
                //    {
                //        deviationDir = Vector3.Slerp(playerFrontAndBackDirNew, playerVelocity, randDeviateAmount);
                //        break;
                //    }
                //    case PetDeviationState.Random: //For now, only moves closer
                //    {
                //        deviationDir = Vector3.Slerp(playerFrontAndBackDirNew, playerVelocity, randDeviateAmount);
                //        break;
                //    }
                //}

                //Smoothly (and randomly) move between deviation values.
                //if (lastMoveInputDeviationValue < 1f)
                //{
                //    lastMoveInputDeviationValue += Time.deltaTime;
                //}
                //else
                //{
                //    lastMoveInputDeviationValue = 1f;
                //}
    
                //_moveInputVector = Vector3.Slerp(_moveInputVector, deviationDir, lastMoveInputDeviationValue).normalized;
            }
                        
            //Slowing down movement while in front of the player.
            if (_shouldSlowDownInFrontOfPlayer)
            {
                if (angleComparison <= -.05f)
                {
                    //We could also do a lerp between moveSpeeds. This is mostly a test.
                    testLerpToZero += Time.deltaTime;
                    _moveInputVector = Vector3.Slerp(_moveInputVector, Vector3.zero, testLerpToZero);
                }
                else
                {
                    testLerpToZero = 0; //Reset this!
                } 
            }

            //(Temporary) Set any move input to be horizontal to the pet's up direction.
            //(Temporary) Set any look input to follow move input.
            _moveInputVector = Vector3.ProjectOnPlane(_moveInputVector, Motor.CharacterUp);
            _lookInputVector = _moveInputVector;
        }

        private void ResetDeviationDir()
        {
            //Reset the deviation value and get the player direction.
            //canChangeDeviationDirection = false;
            lastMoveInputDeviationValue = 0;
            tempPlayerDir = playerDir;
        }
        
        //Returns a new movement input that deviates in a direction relative to the player's velocity.
        public Vector3 DeviationDirectionInput(PetDeviationState state, Vector3 moveInput, float deviationAmount)
        {
            Vector3 testNew = Vector3.ProjectOnPlane(tempPlayerDir, playerVelocity);
            Vector3 playerFrontAndBackDirNew = Vector3.ProjectOnPlane(testNew, Motor.CharacterUp);
            
            //Set a new input based on the direction to the player and deviation amount.
            //(1 = mimicking player velocity. 0 = completely away or completely towards.)
            switch (state)
            {
                case PetDeviationState.Away:
                {
                    deviationDir = Vector3.Slerp(-playerFrontAndBackDirNew, playerVelocity, deviationAmount);
                    break;
                }
                case PetDeviationState.Closer:
                {
                    deviationDir = Vector3.Slerp(playerFrontAndBackDirNew, playerVelocity, deviationAmount);
                    break;
                }
                case PetDeviationState.Mimic:
                {
                    deviationDir = playerVelocity;
                    break;
                }
                case PetDeviationState.Random: //For now, only moves closer
                {
                    deviationDir = Vector3.Slerp(playerFrontAndBackDirNew, playerVelocity, deviationAmount);
                    break;
                }
            }

            //Smoothly (and randomly) move between deviation values.
            if (lastMoveInputDeviationValue < 1f)
            {
                lastMoveInputDeviationValue += Time.deltaTime;
            }
            else
            {
                lastMoveInputDeviationValue = 1f;
            }
    
            return Vector3.Slerp(moveInput, deviationDir, lastMoveInputDeviationValue).normalized;
        }

        /// <summary>
        /// (Called by KinematicCharacterMotor during its update cycle)
        /// This is called before the character begins its movement update
        /// </summary>
        public void BeforeCharacterUpdate(float deltaTime)
        {
            //Player state checking.
            if (CurrentPetState != PetState.FollowPath)
            {
                //NewCharacterState != player.CurrentCharacterState) //WHEN THE PLAYER STATE CHANGES...
                //
                //canStartTransition = true; //Allow pet state transitioning (once)
                //NewCharacterState = player.CurrentCharacterState;
                //

                //canStartTransition)
                //
                //canStartTransition = false;
                //switch (player.CurrentCharacterState)
                //{
                //    case CharacterState.Default:
                //    {
                //        StateTransitionDelayCoroutine =
                //            StartCoroutine(StateTransitionDelay(PetState.Default, .12f));
                //        break;
                //    }
                //    case CharacterState.Crouched:
                //    {
                //        StateTransitionDelayCoroutine =
                //            StartCoroutine(StateTransitionDelay(PetState.Crouched, .12f));
                //        break;
                //    }
                //    case CharacterState.SwordAttack:
                //    {
                //        StateTransitionDelayCoroutine =
                //            StartCoroutine(StateTransitionDelay(PetState.SwordAttack, .12f));
                //        break;
                //    }
                //    case CharacterState.RollAttack:
                //    {
                //        TransitionToState(PetState.RollAttack); //Instant
                //        break;
                //    }
                //}
            }
        }

        /// <summary>
        /// (Called by KinematicCharacterMotor during its update cycle)
        /// This is where you tell your character what its rotation should be right now. 
        /// This is the ONLY place where you should set the character's rotation
        /// </summary>
        public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            if (_lookInputVector.sqrMagnitude > 0f && OrientationSharpness > 0f)
            {
                // Smoothly interpolate from current to target look direction
                Vector3 smoothedLookInputDirection = Vector3.Slerp(Motor.CharacterForward, _lookInputVector,
                    1 - Mathf.Exp(-OrientationSharpness * deltaTime)).normalized;
                // Set the current rotation (which will be used by the KinematicCharacterMotor)
                currentRotation = Quaternion.LookRotation(smoothedLookInputDirection, Motor.CharacterUp);

                if (OrientTowardsGravity)
                {
                    // Rotate from current up to invert gravity
                    currentRotation = Quaternion.FromToRotation((currentRotation * Vector3.up), -Gravity) *
                                      currentRotation;
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
                Vector3 targetMovementVelocity = reorientedInput * targetMoveSpeed; //Multiply it by MoveSpeed.
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
            playerPosition = player.Motor.transform.position;
            
            //While in the default (or crouched) state...
            if (CurrentPetState == PetState.Default || CurrentPetState == PetState.Crouched)
            {
                //While outside the walk radius...
                if (distanceToPlayer > Mathf.Pow(walkRadius, 2))
                {
                    //TransitionToState(PetState.MoveStraightToPlayer);
                }

                //If player has high velocity, transition to run.
                if (playerVelocity.sqrMagnitude >= Mathf.Pow(3f, 2))
                {
                    currentDefaultState = PetDefaultState.Run;
                }
                else if (playerVelocity.sqrMagnitude < Mathf.Pow(3f, 2))
                {
                    //If player's side position is too far, walk.
                    if (playerSideDir.sqrMagnitude >= Mathf.Pow(2f, 2))
                    {
                        currentDefaultState = PetDefaultState.Walk;
                    }
                    //Once close enough, go to idle.
                    else
                    {
                        currentDefaultState = PetDefaultState.Idle;
                    }
                }
                
                //Check pathfinding.
                if (!LineOfSightToTarget(playerPosition))
                {
                    if (_canCheckPathfinding)
                    {
                        _canCheckPathfinding = false;
                        pathfinding.CheckIfCanFollowPath(playerPosition);
                    }

                    //If the player can't be reached...
                    if (!pathfinding.CheckingPath && !pathfinding.PathIsActive)
                    {
                        //WARP!
                        Motor.SetPosition(playerPosition);
                    }

                    if (pathfinding.PathIsActive)
                    {
                        TransitionToState(PetState.FollowPath);
                    }
                }
            }
            
            //Pet state checking.
            switch (CurrentPetState)
            {
                //In the default state, set run, walk, and idle sub-states based on velocity.
                //In the crouched state, do the same thing except take out run.
                case PetState.Default:
                {
                    
                    break;
                }
                case PetState.Crouched:
                {
                    break;
                }
                case PetState.MoveStraightToPlayer:
                {
                    //If distance goes within walkRadius...
                        //Transition to default state.
                    break;
                }
                case PetState.FollowPath:
                {
                    if (LineOfSightToTarget(playerPosition))
                    {
                        pathfinding.StopFollowPath();
                        TransitionToState(PetState.MoveStraightToPlayer);
                    }

                    break;
                }
            }
            //Switch to default state if in midair
            if (!Motor.GroundingStatus.IsStableOnGround)
            {
                if (CurrentPetState != PetState.Default)
                {
                    //_timeBeforeFallingStateCoroutine = StartCoroutine(TimeBeforeFallingStateCoroutine());
                    StartCoroutine(TimeBeforeFallingStateCoroutine());
                }
            }
        }

        public void GetPlayerValues()
        {
            //Player info
            playerPosition = player.Motor.transform.position;
            playerDir = playerPosition - Motor.transform.position;
            playerVelocity = player.Motor.Velocity;
            distanceToPlayer = playerDir.sqrMagnitude;

            //Get the left and right sides of the player based on velocity.
            Vector3 playerCharRight = Vector3.ProjectOnPlane(playerVelocity, playerVelocity);
            Vector3 playerCharLeft = -playerCharRight;

            //Set a left and right position next to the player's velocity.
            Vector3 rightPos = playerPosition + playerCharRight;
            Vector3 leftPos = playerPosition + playerCharLeft;;
            rightDir = Vector3.ProjectOnPlane(rightPos - transformPosition, Motor.CharacterUp);
            leftDir = Vector3.ProjectOnPlane(leftPos - transformPosition, Motor.CharacterUp);
            
            //Set a direction that checks how close the pet is behind or in front of the player.
           playerSideDir = Vector3.ProjectOnPlane(Vector3.ProjectOnPlane(playerDir, playerCharRight), Motor.CharacterUp);

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
            else
            {
                return false;
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

        private IEnumerator ForceDeviationChange()
        {
            yield return CustomTimer.Timer(.5f);
            _canForceDeviation = true;
        }

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

//GUIDE TO TELLING SOMETHING TO RUN ONCE
//EXAMPLE 1:
    //if (petIsOutsideAngle)
    //{
    //    if (!canDeviate)
    //    {
    //        canDeviate == true;
    //        (DO EVERYTHING THAT RUNS ONCE)
    //    }
    //}
    //else
    //{
    //    canDeviate == false;
    //}

//EXAMPLE 2:
    //if (canDeviate)
    //{
    //    canDeviate == false;
    //    (DO EVERYTHING THAT RUNS ONCE)
    //    (START A COROUTINE THAT SETS CANDEVIATE BACK TO TRUE)
    //}

//I'M SO TIRED

//TO DO
//PET MOVEMENT
//I DUNNO IT'S JUST VERY MESSY OKAY
//GOAL:
    //Remove any need to call a "deviateCloser" or "deviateAway" function...
    //THEN, we can just set a simple moveInputVector to slerp towards a direction which is deviating slightly away.
    //TO DO THIS, SEPARATE ANY NEW MOVE INPUT INTO A NEW SUB-STATE. The reason we keep getting spaghetti code is because
        //we end up needing to create a million bools each time we want to run something once, like setting the value to
        //0, running a coroutine, setting a random float value, not letting other checks run while the moveInput is
        //currently something else, etc.

//SOMETHING LIKE THIS
//    if (movingCloser)
//    {
//        targetInput = new vector that deviates closer.
//        if (angleComparison)
//        {
//            movingCloser == false;
//            movingAway == true;
//            value = 0;
//        }
//    }
//    if (movingAway)
//    {
//        targetInput = new vector that deviates away.
//        if (angleComparisonTooFar)
//        {
//            movingAway == false;
//            movingCloser == true;
//            value = 0;
//        }
//    }
//    value += Time.deltaTime;
//    moveInputVector = Slerp(moveInputVector, targetInput, value);

//(Just replace movingCloser and movingAway with states that we change.)

//Make sure targetMoveSpeed changes to maxStableMoveSpeed when it switches from run to walk (so it slows down correctly)
//Change the adjustSpeed section so it works for any type of moveSpeed.
//Set up MoveDirectlyToPlayer correctly.

//PET WARPING
//Choose one of 4 pet warp positions: Sides, behind, or right on the player.
    //For each position, send a raycast down to make sure it would teleport the pet on a viable spot.
    //If none are open, teleport right on top of the player.


//PET ATTACK
//1. When the player swings a sword, the pet will make sure it's within x distance of the player. If false, continue
//moving to the player. If true, dash forward towards attack position 1 or 2. Once reached, the pet will swing its tail
//very quickly. (The dash should act as part of the pet's attack.)
//2. Check for a target. This target is determined by 1: An enemy with the most damage taken in a short amount of time,
//and 2: an enemy within the player's attack range. If no target is found, the pet will look in the player's direction.
//Otherwise, it will turn inward towards the targeted enemy.
    //Note: The pet's attack WILL be offset slightly by the player's sword attack.

//OTHER FUTURE IMPLEMENTATIONS
//Bow and arrow attack.
    //When the player equips the bow and arrow, the pet will grab the quiver from the player's back and hold it next to
    //the player's hand. Note: Think about how the player grabs the arrows. Grabbing arrows from a quiver on your back
    //transitions quickly to loading the arrow. How should the pet be holding the quiver bag?
//Roll attack.
    //When the player activates a roll, if the pet is within x distance, the pet will jump from its current position
    //towards the top of the "roll" position. Once reached, the pet will rotate around the outside of the player.
    //Once the roll ends, the pet will fly out from the direction it was rolling. If the pet is not within x distance,
    //the pet will teleport to the top of the roll position.
//Charged swing attack.
    //If within x distance, the pet will crouch down and start charging its tail. Once the charge is released, the pet
    //spins around underneath the player while the player jumps in a swinging motion.

//State transitioning.
//For most states, the pet should have a slight delay before setting it to the player's.
//Sword attack should have only a VERY slight delay.
//Attacks such as sword spin and roll attack should happen at the same time.

//Pet's movement should only lag slightly behind to feel like a separate object, but close enough that its attacks
//feel congruent with the player.

//Precise timing attacks (such as the roll attack) should check to make sure the pet is in position right before the 
//roll animation plays. If the pet isn't in position, the pet should teleport immediately to the player.