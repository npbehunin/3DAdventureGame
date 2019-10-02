using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KinematicCharacterController;
using System;
using System.Runtime.InteropServices;
using UnityEditor.Experimental.GraphView;
using Random = System.Random;

namespace KinematicCharacterController.Raccoonv2
{
    public enum RaccoonState
    {
        Default,
        Search,
        //FollowTarget,
        Attack,
        Alerted,
        AlertOthers,
        Stunned,
        Knocked,
        Dead,
    }

    public enum RaccoonDefaultState
    {
        Sitting,
        Talking,
        Patrolling,
        Sleeping,
    }

    public enum RaccoonAttackState
    {
        FollowTarget,
        SwordAttack,
        BowAttack,
        RockAttack,
        JumpAttack
    }

    public enum RaccoonFollowTargetState
    {
        MoveToTarget,
        Reposition,
        Wait,
    }
    
    public enum RaccoonSwordAttackState
    {
        NormalSwing,
        DoubleSwing,
        Flail,
    }

    public struct RaccoonCharacterInputs
    {
        public Vector3 MoveVector;
        public Vector3 LookVector;
    }
    
    public class RaccoonControllerv2 : MonoBehaviour, ICharacterController
    {
        public KinematicCharacterMotor Motor;
    
        [Header("Stable Movement")] public float MaxStableMoveSpeed = 5f;
        public float StableMovementSharpness = 15f;
        public float OrientationSharpness = 10f;

        [Header("Air Movement")] public float MaxAirMoveSpeed = 100f;
        public float AirAccelerationSpeed = 15f;
        public float Drag = 0.1f;

        [Header("Misc")] public List<Collider> IgnoredColliders = new List<Collider>();
        public bool OrientTowardsGravity = false;
        public Vector3 Gravity = new Vector3(0, -30f, 0);
        public Transform MeshRoot;
        //public Transform HomePosition;
        
        private Collider[] _probedColliders = new Collider[8];
        public Vector3 _moveInputVector;
    
        private Vector3 _lookInputVector;
        private float testLerp = 0f;
        private Vector3 _internalVelocityAdd = Vector3.zero;

        private Vector3 lastInnerNormal = Vector3.zero;
        private Vector3 lastOuterNormal = Vector3.zero;
        
        //States
        public RaccoonState CurrentRaccoonState;// { get; private set; }
        //private SwordAttackMovementState CurrentSwordAttackMovementState;
        //private RaccoonDefaultState CurrentDefaultState;
        public RaccoonAttackState CurrentAttackState;
        public RaccoonDefaultState SelectedDefaultState;
        //private DetectionState CurrentDetectionState;
        public RaccoonFollowTargetState CurrentFollowState;

        private float RepositionDistance = 4f;
        private float AttackDistance = 2f;

        public LayerMask WallLayerMask;
        public Transform Target;
        public Transform Home;
        private Vector3 targetPosition;
        private Vector3 homePosition;
        private Vector3 pathPosition;
        public bool _lineOfSightToTarget;
        public bool _lineOfSightToHome;
        private float fieldOfView;
        private float timeWhileTargetSeen = 0;
        private float maxAlertedTime = .75f;
        private int timesAlerted = 0;
        private int randomAttackInt = 0;
        private int timesRepositioned;
        private int timesWaitedInPlace;
        private bool _canChooseRandomAttack;
        private bool _canAttack;
        private bool _canCheckPathfinding;
        public bool _canReposition;
        private bool _shouldMoveToHome;

        public UnitFollowNew pathfinding;
        
        //Coroutines
        private Coroutine AlertToSearchCoroutine;
        private bool canStartAlertToSearch = true;
        private Coroutine SearchStateCoroutine;
        private Coroutine AttackDelayTimeCoroutine;
        private bool canStartAttackDelayTime;
        private Coroutine AttackTimeCoroutine;
        private Coroutine RepositionTimeCoroutine;

        //Coroutine
        //bool

        private void Start()
        {
            //Handle initial state
            TransitionToState(RaccoonState.Default);
    
            // Assign the characterController to the motor
            Motor.CharacterController = this;
        }

        /// <summary>
        /// Handles movement state transitions and enter/exit callbacks
        /// </summary>
        public void TransitionToState(RaccoonState newState)
        {
            RaccoonState tmpInitialState = CurrentRaccoonState; //Get current state.
            OnStateExit(tmpInitialState, newState); //Do the OnStateExit stuff from current state to new state.
            CurrentRaccoonState = newState; //Current state = new state.
            Debug.Log("State set to " + newState + ".");
            OnStateEnter(newState, tmpInitialState); //Do the OnStateEnter stuff to new state from the last state.
        }
    
        /// <summary>
        /// Event when entering a state
        /// </summary>
        public void OnStateEnter(RaccoonState state, RaccoonState fromState)
        {
            //SET MAXSPEED etc. HERE
            switch (state)
            {
                case RaccoonState.Default:
                {
                    fieldOfView = 60;
                    timesAlerted = 0;
                    _canCheckPathfinding = true;
                    _shouldMoveToHome = true;
                    MaxStableMoveSpeed = 9f;
                    //MaxMoveSpeed = x.
                    //Set target to home position.
                    //Reset numberOfTimesAlerted.
                    break;
                }
                case RaccoonState.Alerted:
                {
                    fieldOfView = 360;
                    timesAlerted += 1;
                    //MaxMoveSpeed = x.
                    //Set target to the detected object (thrown object or player.)
                    //Play a coroutine for x amount of time.
                    //If the player caused the alert, add 1 to numberOfTimesAlerted . (This is reset in the default state.)
                    break;
                }
                case RaccoonState.AlertOthers:
                {
                    //(Field of view doesn't matter here)
                    //Play coroutine for x amount of time. Transition to followTarget.
                    break;
                }
                case RaccoonState.Attack:
                {
                    //_canAttack = true;
                    _canChooseRandomAttack = true;
                    _canCheckPathfinding = true;
                    timesAlerted = 0;
                    _canReposition = true;
                    CurrentAttackState = RaccoonAttackState.FollowTarget;
                    break;
                }
                case RaccoonState.Search:
                {
                    SearchStateCoroutine = StartCoroutine(SearchStateTime());
                    fieldOfView = 60;
                    break;
                }
            }
        }
    
        /// <summary>
        /// Event when exiting a state
        /// </summary>
        public void OnStateExit(RaccoonState state, RaccoonState toState)
        {
            //STOP ANY COROUTINES HERE
            
            switch (state)
            {
                case RaccoonState.Default:
                {
                    _canCheckPathfinding = true;
                    _shouldMoveToHome = false;
                    break;
                }
                case RaccoonState.Attack:
                {
                    pathfinding.StopFollowPath();
                    _canCheckPathfinding = true;
                    canStartAttackDelayTime = true;
                    _canAttack = false;
                    timesRepositioned = 0;
                    timesWaitedInPlace = 0;
                    StopActiveCoroutine(AttackTimeCoroutine);
                    StopActiveCoroutine(RepositionTimeCoroutine);
                    break;
                }
                case RaccoonState.Alerted:
                {
                    timeWhileTargetSeen = 0;
                    StopActiveCoroutine(AlertToSearchCoroutine);
                    canStartAlertToSearch = true;
                    break;
                }
                case RaccoonState.Search:
                {
                    StopActiveCoroutine(SearchStateCoroutine);
                    break;
                }
            }
        }

        /// <summary>
        /// This is called every frame by the AI script in order to tell the character what its inputs are
        /// </summary>
        public void SetInputs(ref RaccoonCharacterInputs inputs) //AI
        {
            //USE THIS AREA TO SET MOVE INPUT AND LOOK DIRECTION.
            //AVOID SETTING STATE TRANSITIONS HERE.
            Vector3 playerLookDirection = Vector3.ProjectOnPlane(targetPosition - Motor.Transform.position, Motor.CharacterUp);
            switch (CurrentRaccoonState)
            {
                //Default movement
                case RaccoonState.Default:
                {
                    if (_shouldMoveToHome)
                    {
                        if (pathfinding.PathIsActive)
                        {
                            Vector3 pathDirection = pathPosition - Motor.transform.position;
                            _moveInputVector = Vector3.ProjectOnPlane(pathDirection, Motor.CharacterUp).normalized;
                        }
                        else
                        {
                            Vector3 homeDirection = homePosition - Motor.transform.position;
                            _moveInputVector = Vector3.ProjectOnPlane(homeDirection, Motor.CharacterUp).normalized;
                        }
                    }
                    else
                    {
                        _moveInputVector = Vector3.zero;
                        //Do nothin'.
                    }
                    _lookInputVector = _moveInputVector;
                    break;
                }
                //Follow target movement
                case RaccoonState.Attack:
                {
                    switch (CurrentAttackState)
                    {
                        case RaccoonAttackState.FollowTarget:
                        {
                            switch (CurrentFollowState)
                            {
                                case RaccoonFollowTargetState.MoveToTarget:
                                {
                                    //Follow target movement.
                                    if (!_lineOfSightToTarget && pathfinding.PathIsActive) //If both false, goes to search state. (Taken care of below)
                                    {
                                        Vector3 pathDirection = pathPosition - Motor.transform.position;
                                        _moveInputVector = Vector3.ProjectOnPlane(pathDirection, Motor.CharacterUp).normalized;
                                        //Move towards path.
                                    }
                                    else
                                    {
                                        Vector3 playerDirection = targetPosition - Motor.Transform.position;
                                        _moveInputVector = Vector3.ProjectOnPlane(playerDirection, Motor.CharacterUp).normalized;
                                        //Move straight towards the player.
                                    }
                                    
                                    //Follow target rotation.
                                    if (_lineOfSightToTarget)
                                    {
                                        _lookInputVector = playerLookDirection;
                                    }
                                    else
                                    {
                                        _lookInputVector = _moveInputVector;
                                    }
                                    break;
                                }
                                case RaccoonFollowTargetState.Reposition:
                                {
                                    //Move to a spot near the player.
                                    _lookInputVector = _moveInputVector;
                                    MaxStableMoveSpeed = 1f;
                                    break;
                                }
                                case RaccoonFollowTargetState.Wait:
                                {
                                    MaxStableMoveSpeed = 0f;
                                    _moveInputVector = Vector3.zero;
                                    _lookInputVector = playerLookDirection;
                                    //Do nuthin.
                                    break;
                                }
                            }
                            break;
                        }
                        case RaccoonAttackState.SwordAttack:
                        {
                            MaxStableMoveSpeed = 2f; //For testing
                            //Sword attack.
                            break;
                        }
                        case RaccoonAttackState.JumpAttack:
                        {
                            MaxStableMoveSpeed = 4f; //For testing
                            //Jump attack.
                            break;
                        }
                        case RaccoonAttackState.RockAttack:
                        {
                            MaxStableMoveSpeed = 0f; //For testing
                            //Rock attack.
                            break;
                        }
                    }
                    break;
                }
                
                //(Default movement, zero)
                case RaccoonState.Search:
                case RaccoonState.AlertOthers:
                {
                    _moveInputVector = Vector3.zero;
                    _lookInputVector = _moveInputVector;
                    break;
                }

                case RaccoonState.Alerted:
                {
                    _lookInputVector = playerLookDirection;
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
            targetPosition = Target.transform.position; //Target's position.
            pathPosition = pathfinding.targetPathPosition;
            
            switch (CurrentRaccoonState)
            {
                case RaccoonState.Alerted:
                {
                    if (_lineOfSightToTarget)
                    {
                        StopActiveCoroutine(AlertToSearchCoroutine);
                        canStartAlertToSearch = true;
                        timeWhileTargetSeen += deltaTime;
                    }
                    //(Otherwise, it stops counting.)

                    break;
                }
                case RaccoonState.Default:
                {
                    homePosition = Home.transform.position; //Home position.
                    break;
                }
                case RaccoonState.Attack:
                {
                    switch (CurrentFollowState)
                    {
                        case RaccoonFollowTargetState.MoveToTarget:
                        {
                            MaxStableMoveSpeed = 6f;
                            break;
                        }
                        case RaccoonFollowTargetState.Reposition:
                        {
                            MaxStableMoveSpeed = 3f;
                            break;
                        }
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
                Vector3 reorientedInput =
                    Vector3.Cross(effectiveGroundNormal, inputRight).normalized *
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
                            .Cross(Vector3.Cross(Motor.CharacterUp, Motor.GroundingStatus.GroundNormal), Motor.CharacterUp)
                            .normalized;
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
            //SET STATE TRANSITIONS HERE.
            
            //Switch to default state if in midair
            if (!Motor.GroundingStatus.IsStableOnGround)
            {
                if (CurrentRaccoonState != RaccoonState.Default)
                {
                    //Get the current state and store it.
                    //Transition to falling state (and transition back to either a new state or the stored state).
                    //Enemy should be able to transition back to the last state without anything screwing up.
                }
            }

            if (pathfinding.PathIsActive)
            {
                pathfinding.motorUpDirection = Motor.CharacterUp;
            }

            //Check line of sight to the target.
            _lineOfSightToTarget = LineOfSightToTarget(targetPosition, fieldOfView);

            //float maxDistance = 12f;
            Vector3 targetDirection = targetPosition - Motor.Transform.position;
            //Debug.Log(targetDirection);

            //State checking
            switch (CurrentRaccoonState)
            {
                case RaccoonState.Default:
                {
                    if (_lineOfSightToTarget)
                    {
                        TransitionToState(RaccoonState.Alerted);
                    }

                    _lineOfSightToHome = LineOfSightToTarget(homePosition, 360f);
                    
                    //If the home position is too far away...
                    Vector3 homeDirection = homePosition - Motor.Transform.position;
                    float homeMaxDistance = 1f;

                    //Debug.Log(homeDirection);
                    if (!_lineOfSightToHome)
                    {
                        if (_canCheckPathfinding) //(Reset this)
                        {
                            _canCheckPathfinding = false;
                            pathfinding.CheckIfCanFollowPath(targetPosition);
                        }

                        if (pathfinding.PathIsActive)
                        {
                            _shouldMoveToHome = true;
                        }
                        else
                        {
                            _shouldMoveToHome = false;
                        }
                    }
                    else
                    {
                        if (homeDirection.sqrMagnitude > Mathf.Pow(homeMaxDistance, 2))
                        {
                            _shouldMoveToHome = true;
                        }
                        else
                        {
                            _shouldMoveToHome = false;
                        }
                    }
                    

                    //If player is detected, transition to alerted state.
                    break;
                }
                case RaccoonState.Alerted:
                {
                    if (timeWhileTargetSeen >= maxAlertedTime || timesAlerted > 1)
                    {
                        //For now, just transition to follow player until group detection is set up.
                        TransitionToState(RaccoonState.Attack);
                    }

                    if (!_lineOfSightToTarget && canStartAlertToSearch)
                    {
                        canStartAlertToSearch = false;
                        AlertToSearchCoroutine = StartCoroutine(AlertToSearchTransitionTime());
                    }
                    
                    break;
                }
                case RaccoonState.AlertOthers:
                {
                    //Nuthin yet
                    break;
                }
                case RaccoonState.Search:
                {
                    if (_lineOfSightToTarget)
                    {
                        TransitionToState(RaccoonState.Alerted);
                    }
                    break;
                }

                case RaccoonState.Attack:
                {
                    switch (CurrentAttackState)
                    {
                        case RaccoonAttackState.FollowTarget:
                        {
                            float targetDirMagnitude = targetDirection.sqrMagnitude;

                            //If the target is ever outside the reposition distance...
                            if (targetDirMagnitude > Mathf.Pow(RepositionDistance, 2))
                            {
                                //Debug.Log(targetDirMagnitude / targetDirection.magnitude);
                                CurrentFollowState = RaccoonFollowTargetState.MoveToTarget; //**
                                //If 1 hand is free...
                                    //Random chance to throw rocks.
                            }
                            else
                            {
                                if (_canReposition) //Remember to reset this, timesRepositioned, AND timesWaited.
                                {
                                    _canReposition = false;
                                    int rand = UnityEngine.Random.Range(0, 100);

                                    //If repositioned/waited up to 3 times...
                                    if (timesRepositioned + timesWaitedInPlace <= 3)
                                    {
                                        //Reposition (up to 2)
                                        if ((rand > 50 && rand <= 75) || timesWaitedInPlace > 0)
                                        {
                                            Debug.Log("Repositioning.");
                                            timesRepositioned += 1;
                                            CurrentFollowState = RaccoonFollowTargetState.Reposition;
                                            RepositionTimeCoroutine = StartCoroutine(RepositionTime(.65f, .95f));
                                        }
                                        //Wait (up to 1)
                                        else if ((rand > 75 && rand <= 100) && timesWaitedInPlace == 0)
                                        {
                                            Debug.Log("Waiting.");
                                            timesWaitedInPlace += 1;
                                            CurrentFollowState = RaccoonFollowTargetState.Wait;
                                            RepositionTimeCoroutine = StartCoroutine(RepositionTime(.85f, 1.15f));
                                        }
                                    }

                                    //If over the reposition amount OR rand <= 50...
                                    if (timesRepositioned + timesWaitedInPlace > 3 || rand <= 50)
                                    {
                                        //(Moves towards the target here in moveInputVector)
                                        Debug.Log("Continuing movement.");
                                        CurrentFollowState = RaccoonFollowTargetState.MoveToTarget; //**
                                    }
                                }
                            }

                            //Transition to sword attack
                            switch (CurrentFollowState)
                            {
                                case RaccoonFollowTargetState.MoveToTarget:
                                {
                                    if (targetDirMagnitude < Mathf.Pow(AttackDistance, 2)) //**
                                    {
                                        //Sword Attack.
                                        Debug.Log("Sword attack");
                                        CurrentAttackState = RaccoonAttackState.SwordAttack;
                                        AttackTimeCoroutine = StartCoroutine(AttackTime(1.2f));
                                        timesRepositioned = 0;
                                        timesWaitedInPlace = 0;
                                    }
                                    break;
                                }
                            }
                            
                            //Pathfinding when the player is no longer seen
                            if (!_lineOfSightToTarget)
                            {
                                CurrentFollowState = RaccoonFollowTargetState.MoveToTarget;
                                if (_canCheckPathfinding)
                                {
                                    _canCheckPathfinding = false;
                                    pathfinding.CheckIfCanFollowPath(targetPosition);
                                }

                                //If path is unreachable...
                                if (!pathfinding.PathIsActive && !pathfinding.CheckingPath) //*Double check this in game.
                                {
                                    //Coroutine to wait? Then...
                                    TransitionToState(RaccoonState.Search);
                                }
                            }
                            else
                            {
                                _canCheckPathfinding = true;
                                pathfinding.StopFollowPath();
                            }
                            break;
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

        //Check line of sight to target.
        public bool LineOfSightToTarget(Vector3 targetPos, float fieldOfViewToTarget)
        {
            //Check line of sight to the target.
            float maxDistance = 12f;
            Vector3 targetDirection = targetPos - Motor.Transform.position;

            if (targetDirection.sqrMagnitude < Mathf.Pow(maxDistance, 2) && Vector3.Angle(targetDirection, Motor.CharacterForward) <= fieldOfViewToTarget)
            {
                if (!Physics.Linecast(Motor.Transform.position, targetPos, WallLayerMask))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
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
            switch (CurrentRaccoonState)
            {
                case RaccoonState.Default:
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
        
        //Attack time

        private IEnumerator RepositionTime(float range1, float range2)
        {
            float rand = UnityEngine.Random.Range(range1, range2);
            yield return CustomTimer.Timer(rand);
            _canReposition = true;
        }

        private IEnumerator AttackDelayTime()
        {
            float rand = UnityEngine.Random.Range(.35f, 1.35f);
            yield return CustomTimer.Timer(rand);
            _canAttack = true;
            canStartAttackDelayTime = true;
        }
        private IEnumerator AttackTime(float time)
        {
            yield return CustomTimer.Timer(time);
            _canReposition = true;
            CurrentAttackState = RaccoonAttackState.FollowTarget;
        }

        //Alert state to search state transition time
        private IEnumerator AlertToSearchTransitionTime()
        {
            yield return CustomTimer.Timer(.75f);
            TransitionToState(RaccoonState.Search);
        }

        //Search state time before transition to default
        private IEnumerator SearchStateTime()
        {
            yield return CustomTimer.Timer(2f);
            TransitionToState(RaccoonState.Default);
        }
    }
}

//--------------------------------------------------------------------------------------------------------------------
//LAST TIME ON DRAGONBALL Z:
//Added small fixes to move input, look input, and bool resetting.
//Enemy now seems to transition and (for the most part) move and look properly.

//TO DO NEXT
//Implement repositioning around the player.
//Implement a temporary sword attack movement.
//Make adjustments to the default movement so it rotates and positions itself properly.
//General clean up.

//NOTES
//REMEMBER TO STOP COROUTINES AND RESET BOOLS ON STATE EXIT

