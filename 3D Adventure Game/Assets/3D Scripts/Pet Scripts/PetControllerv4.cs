using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KinematicCharacterController;
using System;
using KinematicCharacterController.Nate;

//Movement controller for the fox.

namespace KinematicCharacterController.PetControllerv4
{
    public enum PetState //Character states (Walking, crouching, attacking, etc)
    {
        Default,
        MoveStraightToPlayer,
        FollowPath,
        Crouched,
        WarpToPlayer,
        SwordAttack,
        SpinAttack,
        RollAttack,
    }

    public enum PetDefaultState
    {
        Idle,
        Walk,
        Run,
        StayBehindPlayer
    }

    public enum PetDeviationState
    {
        NoDeviation,
        MoveCloser,
        MoveAway,
    }
    
    public enum PetCrouchedState
    {
        Idle,
        Walk,
        CrouchBehindPlayer,
    }

    public class PetControllerv4 : MonoBehaviour, ICharacterController
    {
        public KinematicCharacterMotor Motor;

        [Header("Stable Movement")] 
        public float MaxStableMoveSpeed = 10f;
        public float StableMovementSharpness = 15f;
        public float OrientationSharpness = 10f;

        [Header("Air Movement")] 
        public float MaxAirMoveSpeed = 100f;
        public float AirAccelerationSpeed = 15f;
        public float Drag = 0.1f;

        [Header("Misc")] 
        public List<Collider> IgnoredColliders = new List<Collider>();
        public bool OrientTowardsGravity = false;
        public Vector3 Gravity = new Vector3(0, -30f, 0);
        public Transform MeshRoot;

        public PetState CurrentPetState;
        public NateCharacterController player;
        private CharacterState NewCharacterState;
        private PetCrouchedState currentCrouchedState;

        private Collider[] _probedColliders = new Collider[8];

        //General vectors
        private Vector3 _moveInputVector;
        private Vector3 _targetMoveInputVector;
        private Vector3 _lookInputVector;
        private Vector3 _internalVelocityAdd = Vector3.zero;
        private Vector3 transformPosition;
        private Vector3 targetDirection;

        //Player Vectors
        private Vector3 playerPosition;
        private Vector3 playerDir;
        private Vector3 playerVelocity;
        private Vector3 projectedPlayerVelocity;
        private Vector3 rightDir;
        private Vector3 leftDir;
        private Vector3 playerDirTowardsVelocity;
        private Vector3 playerDirPerpendicularOfVelocity;
        private Vector3 storedPlayerDir;
        private Vector3 playerPosDelayed;

        //General bools
        private bool _shouldBeCrouching = false;
        private bool _canCheckPathfinding = true;
        private bool _canAdjustSpeed;
        private bool _shouldSlowDownInFrontOfPlayer;

        //General floats
        private float lastMoveInputDeviationValue;
        private float walkRadius;
        private float targetMoveSpeed;
        private float playerMoveSpeed;
        private float randDeviateAmount = 1f;
        private float testLerpToZero = 0f;
        private float angleComparison;
        private float distanceToPlayer;
        private float moveInputSmoothRate;
        private float lineOfSightTimePassed;

        private Vector3 lastInnerNormal = Vector3.zero;
        private Vector3 lastOuterNormal = Vector3.zero;

        public UnitFollowNew pathfinding;
        public LayerMask LineOfSightMask;

        //Current states
        private PetDefaultState currentDefaultState;
        private PetDeviationState currentDeviationState;

        //Coroutines
        private Coroutine ChangeDeviationDirectionCoroutine;
        private bool _canChangeDeviation;
        private Coroutine ForceDeviationChangeCoroutine;
        private bool _forcingMovementTowardsPlayer;
        private Coroutine TimeBeforeWarpingCoroutine;
        private bool canWarpToPlayer;
        private Coroutine TimeBetweenWarpsCoroutine;

        private void Start()
        {
            //Handle initial state
            TransitionToState(PetState.Default);
            currentDefaultState = PetDefaultState.Idle;

            // Assign the characterController to the motor
            Motor.CharacterController = this;
            
            playerPosDelayed = player.transform.position;
            canWarpToPlayer = true;
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
                    _canChangeDeviation = true;
                    MaxStableMoveSpeed = 7f;
                    
                    break;
                }
                case PetState.Crouched:
                {
                    MaxStableMoveSpeed = 3f;
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
                case PetState.FollowPath:
                {
                    _canCheckPathfinding = true;
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
                    break;
                }
                case PetState.Crouched:
                {
                    MaxStableMoveSpeed = 3f;
                    Motor.SetCapsuleDimensions(0.5f, 1f, .25f); //Scales the hitbox.
                    MeshRoot.localScale = new Vector3(1f, .5f, 1.2f); //Scales the mesh root.
                    break;
                }
                case PetState.MoveStraightToPlayer:
                {
                    walkRadius = 3.75f; //3.2f
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
                    
                    _targetMoveInputVector = Vector3.ProjectOnPlane(targetDirection.normalized, Motor.CharacterUp)
                        .normalized;
                    break;
                }
                //Default
                case PetState.Default:
                {
                    _shouldSlowDownInFrontOfPlayer = true;
                    switch (currentDefaultState)
                    {
                        //Default run
                        case PetDefaultState.Run:
                        {
                            MaxStableMoveSpeed = 7f;
                            _canAdjustSpeed = true;

                            //Setting up a direction for deviation.
                            Vector3 deviationDirToMove = new Vector3();
                            Vector3 storedPerpendicularDir = Vector3.ProjectOnPlane(storedPlayerDir, playerVelocity);
                            Vector3 playerPerpendicularOfVelocityStored = Vector3.ProjectOnPlane(storedPerpendicularDir, Motor.CharacterUp);

                            //Default run deviation states.
                            switch (currentDeviationState)
                            {
                                case PetDeviationState.MoveAway:
                                {
                                    deviationDirToMove = -playerPerpendicularOfVelocityStored;
                                    break;
                                }
                                case PetDeviationState.MoveCloser:
                                {
                                    deviationDirToMove = playerPerpendicularOfVelocityStored;
                                    break;
                                }
                                case PetDeviationState.NoDeviation:
                                {
                                    deviationDirToMove = playerPerpendicularOfVelocityStored; //Temp
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

                            //Set the new move input with a deviation direction.
                            Vector3 randLerpDeviationDir = Vector3.Slerp(deviationDirToMove, playerVelocity, randDeviateAmount);
                            //_targetMoveInputVector = Vector3.Slerp(_targetMoveInputVector, randLerpDeviationDir, lastMoveInputDeviationValue).normalized;
                            _targetMoveInputVector = randLerpDeviationDir.normalized;

                            break;
                        }
                        case PetDefaultState.Walk:
                        {
                            MaxStableMoveSpeed = 3f;
                            _canAdjustSpeed = false; //Eventually change once moveSpeed is correctly adjusted
                            _targetMoveInputVector = playerVelocity.normalized;
                            break;
                        }
                        case PetDefaultState.Idle:
                        {
                            //_canAllowMovementDeviation = false;
                            _canAdjustSpeed = false;
                            _targetMoveInputVector = Vector3.zero;
                            break;
                        }
                    }
                    break;
                }
                case PetState.Crouched:
                {
                    switch (currentCrouchedState)
                    {
                        case PetCrouchedState.Walk:
                        {
                            _targetMoveInputVector = playerVelocity.normalized;
                            //Move straight, but with deviation checks.
                            break;
                        }
                        case PetCrouchedState.Idle:
                        {
                            _targetMoveInputVector = Vector3.zero;
                            break;
                        }
                    }
                     
                    break;
                }
                case PetState.FollowPath:
                {
                    Vector3 pathPosition = pathfinding.targetPathPosition;
                    _targetMoveInputVector = Vector3.ProjectOnPlane(pathPosition, Motor.CharacterUp);
                    _lookInputVector = _moveInputVector;
                    break;
                }
            }

            //Adjusting speed.
            if (_canAdjustSpeed)
            {
                //Change speed depending how close the pet is to the side point.
                if (playerDirTowardsVelocity.sqrMagnitude >= Mathf.Pow(2.5f, 2))
                {
                    if (targetMoveSpeed < 8.5f)
                    {
                        targetMoveSpeed += 8f * Time.deltaTime;
                    }
                }
                else if (playerDirTowardsVelocity.sqrMagnitude < Mathf.Pow(1.5f, 2))
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
                            //Debug.Log("Increasing Movespeed");
                           targetMoveSpeed += 8f * Time.deltaTime;
                        }
                    }
                }
            }

            //Slowing down movement while in front of the player.
            if (_shouldSlowDownInFrontOfPlayer)
            {
                if (angleComparison <= -.05f)
                {
                    //We could also do a lerp between moveSpeeds. This is mostly a test.
                    testLerpToZero += Time.deltaTime;
                    _targetMoveInputVector= Vector3.Slerp(_targetMoveInputVector, Vector3.zero, testLerpToZero);
                }
                else
                {
                    testLerpToZero = 0; //Reset this!
                } 
            }

            //(Temporary) Set any move input to be horizontal to the pet's up direction.
            //(Temporary) Set any look input to follow move input.
            _targetMoveInputVector = Vector3.ProjectOnPlane(_targetMoveInputVector, Motor.CharacterUp);
            _moveInputVector = Vector3.Slerp(_moveInputVector, _targetMoveInputVector.normalized, .05f);
            _lookInputVector = _moveInputVector;
        }

        /// <summary>
        /// (Called by KinematicCharacterMotor during its update cycle)
        /// This is called before the character begins its movement update
        /// </summary>
        public void BeforeCharacterUpdate(float deltaTime)
        {
            //Force deviation closer if too far away.
            if (currentDefaultState == PetDefaultState.Run || currentCrouchedState == PetCrouchedState.Walk)
            {
                if (!_forcingMovementTowardsPlayer && distanceToPlayer > Mathf.Pow(3f, 2) && angleComparison <= .7f &&
                    angleComparison >= -.7f)
                {
                    _forcingMovementTowardsPlayer = true;
                    currentDeviationState = PetDeviationState.MoveCloser;
                    StopActiveCoroutine(ChangeDeviationDirectionCoroutine);
                    ForceDeviationChangeCoroutine = StartCoroutine(ForceDeviationChange());
                }
            }

            //Move straight to the player if outside the default radius.
            if (CurrentPetState == PetState.Default || CurrentPetState == PetState.Crouched)
            {
                //Debug.Log(playerDir.magnitude);
                if (playerDir.sqrMagnitude > Mathf.Pow(3.5f, 2f))
                {
                    TransitionToState(PetState.MoveStraightToPlayer);
                }
            }
            
            //Enable pathfinding if no line of sight is met after x amount of time.
            if (CurrentPetState == PetState.Default || CurrentPetState == PetState.Crouched ||
                CurrentPetState == PetState.MoveStraightToPlayer)
            {
                if (!LineOfSightToTarget(playerPosition, .5f))
                {
                    TransitionToState(PetState.FollowPath);
                }
            }
            
            //Check each pet state.
            switch (CurrentPetState)
            {
                //DEFAULT STATE////////////////////////////////////////////////////////////////////////////////////////
                case PetState.Default:
                {
                    //Transition to crouch.
                    if (_shouldBeCrouching)
                    {
                        TransitionToState(PetState.Crouched);
                    }

                    //Check the default sub-states
                    switch (currentDefaultState)
                    {
                        //Default running state
                        case PetDefaultState.Run:
                        {
                            currentCrouchedState = PetCrouchedState.Walk;
                            
                            //Set a new deviation state.
                            if (_canChangeDeviation && !_forcingMovementTowardsPlayer) 
                            {
                                _canChangeDeviation = false;
                                ChangeDeviationDirectionCoroutine = StartCoroutine(RandomDeviationStateTime());
                                
                                //Store values for the move input.
                                storedPlayerDir = playerDir;
                                lastMoveInputDeviationValue = 0;
                                randDeviateAmount = UnityEngine.Random.Range(.75f, .95f);

                                //Deviate away if the pet is too close.
                                if (playerDirPerpendicularOfVelocity.sqrMagnitude < Mathf.Pow(1f, 2))
                                {
                                    currentDeviationState = PetDeviationState.MoveAway;
                                }
                                //Deviate closer if the pet is too far away.
                                else if (playerDirPerpendicularOfVelocity.sqrMagnitude > Mathf.Pow(2.5f, 2))
                                {
                                    currentDeviationState = PetDeviationState.MoveCloser;
                                }
                                //No deviation in the middle.
                                else
                                {
                                    currentDeviationState = PetDeviationState.NoDeviation; //Temp
                                }
                            }

                            //Transition to walk if the player moves too slow.
                            if (playerVelocity.sqrMagnitude < Mathf.Pow(4f, 2))
                            {
                                currentDefaultState = PetDefaultState.Walk;
                            }

                            break;
                        }
                        //Default walking state
                        case PetDefaultState.Walk:
                        {
                            currentCrouchedState = PetCrouchedState.Walk;

                            //Transition to idle if the player is too slow AND the pet is close enough.
                            if (playerDirTowardsVelocity.sqrMagnitude < Mathf.Pow(.5f, 2) && playerVelocity.sqrMagnitude < Mathf.Pow(1f, 2))
                            {
                                currentDefaultState = PetDefaultState.Idle;
                            }

                            //Transition to run if the player's velocity is above walking speed.
                            if (playerVelocity.sqrMagnitude > Mathf.Pow(4.2f, 2))
                            {
                                currentDefaultState = PetDefaultState.Run;
                            }
                            
                            //Move closer to the player if the pet is too far.
                            if (playerDirPerpendicularOfVelocity.sqrMagnitude > Mathf.Pow(2f, 2))
                            {
                                currentDeviationState = PetDeviationState.MoveCloser;
                            }
                            else
                            {
                                currentDeviationState = PetDeviationState.NoDeviation;
                            }
                            break;
                        }
                        //Default idle state
                        case PetDefaultState.Idle:
                        {
                            currentCrouchedState = PetCrouchedState.Idle;

                            //Transition to run if the player is too far or if the player's velocity is too high.
                            if (playerDir.sqrMagnitude > Mathf.Pow(2f, 2) ||
                                playerVelocity.sqrMagnitude > Mathf.Pow(4f, 2f))
                            {
                                currentDefaultState = PetDefaultState.Run;
                            }
                            break;
                        }
                    }

                    break;
                }
                //CROUCHED STATE///////////////////////////////////////////////////////////////////////////////////////
                case PetState.Crouched:
                {
                    //Transition to default.
                    if (!_shouldBeCrouching)
                    {
                        TransitionToState(PetState.Default);
                    }

                    switch (currentCrouchedState)
                    {
                        //Crouched walking state
                        case PetCrouchedState.Walk:
                        {
                            currentDefaultState = PetDefaultState.Walk;

                            //If the pet gets close enough to the player, transition to idle.
                            if (playerDirTowardsVelocity.sqrMagnitude < Mathf.Pow(1f, 2f))
                            {
                                currentCrouchedState = PetCrouchedState.Idle;
                            }

                            if (!_forcingMovementTowardsPlayer)
                            {
                                //If the player is too far, move closer to the player
                                if (playerDirPerpendicularOfVelocity.sqrMagnitude > Mathf.Pow(1.5f, 2))
                                {
                                    currentDeviationState = PetDeviationState.MoveCloser;
                                }
                                else
                                {
                                    currentDeviationState = PetDeviationState.NoDeviation;
                                }
                            }

                            break;
                        }
                        //Crouched idle state
                        case PetCrouchedState.Idle:
                        {
                            currentDefaultState = PetDefaultState.Idle;

                            //Transition to walk if the player is too far or if the player's velocity is too high. (Crouched)
                            if (playerDir.sqrMagnitude > Mathf.Pow(1.5f, 2) ||
                                playerVelocity.sqrMagnitude > Mathf.Pow(1f, 2f))
                            {
                                currentCrouchedState = PetCrouchedState.Walk;
                            }

                            break;
                        }
                    }

                    break;
                }
                //MOVE STRAIGHT TO PLAYER STATE////////////////////////////////////////////////////////////////////////
                case PetState.MoveStraightToPlayer:
                {
                    //If the pet enters the default radius, transition back to default.
                    if (distanceToPlayer < Mathf.Pow(2.8f, 2f))
                    {
                        TransitionToState(PetState.Default);
                    }

                    break;
                }
                case PetState.FollowPath:
                {
                    if (_canCheckPathfinding)
                    {
                        _canCheckPathfinding = false;
                        pathfinding.CheckIfCanFollowPath(playerPosition);
                    }

                    //Transition to default if line of sight is true OR if the pathfinding becomes inactive.
                    if (LineOfSightToTarget(playerPosition, 0f))
                    {
                        TransitionToState(PetState.Default);
                    }

                    //Warp to the player 
                    if (!pathfinding.PathIsActive && !pathfinding.CheckingPath)
                    {
                        _canCheckPathfinding = true;
                        if (canWarpToPlayer)
                        {
                            canWarpToPlayer = false;
                            TimeBeforeWarpingCoroutine = StartCoroutine(TimeBeforeWarping());
                        }
                    }
                    else
                    {
                        canWarpToPlayer = true;
                        StopActiveCoroutine(TimeBeforeWarpingCoroutine);
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
            playerDirTowardsVelocity = Vector3.ProjectOnPlane(Vector3.ProjectOnPlane(playerDir, playerCharRight), Motor.CharacterUp);

            //Set a direction that checks how close the pet is to the left and right of the player.
            Vector3 perpendicularToVelocity = Vector3.ProjectOnPlane(playerDir, playerVelocity);
            playerDirPerpendicularOfVelocity = Vector3.ProjectOnPlane(perpendicularToVelocity, Motor.CharacterUp);
            
            //Set a direction that checks how close the pet is behind or in front of the player.
           //playerSideDir = Vector3.ProjectOnPlane(Vector3.ProjectOnPlane(playerDir, playerCharRight), Motor.CharacterUp);
           projectedPlayerVelocity =
               Vector3.ProjectOnPlane(playerVelocity, Motor.CharacterUp).normalized;
           angleComparison = Vector3.Dot(projectedPlayerVelocity, playerDir.normalized); //Replace with Vector3.Angle.
           
           _shouldBeCrouching = player._isCrouching;
        }

        //Check line of sight to target.
        public bool LineOfSightToTarget(Vector3 targetPosition, float minTime)
        {
            float maxDistance = 12f;
            
            Debug.DrawLine(transformPosition, targetPosition, Color.yellow);
            if (Physics.Linecast(transformPosition, targetPosition, LineOfSightMask))
            {
                lineOfSightTimePassed += Time.deltaTime;
                if (lineOfSightTimePassed >= minTime)
                {
                    return false;
                }
            }
            else
            {
                lineOfSightTimePassed = 0;
            }

            return true;
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

        //Set a delay before warping a second time..
        private IEnumerator DelayBetweenWarping()
        {
            yield return CustomTimer.Timer(2f);
            canWarpToPlayer = true;
        }

        //Set a delay before warping to check line of sight.
        private IEnumerator TimeBeforeWarping()
        {
            yield return CustomTimer.Timer(.75f);
            TransitionToState(PetState.WarpToPlayer);
            TimeBetweenWarpsCoroutine = StartCoroutine(DelayBetweenWarping());
        }
        
        private IEnumerator RandomDeviationStateTime()
        {
            yield return CustomTimer.Timer(UnityEngine.Random.Range(.85f, 1.5f));
            _canChangeDeviation = true;
        }
        
        private IEnumerator ForceDeviationChange()
        {
            yield return CustomTimer.Timer(.5f);
            _forcingMovementTowardsPlayer = false;
            _canChangeDeviation = true;
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

//TO DO:
//1. Modify the canChangeSpeed bool. (Honestly, is this going to be implemented in any state besides default?)
//2. Try messing with move inputs to see if we can get a general slerp movement so that transitioning between states
    //is smoother (Ex: idle and run.)
//3. Fix the pet moving too far after the player stops.
//4. Start implementing moveStraightToPlayer that waits for a minimum amount of time before transitioning back.
//5. Start implementing a system that checks for certain player states (while in default) that will wait for a brief
    //specified delay before transitioning to the state. (Ex: Picking plants, petting, sword attack, etc.) Also include
    //a max amount of time before the pet can no longer transition to it. (Ex: If the pet checks the player's sword
    //attack RIGHT at the end, we don't want the pet to transition to it since x amount of time has passed.) States
    //like a cutscene state will be handled by the cutscene manager itself.