using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KinematicCharacterController;
using System;
using UnityEditor.Experimental.GraphView;
using Random = System.Random;

namespace KinematicCharacterController.Raccoon
{
    public enum RaccoonState
    {
        Default,
        Search,
        FollowTarget,
        Alerted,
        SwordAttack,
        BowAttack,
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
        Reset
    }
    
    public enum RaccoonSwordAttackState
    {
        NormalSwing,
        DoubleSwing,
        Flail,
    }
    
    public enum SwordAttackState
    {
        SwingStart,
        SwingEnd
    }
    
    public struct RaccoonCharacterInputs
    {
        public Vector3 MoveVector;
        public Vector3 LookVector;
    }
    
    public class RaccoonController : MonoBehaviour, ICharacterController
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
        public Transform HomePosition;
        
        private Collider[] _probedColliders = new Collider[8];
        public Vector3 _moveInputVector;
    
        private Vector3 _lookInputVector;
        private float testLerp = 0f;
        private Vector3 _internalVelocityAdd = Vector3.zero;

        private Vector3 lastInnerNormal = Vector3.zero;
        private Vector3 lastOuterNormal = Vector3.zero;
        
        //Custom stuff
        public RaccoonState CurrentRaccoonState;// { get; private set; }
        private SwordAttackState CurrentSwordAttackState;
        private RaccoonDefaultState CurrentDefaultState;
        public RaccoonDefaultState SelectedDefaultState;
        public UnitFollowNew unitPathfinding;
        
        public LayerMask WallLayerMask;
        public Transform target;
        private int fieldOfView;
        private bool _attackIsActive;
        private bool _attackDelay;
        public bool _lineOfSightToTarget;
        private bool _targetDetected;
        private bool _canSetSwordAttack;
        private bool _canCheckPathfinding = true;
        private bool _canStartReactionTime = true;

        public bool _shouldMoveToHome;
        
        private Coroutine DetectionWaitCoroutine;
        private Coroutine WaitForAlertCoroutine;
        private Coroutine SwordAttackCoroutine;
        private Coroutine SwordMovementCoroutine;
        private Coroutine PathfindingCoroutine;
        private Coroutine SearchTimeCoroutine;

        private Vector3 targetPos;


        private void Start()
        {
            //Handle initial state
            TransitionToState(RaccoonState.Default);
    
            // Assign the characterController to the motor
            Motor.CharacterController = this;
        }

        private void Update()
        {
            //Debug.Log(unitPathfinding.EndOfPath);
            LineOfSightCheck(Motor.CharacterForward, target.position);

            if (CurrentRaccoonState == RaccoonState.Default)
            {
                fieldOfView = 50; //70
            }
            else
            {
                fieldOfView = 360;
            }
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
            switch (state)
            {
                case RaccoonState.Default:
                {
                    _shouldMoveToHome = true;
                    break;
                }
                case RaccoonState.Alerted:
                {
                    WaitForAlertCoroutine = StartCoroutine(AlertStateTransitionDelay()); //Delay before leaving alert state.
                    break;
                }
                case RaccoonState.SwordAttack:
                {
                    SwordAttackCoroutine = StartCoroutine(SwordSwingTime(.5f)); //Temporary sword attack time.
                    SwordMovementCoroutine = StartCoroutine(SwordMovement(.5f));
                    break;
                }
                case RaccoonState.Search:
                {
                    SearchTimeCoroutine = StartCoroutine(SearchStateCoroutine());
                    break;
                }
            }
        }
    
        /// <summary>
        /// Event when exiting a state
        /// </summary>
        public void OnStateExit(RaccoonState state, RaccoonState toState)
        {
            switch (state)
            {
                case RaccoonState.Default:
                {
                    //Sound detection false
                    _canCheckPathfinding = true;
                    break;
                }
                case RaccoonState.FollowTarget:
                {
                    //unitPathfinding.EndOfPath = false; //Reset
                    unitPathfinding.PathIsActive = false;
                    _canCheckPathfinding = true; //Reset
                    break;
                }
            }
        }

        /// <summary>
        /// This is called every frame by the AI script in order to tell the character what its inputs are
        /// </summary>
        public void SetInputs(ref RaccoonCharacterInputs inputs) //AI
        {
            switch (CurrentRaccoonState)
            {
                //Default state
                case RaccoonState.Default:
                {
                    MaxStableMoveSpeed = 5f;
                    if (_shouldMoveToHome)
                    {
                        Vector3 homeDirection = HomePosition.position - Motor.Transform.position;

                        //Debug.Log((Vector3.ProjectOnPlane(homeDirection, Motor.CharacterUp).magnitude));
                        if (Vector3.ProjectOnPlane(homeDirection, Motor.CharacterUp).magnitude > .35f) //Change to take into account home position y.
                        {
                            if (_canCheckPathfinding) //If we can check for a path...
                            {
                                _canCheckPathfinding = false;
                                PathfindingCoroutine = StartCoroutine(PathfindingTiming(HomePosition.position));
                            }
                        }
                        else
                        {
                            _shouldMoveToHome = false;
                            //CurrentDefaultState = SelectedDefaultState; //Choose in inspector
                        }
                        
                        //if (unitPathfinding.CanReachTarget)
                        if (unitPathfinding.PathIsActive)
                        {
                            Vector3 pathPosition = new Vector3();
                            if (unitPathfinding.path.Length > 0)
                            {
                                pathPosition = unitPathfinding.targetPathPosition;
                            }
                            else
                            {
                                pathPosition = HomePosition.position; //(In case the pathfinding returns a length of 0)
                                
                            }
                            Vector3 pathDirection = pathPosition - Motor.Transform.position;
                            //Debug.Log(pathPosition.normalized);
                            _moveInputVector = Vector3.ProjectOnPlane(pathDirection, Motor.CharacterUp).normalized; //Move towards pathPosition.
                            
                        }
                        //else
                        //{
                        //    _shouldMoveToHome = false;
                        //}
                    }
                    else
                    {
                        _moveInputVector = Vector3.zero;
                        switch (CurrentDefaultState)
                        {
                            case RaccoonDefaultState.Sitting:
                            {
                                break;
                            }
                            case RaccoonDefaultState.Patrolling:
                            {
                                break;
                            }
                            case RaccoonDefaultState.Sleeping:
                            {
                                break;
                            }
                            case RaccoonDefaultState.Talking:
                            {
                                break;
                            }
                            case RaccoonDefaultState.Reset:
                            {
                                break;
                            }
                        }
                    }
                    break;
                }
                //Follow target state
                case RaccoonState.FollowTarget:
                {
                    Vector3 targetDirection = new Vector3();
                    targetDirection = targetPos - Motor.Transform.position;
                    
                    if (_lineOfSightToTarget) //If there's line of sight...
                    {
                        MaxStableMoveSpeed = 5f;
                        targetPos = target.transform.position;

                        float attackDistance = 4f;
                    
                        if (targetDirection.sqrMagnitude > Mathf.Pow(attackDistance, 2)) //If the target is outside the attack distance...
                        {
                            _moveInputVector = Vector3.ProjectOnPlane(targetDirection.normalized, Motor.CharacterUp); //Move towards the target.
                        }
                        else
                        {
                            _moveInputVector = Vector3.zero;
                            if (!_attackDelay)
                            {
                                TransitionToState(RaccoonState.SwordAttack);
                            }
                        }
                    }
                    else //If there's NO line of sight...
                    {
                        unitPathfinding.motorUpDirection = Motor.CharacterUp;
                        if (_canCheckPathfinding) //If we can check for a path...
                        {
                            _canCheckPathfinding = false;
                            PathfindingCoroutine = StartCoroutine(PathfindingTiming(targetPos));
                        }
                        
                        //if (unitPathfinding.CanReachTarget)
                        if (unitPathfinding.PathIsActive)
                        {
                            Vector3 pathPosition = unitPathfinding.targetPathPosition;
                            Vector3 pathDirection = pathPosition - Motor.Transform.position;
                            _moveInputVector = Vector3.ProjectOnPlane(pathDirection.normalized, Motor.CharacterUp); //Move towards last seen position.
                        }
                        else
                        {
                            TransitionToState(RaccoonState.Search);
                        }
                        //else
                        //{
                        //    TransitionToState(RaccoonState.Search);
                        //    //Don't transition immediately to default, because canreachtarget starts false.
                        //}

                       //if (unitPathfinding.EndOfPath)
                       //{
                       //    TransitionToState(RaccoonState.Search);
                       //}
                    }

                    break;
                    
                    //TO DO HERE:
                    //1. After completing the path, add wait time where the enemy looks around, then uses pathfinding back to its home position.
                    //2. Add pathfinding when there's line of sight, but the height difference is too high.
                }
                //Sword attack state
                case RaccoonState.SwordAttack:
                {
                    if (_canSetSwordAttack) //*Make sure enemy will facing the player first here. (Angle between direct linecast and characterforward)
                                            //Otherwise, enemy might attack without actually facing the player.
                    {
                        _canSetSwordAttack = false;
                        float attackMethod = Mathf.RoundToInt(UnityEngine.Random.Range(-.51f, 2.49f)); //0, 1, or 2.
                        Debug.Log("Executing attack #" + attackMethod + "!");
                        _moveInputVector = Motor.CharacterForward;
                        switch (attackMethod)
                        {
                            case 0: //Sword attack type 1
                            {
                                
                                break;
                            }
                            case 1: //Sword attack type 2
                            {
                            
                                break;
                            }
                            case 2: //Sword attack type 3
                            {
                                break;
                            }
                        }
                    }
                    
                    //Sword attack movement substates
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
                case RaccoonState.Search:
                {
                    _moveInputVector = Vector3.zero;
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
            switch (CurrentRaccoonState)
            {
                case RaccoonState.FollowTarget:
                {
                    break;
                }
                case RaccoonState.SwordAttack:
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
            switch (CurrentRaccoonState)
                {
                    //When the raccoon is following the target...
                    case RaccoonState.FollowTarget:
                    {
                        if (_lineOfSightToTarget)
                        {
                            _lookInputVector = Vector3.ProjectOnPlane(target.transform.position - Motor.Transform.position, Motor.CharacterUp);
                        }
                        else
                        {
                            _lookInputVector = _moveInputVector;
                        }
                        
                        break;
                    }
                    default:
                    {
                        _lookInputVector = _moveInputVector;
                        break;
                    }
                }
                
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
            switch (CurrentRaccoonState)
            {
                case RaccoonState.Default:
                {
                    if (_lineOfSightToTarget && _canStartReactionTime)
                    {
                        Debug.Log("Detected! Time to react.");
                        DetectionWaitCoroutine = StartCoroutine(DetectionReactionTime());
                        _canStartReactionTime = false;
                    }
                    break;
                }
            }
    
            //Switch to default state if in midair
            if (!Motor.GroundingStatus.IsStableOnGround)
            {
                if (CurrentRaccoonState != RaccoonState.Default)
                {
                    //_timeBeforeFallingStateCoroutine = StartCoroutine(TimeBeforeFallingStateCoroutine());
                    //StartCoroutine(TimeBeforeFallingStateCoroutine());
                }
            }
        }

        public void LineOfSightCheck(Vector3 lookDirection, Vector3 targetPosition)
        {
            //Check line of sight to the target
            float maxDistance = 12f;
            Vector3 targetDirection = targetPosition - Motor.Transform.position;

            if (targetDirection.sqrMagnitude < Mathf.Pow(maxDistance, 2) && Vector3.Angle(targetDirection, Motor.CharacterForward) <= fieldOfView)
            {
                if (!Physics.Linecast(Motor.Transform.position, targetPosition, WallLayerMask))
                {
                    _lineOfSightToTarget = true;
                }
                else
                {
                    _lineOfSightToTarget = false;
                }
            }
            else
            {
                _lineOfSightToTarget = false;
            }
        }

        public void SoundAlertCheck(Vector3 soundPosition)
        {
            //Check surrounding area for a sound effect. (Only in default state)
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
    
        // COROUTINES
        
        //Search state time
        private IEnumerator SearchStateCoroutine()
        {
            yield return CustomTimer.Timer(2f);
            TransitionToState(RaccoonState.Default);
        }

        //Search for a path (pathfinding)
        private IEnumerator PathfindingTiming(Vector3 target)
        {
            yield return CustomTimer.Timer(.2f);
            unitPathfinding.CheckIfCanFollowPath(target);
        }
        
        //Transition to FollowTarget after a sword swing and set an attack delay.
        private IEnumerator SwordSwingTime(float seconds)
        {
            yield return CustomTimer.Timer(seconds);
            _attackDelay = true;
            TransitionToState(RaccoonState.FollowTarget);
            yield return CustomTimer.Timer(2f);
            _canSetSwordAttack = true;
            _attackDelay = false;
        }
        
        //Set the time between SwingStart and SwingEnd in a sword swing.
        private IEnumerator SwordMovement(float time) //Temp
        {
            yield return CustomTimer.Timer(.2f);
            float timeStart = time * .35f;
            float timeEnd = timeStart - time;
            CurrentSwordAttackState = SwordAttackState.SwingStart; //Start swing movement
            yield return CustomTimer.Timer(timeStart);
            CurrentSwordAttackState = SwordAttackState.SwingEnd; //End swing movement
            yield return CustomTimer.Timer(timeEnd);
        }

        //Transition to default state while in midair after x amount of time.
        //private IEnumerator TimeBeforeFallingStateCoroutine()
        //{
        //    yield return CustomTimer.Timer(.1f);
        //    TransitionToState(RaccoonState.Default);
        //}
        //Greyed out for now until we can tell the raccoon to go back to its last state after falling.
        
        //Reaction time to detect something.
        private IEnumerator DetectionReactionTime()
        {
            float reactionTime = .35f;
            yield return CustomTimer.Timer(reactionTime);
            TransitionToState(RaccoonState.Alerted);
            _canStartReactionTime = true; //Reset
        }

        //Alert state time before following a target.
        private IEnumerator AlertStateTransitionDelay()
        {
            yield return CustomTimer.Timer(.75f);
            TransitionToState(RaccoonState.FollowTarget);
        }
    }
}

//CURRENT SCRIPT NOTES
//Enemy will sword attack even if not directly facing the player.
//Some coroutines might cause issues with state transitions unless they're stopped. (Ex: A coroutine to transition to
//followtarget will still run after the enemy is knocked.)

//--------------------------------------------------------------------------------------------------------------------

//TO DO:
//Start the first rewrite. In the next script, we'll have 2 different detection states and 2 rotation states.
//(Try to make it as generic as possible so we can use most of it for other enemies!)

//DETECTION STATES:
//Direct Linecast - Used in follow target. When the player goes behind a wall, the enemy will begin pathfinding.
//Line of sight - Used in default and search. Used when the player isn't detected, and it will always transition to
    //the alerted state.

//ROTATION STATES:
//Movement based - Used in every state that doesn't require looking at a target.
//Target based - Used in states like alerted and follow target. Turns facing towards a target.

//OVERVIEW:

//1. Start in default state. Enemy will move based on the 4 default substates. Line of sight and audio detection is
//enabled. If the enemy is too far away from its home position, use pathfinding to get back. If the path can't be
//completed, remain in default state.

//2. If line of sight is made, transition to the alerted state. In the alerted state, keep track of how much time passes
//between the start of the state and the end. If the time goes over x amount, transition to the followplayer state.
//Otherwise, transition to the search state.
//Target based rotation will be turned on for as long as the player is within line of sight.

//3. Once in the followplayer state, direct linecast detection is turned on and pathfinding will be enabled. IF the
//enemy gets close enough and an x amount of random time passes, the enemy will attack. (We'll implement different
//attacks and group attacks later.) If the player leaves linecast detection, the enemy will wait a brief moment and
//start pathfinding. If pathfinding can't be completed and line of sight is still false, transition to search state.

//4. In the attack state, the enemy will attack until the animation ends or x amount of time passes, then transition
//back to followplayer.

//5. Once the end of the path has been reached, transition to the search state and enable line of sight detection. After
//x amount of time passes, transition back to the default state.

//NOTES
//On a slope or an unreachable area, the enemy might still try to use normal attacks as long as it's within attacking
//distance. Eventually, we should try always implementing pathfinding so the enemy knows if it definitely can't reach
//the player, instead of just checking direct linecast sight.
