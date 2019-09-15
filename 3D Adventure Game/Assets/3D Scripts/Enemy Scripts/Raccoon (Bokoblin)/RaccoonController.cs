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
        Sleeping
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
        
        private Collider[] _probedColliders = new Collider[8];
        private Vector3 _moveInputVector;
    
        private Vector3 _lookInputVector;
        private float testLerp = 0f;
        private Vector3 _internalVelocityAdd = Vector3.zero;

        private Vector3 lastInnerNormal = Vector3.zero;
        private Vector3 lastOuterNormal = Vector3.zero;
        
        //Custom stuff
        public RaccoonState CurrentRaccoonState { get; private set; }
        public LayerMask WallLayerMask;
        public Transform target;
        private int fieldOfView;
        private bool _attackIsActive;
        public bool _attackDelay;
        public bool _lineOfSightToTarget;
        private bool _targetDetected;
        private bool _canSetSwordAttack;
        private Coroutine DetectionWaitCoroutine;
        private Coroutine WaitForAlertCoroutine;
        private Coroutine SwordAttackCoroutine;


        private void Start()
        {
            //Handle initial state
            TransitionToState(RaccoonState.Default);
    
            // Assign the characterController to the motor
            Motor.CharacterController = this;
        }

        private void Update()
        {
            LineOfSightCheck(Motor.CharacterForward, target.position);

            if (CurrentRaccoonState == RaccoonState.Default)
            {
                fieldOfView = 35; //70
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
                    break;
                }
                case RaccoonState.Alerted:
                {
                    WaitForAlertCoroutine = StartCoroutine(AlertStateTransitionDelay()); //Delay before leaving alert state.
                    break;
                }
                case RaccoonState.SwordAttack:
                {
                    SwordAttackCoroutine = StartCoroutine(SwordSwingTime(1.5f)); //Temporary sword attack time.
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
                    break;
                }
            }
        }

        /// <summary>
        /// This is called every frame by the AI script in order to tell the character what its inputs are
        /// </summary>
        public void SetInputs(ref RaccoonCharacterInputs inputs) //AI
        {
            //_moveInputVector = inputs.MoveVector;
            //_lookInputVector = inputs.LookVector;
            switch (CurrentRaccoonState)
            {
                case RaccoonState.FollowTarget:
                {
                    //METHOD 1
                    //If target is outside of attack distance...
                        //Choose a position somewhere within 180 degrees of the target.
                        //Move towards the position.

                        //Otherwise, if it's in the attack distance...
                        //Check the other group enemies.
                        
                        //RULES:
                        //Only 1-2 enemies can swing at a time.
                        //Only 1 enemy can be leaping at a time.
                        //Only 1-2 enemies can be throwing objects at a time.
                        
                        //If outside of melee and jumping range...
                            //Throw objects
                        //If outside of melee range...
                            //Throw objects or jump
                        //If inside melee range...
                            //Transition to sword attack state
                    
                    //METHOD 2
                    //If target is outside of attack distance...
                        //Move towards the target.
                    
                    //Otherwise, if it's in the attack distance...
                        //Transition to the attack state.
                        
                    //(Then, the attack state will handle different sub-attack states and choose one randomly
                    //base on what is equipped and how far the enemy is from the player.)

                    Vector3 targetDirection = target.transform.position - Motor.Transform.position;
                    float attackDistance = 4f;
                    
                    Debug.Log(_moveInputVector);
                    if (targetDirection.sqrMagnitude > Mathf.Pow(attackDistance, 2))
                    {
                        _moveInputVector = Vector3.ProjectOnPlane(targetDirection.normalized, Motor.CharacterUp); //Move towards the target.
                    }
                    else
                    {
                        _moveInputVector = Vector3.zero;
                        if (!_attackDelay)
                        {
                            //Debug.Log("Should transition here");
                            TransitionToState(RaccoonState.SwordAttack);
                        }
                    }
                    break;
                }
                case RaccoonState.SwordAttack:
                {
                    if (_canSetSwordAttack)
                    {
                        _canSetSwordAttack = false;
                        float attackMethod = Mathf.RoundToInt(UnityEngine.Random.Range(0, 2.49f)); //0, 1, or 2.
                        Debug.Log("Executing attack #" + attackMethod + "!");
                        switch (attackMethod)
                        {
                            case 0:
                            {
                                break;
                            }
                            case 1:
                            {
                            
                                break;
                            }
                            case 2:
                            {
                                break;
                            }
                        }
                    }
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
            //switch (CurrentRaccoonState)
            //{
            //    case RaccoonState.Default:
                //case RaccoonState.Crouched:
                {
                    _lookInputVector = _moveInputVector;
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
    
                    //break;
                }
           // }
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
                    if (_lineOfSightToTarget && DetectionWaitCoroutine == null)
                    {
                        DetectionWaitCoroutine = StartCoroutine(DetectionReactionTime());
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
                    StartCoroutine(TimeBeforeFallingStateCoroutine());
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
                if (Physics.Linecast(Motor.Transform.position, targetPosition, WallLayerMask));
                {
                    _lineOfSightToTarget = true;
                }
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

        private IEnumerator SwordSwingTime(float seconds)
        {
            yield return CustomTimer.Timer(seconds);
            _attackDelay = true;
            TransitionToState(RaccoonState.FollowTarget);
            yield return CustomTimer.Timer(2f);
            _canSetSwordAttack = true;
            _attackDelay = false;
        }

        private IEnumerator WaitForSeconds(float seconds)
        {
            yield return CustomTimer.Timer(seconds);
        }

        private IEnumerator TimeBeforeFallingStateCoroutine()
        {
            yield return CustomTimer.Timer(.1f);
            TransitionToState(RaccoonState.Default);
        }
        
        private IEnumerator DetectionReactionTime()
        {
            float reactionTime = .35f;
            yield return CustomTimer.Timer(reactionTime);
            TransitionToState(RaccoonState.Alerted);
        }

        private IEnumerator AlertStateTransitionDelay()
        {
            yield return CustomTimer.Timer(.75f);
            TransitionToState(RaccoonState.FollowTarget);
        }
    }
}

//IDEAS:
//Setup a custom coroutine method that stops any coroutines running in a different state.
