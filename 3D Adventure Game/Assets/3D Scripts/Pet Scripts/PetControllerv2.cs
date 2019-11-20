using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KinematicCharacterController;
using System;
using System.Diagnostics;
using KinematicCharacterController.Nate;
using UnityEditor;
using Debug = UnityEngine.Debug;

//Movement controller for the fox.
namespace KinematicCharacterController.PetControllerv2
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

    public enum PetDefaultState //Default.
    {
        Idle,
        Walk,
        Run,
        StayBehindPlayer,
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

        public PetState CurrentPetState;
        public NateCharacterController player;
        public CharacterState NewCharacterState;

        private Collider[] _probedColliders = new Collider[8];

        private Vector3 _moveInputVector;
        private Vector3 _lookInputVector;
        private Vector3 _internalVelocityAdd = Vector3.zero;
        private Vector3 transformPosition;
        private Vector3 targetDirection;

        private Vector3 deviationDir;

        private bool _isCrouching = false;
        private bool _playerIsCrouching;
        private bool _shouldBeCrouching = false;
        private bool _canCheckPathfinding = true;
        private bool _moveSpeedBasedOnTargetDistance;

        //Obstacle checks
        private bool _wallDetected;
        private bool _ledgeDetected;
        private Vector3 _detectedWallNormal;
        private Vector3 _detectedLedgeDirection;

        private float moveInputDeviationValue;
        private float lastMoveInputDeviationValue;
        private float walkRadius;
        private float targetMoveSpeed;
        
        //Keep the state's movespeed stored and only change it during a state transition.
        //private float stateMaxStableMoveSpeed;
        
        //Player information
        private float playerMaxStableMoveSpeed;
        private Vector3 playerPosition;
        private Vector3 playerDir;
        private Vector3 storedDeviationDir;
        private Vector3 leftDir;
        private Vector3 rightDir;
        private Vector3 playerDirTowardsVelocity;
        private Vector3 playerDirPerpendicularOfVelocity;
        private Vector3 playerVelocity;
        private Vector3 projectedPlayerVelocity;
        private float angleComparison;
        private float distanceToPlayer;
        public float moveInputLerpTest;

        private Vector3 lastInnerNormal = Vector3.zero;
        private Vector3 lastOuterNormal = Vector3.zero;

        public UnitFollowNew pathfinding;
        public LayerMask WallLayerMask;

        //Enums
        public PetDefaultState currentDefaultState;
        public PetDeviationState currentDeviationState;
        public PetCrouchedState currentCrouchedState;

        //Coroutines
        private Coroutine ForceDeviationStateCoroutine;
        private bool _forcingMovementTowardsPlayer;
        private Coroutine ChangeDeviationDirectionCoroutine;
        private bool _canChangeDeviation;

        private void Start()
        {
            //Handle initial state
            TransitionToState(PetState.Default);
            currentDefaultState = PetDefaultState.Idle;
            currentDeviationState = PetDeviationState.NoDeviation;
            _canChangeDeviation = true;

            // Assign the characterController to the motor
            Motor.CharacterController = this;
        }

        private void Update()
        {
            SetInputs(); //Temp
            transformPosition = Motor.transform.position;
            playerMaxStableMoveSpeed = player.MaxStableMoveSpeed;
            _playerIsCrouching = player._isCrouching;
            Debug.DrawRay(Motor.transform.position, storedDeviationDir, Color.red);
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
                    break;
                }
                case PetState.Crouched:
                {
                    Motor.SetCapsuleDimensions(0.5f, 1f, 0.25f); //Scales the hitbox.
                    MeshRoot.localScale = new Vector3(1f, 0.35f, 1.2f); //Scales the mesh root.
                    //MaxStableMoveSpeed = 3f;
                    break;
                }
                case PetState.MoveStraightToPlayer:
                {
                    //MaxStableMoveSpeed = playerMaxStableMoveSpeed + 1f;
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
                            break;
                        }
                        case PetDefaultState.Walk:
                        {
                            break;
                        }
                    }
                    break;
                }
                case PetState.Crouched:
                {
                    Motor.SetCapsuleDimensions(0.5f, 1f, .25f); //Scales the hitbox.
                    MeshRoot.localScale = new Vector3(1f, .5f, 1.2f); //Scales the mesh root.
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
            //Change the targetMoveSpeed.
            if (CurrentPetState == PetState.Default && currentDefaultState == PetDefaultState.Run)
            {
                targetMoveSpeed = AdjustSpeedBasedOnTargetDistance(targetMoveSpeed, 2.5f, 1.5f, 3f);
            }
            else if (CurrentPetState == PetState.Crouched && currentCrouchedState == PetCrouchedState.Walk)
            {
                targetMoveSpeed = AdjustSpeedBasedOnTargetDistance(targetMoveSpeed, 2f, 1f, 2.5f);
            }
            else
            {
                if (targetMoveSpeed <= MaxStableMoveSpeed)
                {
                    targetMoveSpeed += MaxStableMoveSpeed * Time.deltaTime * 5f;
                }
                //Debug.Log(targetMoveSpeed);
            }

            //Set inputs for each pet state.
            switch (CurrentPetState)
            {
                //Default state inputs.
                case PetState.Default:
                {
                    switch (currentDefaultState)
                    {
                        case PetDefaultState.Run:
                        {
                            MaxStableMoveSpeed = playerMaxStableMoveSpeed;
                            switch (currentDeviationState)
                            {
                                case PetDeviationState.NoDeviation:
                                {
                                    _moveInputVector = Vector3.Slerp(_moveInputVector, projectedPlayerVelocity, moveInputLerpTest).normalized;
                                    break;
                                }
                                case PetDeviationState.MoveAway:
                                case PetDeviationState.MoveCloser:
                                {
                                    _moveInputVector = Vector3.Slerp(_moveInputVector, storedDeviationDir, moveInputLerpTest).normalized; //Obvious test
                                    break;
                                }
                            }
                            break;
                        }
                        case PetDefaultState.Walk:
                        {
                            MaxStableMoveSpeed = 3f;
                            switch (currentDeviationState)
                            {
                                case PetDeviationState.NoDeviation:
                                {
                                    _moveInputVector = Vector3.Slerp(_moveInputVector, projectedPlayerVelocity, moveInputLerpTest).normalized;
                                    break;
                                }
                                case PetDeviationState.MoveCloser:
                                {
                                    Vector3 dirTowardsPlayer = Vector3.Slerp(playerVelocity, playerDirPerpendicularOfVelocity, 1f);
                                    _moveInputVector = Vector3.Slerp(_moveInputVector, dirTowardsPlayer, moveInputLerpTest).normalized;
                                    Debug.DrawRay(Motor.transform.position, dirTowardsPlayer, Color.yellow);
                                    break;
                                }
                            }
                            break;
                        }
                        case PetDefaultState.Idle:
                        {
                            MaxStableMoveSpeed = 0f;
                            _moveInputVector = Vector3.zero;
                            break;
                        }
                    }
                    break;
                }
                //Crouched state inputs.
                case PetState.Crouched:
                {
                    switch (currentCrouchedState)
                    {
                        case PetCrouchedState.Walk:
                        {
                            MaxStableMoveSpeed = 3f;
                            switch (currentDeviationState)
                            {
                                case PetDeviationState.NoDeviation:
                                {
                                    _moveInputVector = Vector3.Slerp(_moveInputVector, projectedPlayerVelocity, moveInputLerpTest).normalized;
                                    break;
                                }
                                case PetDeviationState.MoveCloser:
                                {
                                    Vector3 dirTowardsPlayer = Vector3.Slerp(playerVelocity, -playerDirPerpendicularOfVelocity, .3f);
                                    _moveInputVector = Vector3.Slerp(_moveInputVector, dirTowardsPlayer, moveInputLerpTest).normalized;
                                    break;
                                }
                            }
                            break;
                        }
                        case PetCrouchedState.Idle:
                        {
                            MaxStableMoveSpeed = 0f;
                            _moveInputVector = Vector3.zero;
                            break;
                        }
                    }
                    break;
                }
                //MoveStraightToPlayer inputs.
                case PetState.MoveStraightToPlayer:
                {
                    MaxStableMoveSpeed = 8f;
                    if (rightDir.sqrMagnitude < leftDir.sqrMagnitude)
                    {
                        targetDirection = rightDir;
                    }
                    else
                    {
                        targetDirection = leftDir;
                    }
                    
                    _moveInputVector = Vector3.Slerp(_moveInputVector, Vector3.ProjectOnPlane(
                        targetDirection.normalized, Motor.CharacterUp).normalized, moveInputLerpTest).normalized;
                    _lookInputVector = _moveInputVector;
                    break;
                }
            }
            //MoveInput = Slerp(moveinput, targetInput, moveInputSlerpValue)
            _lookInputVector = _moveInputVector;
        }

        /// <summary>
        /// (Called by KinematicCharacterMotor during its update cycle)
        /// This is called before the character begins its movement update
        /// </summary>
        public void BeforeCharacterUpdate(float deltaTime)
        {
            GetPlayerValues();
            //Crouch if the player is crouching.
            _shouldBeCrouching = player._isCrouching;
            
            //If the current state is default or crouched...
            if (CurrentPetState == PetState.Default || CurrentPetState == PetState.Crouched)
            {
                //Force the pet to move closer to the player if it goes outside the allowed radius or detects and obstacle.
                if (!_forcingMovementTowardsPlayer)
                {
                    if (distanceToPlayer > Mathf.Pow(3f, 2) && angleComparison <= .7f && 
                        angleComparison >= -.7f || _wallDetected || _ledgeDetected)
                    {
                        _forcingMovementTowardsPlayer = true;
                        StopActiveCoroutine(ChangeDeviationDirectionCoroutine);
                        ForceDeviationStateCoroutine = StartCoroutine(ForceDeviationStateTime());
                        
                        //Store a deviation direction.
                        storedDeviationDir = Vector3.Slerp(playerVelocity, -playerDirPerpendicularOfVelocity, .3f);
                        currentDeviationState = PetDeviationState.MoveCloser;
                    }
                }

                //If the pet goes outside the default radius, transition to MoveStraightToPlayer.
                if (distanceToPlayer > Mathf.Pow(3.5f, 2f))
                {
                    TransitionToState(PetState.MoveStraightToPlayer);
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

                            //Start deviation if the coroutine ends.
                            if (_canChangeDeviation && !_forcingMovementTowardsPlayer)
                            {
                                _canChangeDeviation = false;
                                ChangeDeviationDirectionCoroutine = StartCoroutine(RandomDeviationStateTime());
                                
                                //(Store a deviation direction so we can use it for the move input.)
                                Vector3 dirToMove = new Vector3();

                                //Check the distance towards the player to determine a deviation direction.
                                if (playerDirPerpendicularOfVelocity.sqrMagnitude < Mathf.Pow(1f, 2))
                                {
                                    currentDeviationState = PetDeviationState.MoveAway;
                                    dirToMove = -playerDirPerpendicularOfVelocity;
                                }
                                else if (playerDirPerpendicularOfVelocity.sqrMagnitude > Mathf.Pow(2.5f, 2))
                                {
                                    currentDeviationState = PetDeviationState.MoveCloser;
                                    dirToMove = playerDirPerpendicularOfVelocity;
                                }
                                else
                                {
                                    currentDeviationState = PetDeviationState.MoveCloser;
                                    //(Can also randomly deviate here.)
                                }
                                storedDeviationDir = Vector3.Slerp( dirToMove, playerVelocity,
                                    UnityEngine.Random.Range(.75f, .95f));
                            }
                            
                            //If the player starts moving slow, transition to walk.
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

                            //If the pet gets close enough to the player, transition to idle.
                            if (playerDirTowardsVelocity.sqrMagnitude < Mathf.Pow(.5f, 2))
                            {
                                currentDefaultState = PetDefaultState.Idle;
                            }
                            Debug.Log(playerDirTowardsVelocity.magnitude);

                            //If the player's velocity goes above walking speed, transition to run.
                            if (playerVelocity.sqrMagnitude > Mathf.Pow(4.2f, 2))
                            {
                                currentDefaultState = PetDefaultState.Run;
                            }

                            if (!_forcingMovementTowardsPlayer)
                            {
                                //If the side distance to the player is too far, move closer to the player
                                if (playerDirPerpendicularOfVelocity.sqrMagnitude > Mathf.Pow(2f, 2))
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
                        //Default idle state
                        case PetDefaultState.Idle:
                        {
                            currentCrouchedState = PetCrouchedState.Idle;

                            //If the player gets too far or if the player's velocity is too high... (Default)
                            if (playerDir.sqrMagnitude > Mathf.Pow(2f, 2) ||
                                playerVelocity.sqrMagnitude > Mathf.Pow(4f, 2f))
                            {
                                currentDefaultState = PetDefaultState.Run;
                            }
                            
                            //Debug.Log(playerDir.magnitude + ", " + playerVelocity.magnitude);
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
                            
                            //If the player gets too far or if the player's velocity is too high... (Crouched)
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

                case PetState.MoveStraightToPlayer:
                {
                    //If the pet enters the default radius, transition back to default.
                    if (distanceToPlayer < Mathf.Pow(3f, 2f))
                    {
                        TransitionToState(PetState.Default);
                    }
                    break;
                }
            }
        }
        
        //Temporary way of getting info about the player.
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
            
            //Get the dot product between the direction to the player and the player's velocity.
            projectedPlayerVelocity =
                Vector3.ProjectOnPlane(playerVelocity, Motor.CharacterUp).normalized;
            angleComparison = Vector3.Dot(projectedPlayerVelocity, Vector3.ProjectOnPlane(playerDir.normalized, Motor.CharacterUp)); //Replace with Vector3.Angle.
        }

        private float AdjustSpeedBasedOnTargetDistance(float speed, float minDistance, float maxDistance, float maxRadiusSize)
        {
            //Change speed depending how close the pet is to the player.
            if (playerDirTowardsVelocity.sqrMagnitude >= Mathf.Pow(maxDistance, 2))
            {
                if (speed < MaxStableMoveSpeed + 1.5f)
                {
                    speed += (MaxStableMoveSpeed + 1f) * Time.deltaTime;
                }
            }
            else if (playerDirTowardsVelocity.sqrMagnitude < Mathf.Pow(minDistance, 2))
            {
                if (speed > MaxStableMoveSpeed)
                {
                    speed -= (MaxStableMoveSpeed + 1f) * Time.deltaTime;
                }
            }
                            
            //Angle check for increasing moveSpeed. (Make sure to have it last shortly after coming in.)
            //Check the moveSpeed angle.
            if (distanceToPlayer > Mathf.Pow(maxRadiusSize, 2))
            {
                if (angleComparison <= 1f && angleComparison > .5f)
                {
                    if (speed < MaxStableMoveSpeed + 1.5f)
                    {
                        Debug.Log("Increasing Movespeed");
                        speed += (MaxStableMoveSpeed + 1f) * Time.deltaTime;
                    }
                }
            }
            return speed;
            //In the case that the pet is inbetween both of the above checks, keep the moveSpeed
            //at whatever targetMoveSpeed it was set to.
            //MaxStableMoveSpeed = targetMoveSpeed;
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
            }

            if (OrientTowardsGravity)
            {
                // Rotate from current up to invert gravity
                currentRotation = Quaternion.FromToRotation((currentRotation * Vector3.up), -Gravity) *
                                  currentRotation;
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

        private IEnumerator RandomDeviationStateTime()
        {
            yield return CustomTimer.Timer(UnityEngine.Random.Range(.85f, 1.5f));
            _canChangeDeviation = true;
        }

        private IEnumerator ForceDeviationStateTime()
        {
            yield return CustomTimer.Timer(.5f);
            _forcingMovementTowardsPlayer = false;
            _canChangeDeviation = true;
        }
        private IEnumerator TimeBeforeFallingStateCoroutine()
        {
            yield return CustomTimer.Timer(.1f);
            TransitionToState(PetState.Default);
        }
    }
}

//TO DO:
//Add a coroutine that allows each state to run for a minimum amount of time to run before switching. Can have a bool
    //like canSetState set to false, then have a parameter in the coroutine like (float minTime).

//METHOD 1:
//Use the moveCloser and moveAway states for random deviation, and transition to the default deviation state if a ledge,
    //slope, or wall obstacle is detected. The default state will handle any new move inputs for obstacles until none
    //are detected. (Temporary solution for now)
//Use the psd file reference for ledge detection moveInput.
//Transition to moveCloser upon wall or slope detection.