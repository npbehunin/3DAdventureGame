using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KinematicCharacterController;
using System;
using KinematicCharacterController.Nate;

//Movement controller for the fox.

namespace KinematicCharacterController.PetController
{
    public enum PetState //Character states (Walking, crouching, attacking, etc)
    {
        Default, 
        Walk, 
        FollowPath,
        Crouched, 
        SwordAttack,
        SpinAttack,
        RollAttack,
    }

    public class PetController : MonoBehaviour, ICharacterController
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
        private bool _shouldBeCrouching = false;
        private bool _isCrouching = false;
        private bool _cannotUncrouch = false;
        private bool _canCheckPathfinding = true;

        private Vector3 lastInnerNormal = Vector3.zero;
        private Vector3 lastOuterNormal = Vector3.zero;

        public UnitFollowNew pathfinding;
        public LayerMask WallLayerMask;
        
        //Coroutines
        private Coroutine StateTransitionDelayCoroutine;
        private bool canStartTransition = true;

        private void Start()
        {
            //Handle initial state
            TransitionToState(PetState.Default);

            // Assign the characterController to the motor
            Motor.CharacterController = this;
        }

        private void Update()
        {
            SetInputs(); //Temp
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
                    break;
                }
                case PetState.Crouched:
                {
                    StableMovementSharpness = 7f;
                    
                    //Set dimensions and scale
                    Motor.SetCapsuleDimensions(0.5f, 1f, 0.25f); //Scales the hitbox.
                    MeshRoot.localScale = new Vector3(1f, 0.35f, 1.2f); //Scales the mesh root.
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
                    break;
                }
                case PetState.Crouched:
                {
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
            switch (CurrentPetState)
            {
                case PetState.Default:
                case PetState.Crouched:
                {
                    Vector3 playerCharUp = player.Motor.CharacterUp;
                    Vector3 playerCharRight = player.Motor.CharacterRight;
                    Vector3 playerPosition = player.Motor.transform.position;

                    Vector3 rightPos = Vector3.ClampMagnitude(
                        Vector3.ProjectOnPlane(playerCharRight, playerCharUp), 1); //Right of player
                    Vector3 leftPos = -rightPos; //Left of player
                    Vector3 behindPos = Vector3.ClampMagnitude(Vector3.ProjectOnPlane(Vector3.back, 
                        playerCharUp), 1); //Behind the player
                    Vector3 leftBehindPos = Vector3.Cross(leftPos, behindPos); //Left-back corner of player
                    Vector3 rightBehindPos = Vector3.Cross(rightPos, behindPos); //Right-back corner of player
                    //Vector3 dir = (playerPosition + rightPos) - Motor.transform.position; //Direction to check
                    Vector3 dir = playerPosition - Motor.transform.position; //Player's general position. Is this needed?
                    
                    //Random number between 0-4.
                    //Number will choose one of the 5 positions.
                    //Set the direction to be between this pet and the movePoint.
                    //Run the distance check below.
                    
                    Debug.DrawRay(Motor.transform.position, dir, Color.yellow);
                    float maxDist = .5f;
                    if (dir.sqrMagnitude > Mathf.Pow(maxDist, 2))
                    {
                        _moveInputVector = Vector3.ProjectOnPlane(dir.normalized, Motor.CharacterUp);
                        _lookInputVector = Vector3.ProjectOnPlane(_moveInputVector, Motor.CharacterUp);
                    }
                    else
                    {
                        _moveInputVector = Vector3.zero;
                        _lookInputVector = Vector3.ProjectOnPlane(player.Motor.CharacterForward, Motor.CharacterUp);
                    }
                    
                    break;
                    
                    //TO DO:
                    //QUESTION: Should the pet always remain on the right side of the player?
                    //REASON: First off, the pet's movement will feel consistent and the player will
                    //know where the pet will be instinctively. Second, it would follow correctly with the sword swing
                    //under the assumption the player model will be right-handed. Lastly, it would simplify animations
                    //that requires the pet to be on a certain side, such as the pet holding the arrow pouch while the 
                    //player shoots and reloads.
                    //Set up pathfinding or teleportation if pet is out of reach.
                }
                case PetState.FollowPath:
                {
                    Vector3 pathPosition = pathfinding.targetPathPosition;
                    _moveInputVector = Vector3.ProjectOnPlane(pathPosition, Motor.CharacterUp);
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
                    MaxStableMoveSpeed = 7f;
                    break;
                }
                case PetState.Crouched:
                {
                    //Run overlap test
                    if (Motor.CharacterOverlap(
                            Motor.TransientPosition,
                            Motor.TransientRotation,
                            _probedColliders,
                            Motor.CollidableLayers,
                            QueryTriggerInteraction.Ignore) > 0)
                    {
                        _cannotUncrouch = true;
                    }
                    else
                    {
                        _cannotUncrouch = false;
                    }
                    
                    MaxStableMoveSpeed = 3f;
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
                case PetState.Crouched:
                case PetState.FollowPath:
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
            //Player state checking.
            if (!_cannotUncrouch && CurrentPetState != PetState.FollowPath) //Temporary check to make sure the pet can uncrouch before transitioning.
            {
                if (NewCharacterState != player.CurrentCharacterState) //WHEN THE PLAYER STATE CHANGES...
                {
                    canStartTransition = true; //Allow pet state transitioning (once)
                    NewCharacterState = player.CurrentCharacterState;
                }

                if (canStartTransition)
                {
                    canStartTransition = false;
                    switch (player.CurrentCharacterState)
                    {
                        case CharacterState.Default:
                        {
                            StateTransitionDelayCoroutine = StartCoroutine(StateTransitionDelay(PetState.Default, .12f));
                            break;
                        }
                        case CharacterState.Crouched:
                        {
                            StateTransitionDelayCoroutine = StartCoroutine(StateTransitionDelay(PetState.Crouched, .12f));
                            break;
                        }
                        case CharacterState.SwordAttack:
                        {
                            StateTransitionDelayCoroutine = StartCoroutine(StateTransitionDelay(PetState.SwordAttack, .12f));
                            break;
                        }
                        case CharacterState.RollAttack:
                        {
                            TransitionToState(PetState.RollAttack); //Instant
                            break;
                        }
                    } 
                }
            }
            
            //Pet state checking.
            Vector3 playerPosition = player.Motor.transform.position;
            switch (CurrentPetState)
            {
                case PetState.Default:
                case PetState.Crouched:
                {
                    //Check pathfinding.
                    //Debug.Log(LineOfSightToTarget(playerPosition));
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
                            Motor.transform.position = playerPosition; //*Fix this here so the pet will teleport.
                        }

                        if (pathfinding.PathIsActive)
                        {
                            TransitionToState(PetState.FollowPath);
                        }
                    }
                    break;
                }
                case PetState.FollowPath:
                {
                    if (LineOfSightToTarget(playerPosition))
                    {
                        pathfinding.StopFollowPath();
                        TransitionToState(PetState.Default);
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

        // COROUTINES

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
//TO DO
//PET MOVEMENT
//1. Allow the pet to hover around 1 of 5 points around the player. (Left, right, left-down, right-down, and down.)
//2. The pet will randomly choose a new position after x amount of time has passed AND the pet is outside x distance.
//3. If one of the points is obstructed by a wall, too close to a ledge, OR too low below the player's height, the pet
//will also choose a new point.
    //(Is (spherecast) from movePoint colliding with a wall?
    //(Is movePoint too high up from the ground?) (Take the point and raycast down until step height, OR extend the
    //movePoint outwards over a nearby ledge, then cast down.
    //(Is the up distance between movePoint and the player's point too big?)
//4. Each point will have a slight variation the pet can move towards, so it's not always moving to the exact point.
    //Take the point and look towards a random direction within x random distance.
    //Might make the pet feel more real and fluid.
//5. Use pathfinding as needed.

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