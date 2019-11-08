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
    }

    public enum PetDeviationState
    {
        Default,
        MoveCloser,
        MoveAway,
    }

    public enum PetCrouchedState
    {
        Idle,
        Walk,
        MoveCloser,
        MoveAway,
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
        public PetDeviationState currentDeviationState;
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
        
        //Ledge detection testing
        private Vector3[] ledgeNodeVectorArray;
        private bool[] ledgeNodeBoolArray;

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
            //TestLedgeDetection();
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
                    currentDefaultState = PetDefaultState.Run;
                    break;
                }
                case PetState.Crouched:
                {
                    break;
                }
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
            switch (CurrentPetState)
            {
                case PetState.Default:
                {
                    switch (currentDefaultState)
                    {
                        case PetDefaultState.Run:
                        {
                            switch (currentDeviationState)
                            {
                                case PetDeviationState.Default:
                                {
                                    //Regular running movement.
                                    break;
                                }
                                case PetDeviationState.MoveAway:
                                {
                                    //Movement away from the player.
                                    break;
                                }
                                case PetDeviationState.MoveCloser:
                                {
                                    //Movement closer to the player.
                                    break;
                                }
                            }
                            break;
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
            switch (CurrentPetState)
            {
                case PetState.Default:
                {
                    //If (shouldBeCrouching)
                        //TransitionTo(CrouchedState)
                    
                    SetDeviationState();
                    switch (currentDefaultState)
                    {
                        case PetDefaultState.Run:
                        {
                            //Crouched state = walk.
                            switch (currentDeviationState)
                            {
                                case PetDeviationState.Default:
                                {
                                    //Random deviation
                                    break;
                                }
                                case PetDeviationState.MoveAway:
                                case PetDeviationState.MoveCloser:
                                {
                                    //Wait for x amount of time.
                                        //Randomly transition to moveAway or moveCloser.
                                    break;
                                }
                                
                            }
                            break;
                        }
                        case PetDefaultState.Walk:
                        {
                            //Crouched state = walk.
                            //if (distanceToPlayerSideDir < x)
                                //Transition to idle.
                            //if (playerVelocity > x)
                                //Transition to run.
                                
                            switch (currentDeviationState)
                            {
                                case PetDeviationState.Default:
                                {
                                    //(No random switching.)
                                    break;
                                }
                                case PetDeviationState.MoveAway:
                                case PetDeviationState.MoveCloser:
                                {
                                    //Wait for x amount of time OR until angle is met.
                                    //Transition back to default.
                                    //Wait for x amount of time and reset canDeviate.
                                    break;
                                }
                                
                            }
                            break;
                        }
                        case PetDefaultState.Idle:
                        {
                            //Crouched state = idle.
                            //if (distanceToPlayer > x) or (playerVelocity > x)...
                                //Transition to run state.
                            break;
                        }
                    }
                    break;
                }
                case PetState.Crouched:
                {
                    SetDeviationState();
                    switch (currentCrouchedState)
                    {
                        case PetCrouchedState.Walk:
                        {
                            //Crouched state = walk.
                            //if (checkRequiredToMoveCloser)
                                //currentDeviationState = moveCloser.
                            //else if (checkRequiredToMoveAway)
                                //currentDeviationState = moveAway.
                            //else
                                //currentDeviationState = default.
                                
                            switch (currentDeviationState)
                            {
                                case PetDeviationState.Default:
                                {
                                    //Nothing for now.
                                    break;
                                }
                                case PetDeviationState.MoveAway:
                                case PetDeviationState.MoveCloser:
                                {
                                    //Wait for x amount of time OR until angle is met.
                                    //Transition back to default.
                                    //Wait for x amount of time and reset canDeviate.
                                    break;
                                }
                                
                            }
                            break;
                        }
                        case PetCrouchedState.Idle:
                        {
                            //Crouched state = idle.
                            break;
                        }
                    }
                    break;
                }
            }
            //MoveInput = Slerp(moveinput, targetInput, moveInputSlerpValue)
        }

        //Eh, don't use this yet vvv
        private void SetDeviationState()
        {
            //if (canDeviate)
                //if (checkRequiredToMoveCloser)
                    //currentDeviationState = moveCloser.
                //else if (checkRequiredToMoveAway)
                    //currentDeviationState = moveAway.
                //else
                    //currentDeviationState = default.
                    
            //(Make sure default, moveCloser, and moveAway can transition to each other freely.
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
        private IEnumerator TimeBeforeFallingStateCoroutine()
        {
            yield return CustomTimer.Timer(.1f);
            TransitionToState(PetState.Default);
        }
    }
}

//TO DO:
//Implement deviation that isn't active/activates during obstacle checks like ledge checking and wall detection.

//METHOD 1:
//Use the moveCloser and moveAway states for random deviation, and transition to the default deviation state if a ledge,
    //slope, or wall obstacle is detected. The default state will handle any new move inputs for obstacles until none
    //are detected. (Temporary solution for now)
//Use the psd file reference for ledge detection moveInput.
//Transition to moveCloser upon wall or slope detection.