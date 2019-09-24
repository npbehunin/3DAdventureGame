using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KinematicCharacterController;
using System;
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
        Reset
    }

    public enum RaccoonAttackState
    {
        FollowTarget,
        SwordAttack,
        BowAttack,
        RockAttack,
        JumpAttack
    }
    
    public enum RaccoonSwordAttackState
    {
        NormalSwing,
        DoubleSwing,
        Flail,
    }
    
    public enum SwordAttackMovementState
    {
        SwingStart,
        SwingEnd
    }

    public enum DetectionState
    {
        AlmostDetected,
        Detected
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
        private SwordAttackMovementState CurrentSwordAttackMovementState;
        //private RaccoonDefaultState CurrentDefaultState;
        private RaccoonAttackState CurrentAttackState;
        public RaccoonDefaultState SelectedDefaultState;
        private DetectionState CurrentDetectionState;
        
        private float RockAttackDistance = 8f;
        private float JumpAttackDistance = 5f;
        private float MeleeAttackDistance = 2.5f;

        public LayerMask WallLayerMask;
        public Transform Target;
        public Transform Home;
        private Vector3 targetPosition;
        private Vector3 homePosition;
        private Vector3 pathPosition;
        private bool _lineOfSightToTarget;
        private float fieldOfView;
        private float timeWhileTargetSeen = 0;
        private float maxAlertedTime = .75f;
        private int timesAlerted = 0;
        private int randomAttackInt = 0;
        private bool _canChooseRandomAttack;
        private bool _canAttack;
        private bool _canCheckPathfinding;

        public UnitFollowNew pathfinding;
        
        //Coroutines
        private Coroutine AlertToSearchCoroutine;
        private bool canStartAlertToSearch = true;

        private Coroutine SearchStateCoroutine;

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
                    _canChooseRandomAttack = true;
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

                    break;
                }
                case RaccoonState.Attack:
                {
                    pathfinding.StopFollowPath();
                    _canCheckPathfinding = true;
                    break;
                }
                case RaccoonState.Alerted:
                {
                    timeWhileTargetSeen = 0;
                    StopCoroutine(AlertToSearchCoroutine);
                    break;
                }
                case RaccoonState.Search:
                {
                    StopCoroutine(SearchStateCoroutine);
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
            
            switch (CurrentRaccoonState)
            {
                //Default movement
                case RaccoonState.Default:
                {
                    //If enemy is too far from home, move to it.
                    //Use pathfinding as necessary.
                    break;
                }
                //Follow target movement
                case RaccoonState.Attack:
                {
                    if (pathfinding.CanReachTarget)
                    {
                        pathPosition = pathfinding.targetPathPosition;
                        //Set moveinputvector to move towards pathPosition.
                        //Pathfinding movement to path position.
                    }
                    else
                    {
                        //Attack type 1:
                            //If the distance is outside rock throwing distance, move closer.
                            //Throw rocks.
                        //Attack type 2:
                            //If the distance is outside jumping distance, move closer.
                            //Jump.
                        //Attack type 3:
                            //If the distance is outside melee distance, move closer.
                            //Swing melee weapon.
                        
                        //(After the attack is finished, transition to followtarget. This can be done in the
                        //coroutines.)
                    }
                    break;
                }
                
                //(Default movement, zero)
                case RaccoonState.Search:
                case RaccoonState.AlertOthers: 
                    //Etc.
                {

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
            homePosition = Home.transform.position; //Home position.
            switch (CurrentRaccoonState)
            {
                case RaccoonState.Alerted:
                {
                    if (_lineOfSightToTarget)
                    {
                        StopCoroutine(AlertToSearchCoroutine);
                        timeWhileTargetSeen += deltaTime;
                    }
                    //(Otherwise, it stops counting.)

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
            
            //Check line of sight to the target.
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

            //State checking
            switch (CurrentRaccoonState)
            {
                case RaccoonState.Default:
                {
                    if (_lineOfSightToTarget)
                    {
                        TransitionToState(RaccoonState.Alerted);
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

                    //Check if the player is still in line of sight after the coroutine OR if numberOfTimesAlerted > 1.
                        //If true, check if any other enemies don't detect the player.
                            //If true, check if any other enemies are sounding an alarm.
                                //If true, transition to followtarget.
                                //Else, transition to AlertOthers.
                            //If false, transition to followtarget.
                        //If false, transition to search state.
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
                    //If the player or an object is detected, transition to alerted state.
                    //If the enemy is a sword type, walk towards the last seen area.
                        //Once close enough, look around and start a coroutine.
                    //If the enemy is a bow type, look around and start a coroutine.
                    //If the search coroutine ends, transition to default state.
                    break;
                }

                case RaccoonState.Attack:
                {
                    //Choose a random attack option
                    if (_canChooseRandomAttack)
                    {
                        _canChooseRandomAttack = false; //Reset when entering state or performing another attack.
                        float targetDirMagnitude = targetDirection.sqrMagnitude;

                        //Melee, jump, or throw rock
                        if (targetDirMagnitude > Mathf.Pow(JumpAttackDistance, 2))
                        {
                            randomAttackInt = Mathf.RoundToInt(UnityEngine.Random.Range(-.5f, 2.49f));
                        }
                        //Melee, or jump
                        else if (targetDirMagnitude > Mathf.Pow(MeleeAttackDistance, 2) &&
                                 targetDirMagnitude < Mathf.Pow(JumpAttackDistance, 2))
                        {
                            randomAttackInt = Mathf.RoundToInt(UnityEngine.Random.Range(-.5f, 1.49f));
                        }
                        //Melee
                        else if (targetDirMagnitude < Mathf.Pow(MeleeAttackDistance, 2))
                        {
                            randomAttackInt = 0;
                        }
                    }

                    //Transition to an attack state
                    if (_canAttack)
                    {
                        if (randomAttackInt == 0)
                        {
                            CurrentAttackState = RaccoonAttackState.SwordAttack;
                        }
                        else if (randomAttackInt == 1)
                        {
                            CurrentAttackState = RaccoonAttackState.JumpAttack;
                        }
                        else if (randomAttackInt == 2)
                        {
                            CurrentAttackState = RaccoonAttackState.RockAttack;
                        }
                    }

                    //Pathfinding
                    if (!_lineOfSightToTarget)
                    {
                        if (CurrentAttackState == RaccoonAttackState.FollowTarget && _canCheckPathfinding) //(Reset this)
                        {
                            pathfinding.CheckIfCanFollowPath(targetPosition);
                            //Request a path.
                            //Reset cancheckpathfinding after lineofsight is met or enemy exits attack state.
                            //If lineofsight is still false after endofpath OR path can't be reached, transition to search state.
                        }

                        if (pathfinding.EndOfPath)
                        {
                            //Coroutine to wait? Then...
                            TransitionToState(RaccoonState.Search);
                        }
                    }
                    else
                    {
                        pathfinding.StopFollowPath();
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

        public void StopCoroutine(Coroutine coroutine)
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
            }
        }
    
        // COROUTINES
        
        //Attack time
        private IEnumerator AttackTime(float time)
        {
            yield return CustomTimer.Timer(time);
            CurrentAttackState = RaccoonAttackState.FollowTarget;
        }

        //Alert state to search state transition time
        private IEnumerator AlertToSearchTransitionTime()
        {
            yield return CustomTimer.Timer(.75f);
            TransitionToState(RaccoonState.Search);
            canStartAlertToSearch = true;
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
//Polished the attack state and changed the followtarget state to be an attack state.
//Started implementing pathfinding.

//TO DO NEXT
//Tell attack script when to transition to followtarget attack state.
//Improve the pathfinding under beforeupdate.

//(Transition to followTarget when entering the attack state or after an attack is performed.)
//(Set canAttack to true when the position is within the distance of the associated random attack.)
//(Once the attack is executed, canAttack is false and the attack state is followTarget.)

//CURRENT SCRIPT NOTES
//REMEMBER TO STOP COROUTINES ON STATE EXIT

