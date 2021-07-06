using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.InputSystem;


public class OurMinifigController : MonoBehaviour
{
    public GameObject Minifig;
    // Constants.
    const float stickyTime = 0.05f;
    const float stickyForce = 9.6f;
    const float coyoteDelay = 0.1f;

    const float distanceEpsilon = 0.1f;
    const float angleEpsilon = 0.1f;


    // Internal classes used to define targets when automatically animating.
    class MoveTarget
    {
        public Vector3 destination;
        public float minDistance;
        public Action onComplete;
        public float onCompleteDelay;
        public float moveDelay;
        public bool cancelSpecial;
        public float speedMultiplier;
        public float rotationSpeedMultiplier;
        public Vector3? turnToWhileCompleting;
    }
    
    class TurnTarget
    {
        public Vector3 target;
        public float minAngle;
        public Action onComplete;
        public float onCompleteDelay;
        public float turnDelay;
        public bool cancelSpecial;
        public float rotationSpeedMultiplier;
    }

    // State used when automatically animating.
    enum State
    {
        Idle,
        Moving,
        CompletingMove,

        Turning,
        CompletingTurn,

    }

    [Header("Movement")]

    public float maxForwardSpeed = 6f;
    [Range(0, 10)]
    public float maxBackwardSpeed = 4f;
    [Range(1, 100)]
    public float acceleration = 60.0f;
    // TODO Add sensible range when animations are fixed.
    [Range(0, 2000)]
    public float maxRotateSpeed = 1000f;
    [Range(0, 2500)]
    public float rotateAcceleration = 600f;
    public float jumpSpeed = 20f;
    public float gravity = 40f;
    private Vector2 _movement = new Vector2();

    //Our Custom Variables
    /// <summary>
    /// Whether the player has died
    /// </summary>
    public bool died = false;
    
    /// <summary>
    /// This variable is needed for the GameManagerR to see whether it has already noticed the death of this player
    /// </summary>
    public bool noticedDeath = false;
    
    /// <summary>
    /// Place (as in "rank") of this player. Will get updated during the game.
    /// </summary>
    public int place = 1;
    
    /// <summary>
    /// The GameManagerR sets this to true if the game is over
    /// </summary>
    public bool gameOver = false;

    /// <summary>
    /// The type of item that the player has at the moment. e.g. batarang
    /// </summary>
    public string itemType = "";
    
    /// <summary>
    /// 3D Vector representing the force knocking the player back from getting hit
    /// </summary>
    public Vector3 _knockback = Vector3.zero;
    
    /// <summary>
    /// How much damage the player has already taken from opponents
    /// ~ knockback
    /// </summary>
    public int damage = 0;
    
    /// <summary>
    /// Damage that is dealt to other players if this player hits them 
    /// </summary>
    public int strength = 10;
    
    /// <summary>
    /// Maximum distance an opponent to this player , where the opponent will still be hit
    /// </summary>
    public float hitRange = 2;

    /// <summary>
    /// Position of second platform where players will be teleported when dead
    /// </summary>
    public Vector3 endZone = new Vector3(-24,7,0);

    /// <summary>
    /// Whether this player has picked up an item in his hand
    /// </summary>
    public bool hasItem = false;

    /// <summary>
    /// Reference to the iceberg that moves with the player and is set to visible if the player gets frozen
    /// </summary>
    public GameObject iceBerg;

    private bool frozen = false;

    public Item item = null;


    [Header("Audio")]

    public List<AudioClip> stepAudioClips = new List<AudioClip>();
   

    public AudioClip jumpAudioClip;
    public AudioClip doubleJumpAudioClip;
    public AudioClip landAudioClip;
    public AudioClip explodeAudioClip;
    public AudioClip throwPunchAudioClip;
    public AudioClip receivePunchAudioClip;
    
    public AudioClip LaserAudio1;
    public AudioClip LaserAudio2;
    public AudioClip deadAudioClip;
    public AudioClip applausAudioClip;
    public AudioClip smallGunAudioClip;
    public AudioClip bigGunAudioClip;
    public AudioClip batAudioClip;
    public AudioClip boomerangAudioClip;

    [Header("Controls")]
    [SerializeField]
    bool inputEnabled = true;
    [SerializeField, Range(0, 10)]
    int maxJumpsInAir = 5;

    public enum SpecialAnimation
    {
        AirGuitar = 0,
        Ballerina = 1,
        Crawl = 2,
        CrawlOnWallLeft = 3,
        CrawlOnWallRight = 4,
        Dance = 5,
        Flexing = 6,
        Flip_No_Y_Axis = 7,
        HatSwap = 8,
        HatSwap2 = 9,
        Idle_Light = 10,
        // Unused 11
        IdleHeavy = 12,
        IdleImpatient = 13,
        Jump = 14,
        Jump_GoingDown = 15,
        Jump_GoingUp = 16,
        Jump_LandingBounce = 17,
        Jump_Midair = 18,
        Jump_NoLanding_No_Y_Axis = 19,
        Jump_wBounce = 20,
        KickRightFoot = 21,
        Laughing = 22,
        LeftHandReaction = 23,
        LegReactive = 24,
        LookAbove = 25,
        LookingAround = 26,
        LookingDown = 27,
        LookingUp = 28,
        MoonWalk = 29,
        PantsSwap = 30,
        RightHandReactive = 31,
        Run = 32,
        // Unused 33 
        Skipping = 34,
        Sleeping = 35,
        Slide = 36,
        Sneezing = 37,
        Snoozing = 38,
        Spiderman = 39,
        Spin = 40,
        Stretching = 41,
        Superman_Flying = 42,
        Superman_Setoff = 43,
        Torso_Reactive = 44,
        Turn_90_Left = 45,
        Turn_90_Right = 46,
        Walk = 47,
        WalkBackwards = 48,
        Wave = 49
    }


    CharacterController controller;
    Animator animator;
    AudioSource audioSource;

    bool airborne;
    float airborneTime;
    int jumpsInAir;
    Vector3 directSpeed;
    bool stepped;

    List<MoveTarget> moves = new List<MoveTarget>();
    MoveTarget currentMove;
    TurnTarget currentTurnTarget;
    State state;
    float waitedTime = 0.0f;

    float speed;
    float rotateSpeed;
    Vector3 moveDelta = Vector3.zero;
    bool stopSpecial;
    bool cancelSpecial;

    float externalRotation;
    Vector3 externalMotion;

    Transform groundedTransform;
    Vector3 groundedLocalPosition;
    Vector3 oldGroundedPosition;
    Quaternion oldGroundedRotation;

    int speedHash = Animator.StringToHash("Speed");
    int rotateSpeedHash = Animator.StringToHash("Rotate Speed");
    int groundedHash = Animator.StringToHash("Grounded");
    int jumpHash = Animator.StringToHash("Jump");
    int playSpecialHash = Animator.StringToHash("Play Special");
    int cancelSpecialHash = Animator.StringToHash("Cancel Special");
    int specialIdHash = Animator.StringToHash("Special Id");
    int punchHash = Animator.StringToHash("Punch");
    int swordHash = Animator.StringToHash("Sword");
    int throwHash = Animator.StringToHash("Throw");
    int shootHash = Animator.StringToHash("Shoot");

    Action<bool> onSpecialComplete;

    void OnValidate()
    {
        maxForwardSpeed = Mathf.Clamp(maxForwardSpeed, 5, 30);
    }

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = Minifig.GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();

        // Initialise animation.
        animator.SetBool(groundedHash, true);

        // Make sure the Character Controller is grounded if starting on the ground.
        controller.Move(Vector3.down * 0.01f);
    }

    private void Start()
    {
        string controlScheme = GetComponent<PlayerInput>().defaultControlScheme;
        GetComponent<PlayerInput>().SwitchCurrentControlScheme(controlScheme, Keyboard.current);
        audioSource.PlayDelayed(0.5f);
        



    }

    void Update()
    {
        // Handle input.
        if (inputEnabled)
        {
            // Calculate direct speed and speed.
            var right = Vector3.right;
            var forward = Vector3.forward;
            if (Camera.main)
            {
                right = Camera.main.transform.right;
                right.y = 0.0f;
                right.Normalize();
                forward = Camera.main.transform.forward;
                forward.y = 0.0f;
                forward.Normalize();
            }

            var targetSpeed = right * _movement.x;
            targetSpeed += forward * _movement.y;
            if (targetSpeed.sqrMagnitude > 0.0f)
            {
                targetSpeed.Normalize();
            }
            targetSpeed *= maxForwardSpeed;

            var speedDiff = targetSpeed - directSpeed;
            if (speedDiff.sqrMagnitude < acceleration * acceleration * Time.deltaTime * Time.deltaTime)
            {
                directSpeed = targetSpeed;
            }
            else if (speedDiff.sqrMagnitude > 0.0f)
            {
                speedDiff.Normalize();

                directSpeed += speedDiff * acceleration * Time.deltaTime;
            }
            speed = directSpeed.magnitude;

            // Calculate rotation speed - ignore rotate acceleration.
            rotateSpeed = 0.0f;
            if (targetSpeed.sqrMagnitude > 0.0f)
            {
                var localTargetSpeed = transform.InverseTransformDirection(targetSpeed);
                var angleDiff = Vector3.SignedAngle(Vector3.forward, localTargetSpeed.normalized, Vector3.up);

                if (angleDiff > 0.0f)
                {
                    rotateSpeed = maxRotateSpeed;
                }
                else if (angleDiff < 0.0f)
                {
                    rotateSpeed = -maxRotateSpeed;
                }

                // Assumes that x > NaN is false - otherwise we need to guard against Time.deltaTime being zero.
                if (Mathf.Abs(rotateSpeed) > Mathf.Abs(angleDiff) / Time.deltaTime)
                {
                    rotateSpeed = angleDiff / Time.deltaTime;
                }
            }

            // Calculate move delta.
            moveDelta = new Vector3(directSpeed.x, moveDelta.y, directSpeed.z);


            // Check if player is grounded.
            if (!airborne)
            {
                jumpsInAir = maxJumpsInAir;
            }


            // Cancel special.
            cancelSpecial = !Mathf.Approximately(Input.GetAxis("Vertical"), 0) || !Mathf.Approximately(Input.GetAxis("Horizontal"), 0) || Input.GetButtonDown("Jump");

        }
        else
        {
            // Handle automatic animation.

            waitedTime += Time.deltaTime;

            switch (state)
            {
                case State.Idle:
                    {
                        // Stop moving.
                        MoveInDirection(transform.forward, 0.0f, false, 0.0f, 0.0f, 0.0f, false);
                        break;
                    }
                case State.Moving:
                    {
                        if (waitedTime > currentMove.moveDelay)
                        {
                            var direction = currentMove.destination - transform.position;

                            // Neutralize y component.
                            direction.y = 0.0f;

                            if (direction.magnitude > currentMove.minDistance + distanceEpsilon)
                            {
                                var shouldBreak = currentMove.onCompleteDelay > 0.0f || moves.Count == 1 || (moves.Count > 1 && moves[1].moveDelay > 0.0f);
                                MoveInDirection(direction, currentMove.minDistance, shouldBreak, currentMove.speedMultiplier, 0.0f, currentMove.rotationSpeedMultiplier, currentMove.cancelSpecial);
                            }
                            else
                            {
                                if (currentMove.onCompleteDelay > 0.0f)
                                {
                                    SetState(State.CompletingMove);
                                }
                                else
                                {
                                    CompleteMove();
                                }
                            }
                        }
                        else
                        {
                            // Set speed, move delta and rotation speed.
                            speed = 0.0f;
                            moveDelta = new Vector3(0.0f, moveDelta.y, 0.0f);
                            rotateSpeed = 0.0f;
                        }
                    }
                    break;
                case State.CompletingMove:
                    {
                        // Possibly turn to position.
                        if (currentMove.turnToWhileCompleting.HasValue)
                        {
                            var turnToDirection = currentMove.turnToWhileCompleting.Value - transform.position;
                            if (turnToDirection.magnitude > distanceEpsilon)
                            {
                                TurnToDirection(turnToDirection, 0.0f, currentMove.rotationSpeedMultiplier, currentMove.cancelSpecial);
                            }
                        }

                        if (waitedTime > currentMove.onCompleteDelay)
                        {
                            CompleteMove();
                        }
                    }
                    break;
                case State.Turning:
                    {
                        if (waitedTime > currentTurnTarget.turnDelay)
                        {
                            var direction = currentTurnTarget.target - transform.position;

                            // Neutralize y component.
                            direction.y = 0.0f;

                            if (direction.magnitude > distanceEpsilon)
                            {
                                TurnToDirection(direction, currentTurnTarget.minAngle, currentTurnTarget.rotationSpeedMultiplier, currentTurnTarget.cancelSpecial);
                            }

                            if (Vector3.Angle(transform.forward, direction) <= currentTurnTarget.minAngle + angleEpsilon)
                            {
                                if (currentTurnTarget.onCompleteDelay > 0.0f)
                                {
                                    SetState(State.CompletingTurn);
                                }
                                else
                                {
                                    CompleteTurn();
                                }
                            }
                        }
                        else
                        {
                            // Set speed, move delta and rotation speed.
                            speed = 0.0f;
                            moveDelta = new Vector3(0.0f, moveDelta.y, 0.0f);
                            rotateSpeed = 0.0f;
                        }
                        break;
                    }
                case State.CompletingTurn:
                    {
                        if (waitedTime > currentTurnTarget.onCompleteDelay)
                        {
                            CompleteTurn();
                        }
                        break;
                    }
            }
        }

        // Handle external motion.
        externalMotion = Vector3.zero;
        externalRotation = 0.0f;

        var wasGrounded = controller.isGrounded;

        if (!controller.isGrounded)
        {
            if(transform.position.y < -10){
                //Player fell off -> dies
                
                died = true;
                damage = 0; //Can still get new damage in endzone but will not be shown in GUI
                TeleportTo(endZone);
            }

            // Apply gravity.
            moveDelta.y -= gravity * Time.deltaTime;

            groundedTransform = null;

            airborneTime += Time.deltaTime;
        }
        else
        {
            // Apply external motion and rotation.
            if (groundedTransform && Time.deltaTime > 0.0f)
            {
                var newGroundedPosition = groundedTransform.TransformPoint(groundedLocalPosition);
                externalMotion = (newGroundedPosition - oldGroundedPosition) / Time.deltaTime;
                oldGroundedPosition = newGroundedPosition;

                var newGroundedRotation = groundedTransform.rotation;
                // FIXME Breaks down if rotating more than 180 degrees per frame.
                var diffRotation = newGroundedRotation * Quaternion.Inverse(oldGroundedRotation);
                var rotatedRight = diffRotation * Vector3.right;
                rotatedRight.y = 0.0f;
                if (rotatedRight.magnitude > 0.0f)
                {
                    rotatedRight.Normalize();
                    externalRotation = Vector3.SignedAngle(Vector3.right, rotatedRight, Vector3.up) / Time.deltaTime;
                }
                oldGroundedRotation = newGroundedRotation;
            }
        }

        // Move minifig - check if game object was made inactive in some callback to avoid warnings from CharacterController.Move.
        if (gameObject.activeInHierarchy)
        {
            // Use a sticky move to make the minifig stay with moving platforms.
            var stickyMove = airborneTime < stickyTime ? Vector3.down * stickyForce * Time.deltaTime : Vector3.zero;
            controller.Move((moveDelta + externalMotion) * Time.deltaTime + stickyMove);
        }

        // If becoming grounded by this Move, reset y movement and airborne time.
        if (!wasGrounded && controller.isGrounded)
        {
            // Play landing sound if landing sufficiently hard.
            if (moveDelta.y < -5.0f)
            {
                if (landAudioClip)
                {
                    audioSource.PlayOneShot(landAudioClip);
                }
            }

            moveDelta.y = 0.0f;
            _knockback.y = 0.0f;
            airborneTime = 0.0f;
        }

        // Update airborne state.
        airborne = airborneTime >= coyoteDelay;

        // Rotate minifig.
        transform.Rotate(0, (rotateSpeed + externalRotation) * Time.deltaTime, 0);

        // Apply _knockback to transform
        Vector3 position = transform.position + _knockback;
        // Keep X-position of minifig at 0.
        if(!died || gameOver){
            position.x = 0;
        }else{
            position.x = endZone.x;
        }
        TeleportTo(position);
        // Decrease the knockback.
        float delta = 0.93f;// (Time.deltaTime * 100f); //Movement becomes weird when multiplying with deltaTime
        _knockback = _knockback * delta;
        if (Mathf.Abs(_knockback.z) < 0.05f)
            _knockback.z = 0f;

        // Stop special if requested.
        cancelSpecial |= stopSpecial;
        stopSpecial = false;

        // Update animation - delay airborne animation slightly to avoid flailing arms when falling a short distance.
        animator.SetBool(cancelSpecialHash, cancelSpecial);
        animator.SetFloat(speedHash, speed);
        animator.SetFloat(rotateSpeedHash, rotateSpeed);
        animator.SetBool(groundedHash, !airborne);
    }

    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;
    }

    public void PlaySpecialAnimation(SpecialAnimation animation, AudioClip specialAudioClip = null, Action<bool> onSpecialComplete = null)
    {
        animator.SetBool(playSpecialHash, true);
        animator.SetInteger(specialIdHash, (int)animation);

        if (specialAudioClip)
        {
            audioSource.PlayOneShot(specialAudioClip);
        }

        this.onSpecialComplete = onSpecialComplete;
    }

    public void StopSpecialAnimation()
    {
        stopSpecial = true;
    }

    /// <summary>
    /// Do not call this directly.
    /// Called from SpecialAnimationBehaviour to signal that a special animation finished.
    /// </summary>
    public void SpecialAnimationFinished()
    {
        // Do callback.
        onSpecialComplete?.Invoke(animator.GetBool(cancelSpecialHash));
    }

    public void TeleportTo(Vector3 position)
    {
        controller.enabled = false;
        transform.position = position;
        controller.enabled = true;
    }
    
    public void StopTurning()
    {
        currentTurnTarget = null;
        UpdateState();
    }

    // Animation event.
    public void StepFoot()
    {
        if (!stepped)
        {
            if (stepAudioClips.Count > 0)
            {
                var stepAudioClip = stepAudioClips[UnityEngine.Random.Range(0, stepAudioClips.Count)];
                if (stepAudioClip)
                {
                    audioSource.PlayOneShot(stepAudioClip);
                }
            }
        }
        stepped = true;
    }

    // Animation event.
    public void LiftFoot()
    {
        stepped = false;
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (controller.isGrounded)
        {
            RaycastHit raycastHit;
            if (Physics.SphereCast(transform.position + Vector3.up * 0.8f, 0.8f, Vector3.down, out raycastHit, 0.1f, -1, QueryTriggerInteraction.Ignore))
            {
                groundedTransform = raycastHit.collider.transform;
                oldGroundedPosition = raycastHit.point;
                groundedLocalPosition = groundedTransform.InverseTransformPoint(oldGroundedPosition);
                oldGroundedRotation = groundedTransform.rotation;
            }
        }
    }

    // Assumes that direction is not a zero vector.
    void MoveInDirection(Vector3 direction, float minDistance, bool breakBeforeTarget, float speedMultiplier, float minAngle, float rotationSpeedMultiplier, bool cancelSpecial)
    {
        // Distance required to stop current speed.
        var breakDistance = (speed * speed) / (2.0f * acceleration);

        // If breaking before target, only set target speed if still possible to break.
        var targetSpeed = 0.0f;
        if (!breakBeforeTarget || direction.magnitude - minDistance > breakDistance + distanceEpsilon)
        {
            targetSpeed = speedMultiplier * maxForwardSpeed;
        }

        // Adjust speed based on target speed.
        if (targetSpeed > speed)
        {
            speed = Mathf.Min(targetSpeed, speed + acceleration * Time.deltaTime);
        }
        else if (targetSpeed < speed)
        {
            speed = Mathf.Max(targetSpeed, speed - acceleration * Time.deltaTime);
        }

        // Calculate move delta - prevent overshoot by limiting speed.
        // Assumes that direction is not zero length as it will cause issues when Time.deltaTime is zero.
        var moveDirection = direction.normalized * Mathf.Min(speed, direction.magnitude / Time.deltaTime);
        moveDelta = new Vector3(moveDirection.x, moveDelta.y, moveDirection.z);

        // Angle required to stop current rotate speed.
        var breakAngle = (rotateSpeed * rotateSpeed) / (2.0f * rotateAcceleration);

        // Only set target rotate speed if still possible to break.
        var targetRotateSpeed = 0.0f;
        var angleDiff = Vector3.SignedAngle(transform.forward, direction.normalized, Vector3.up);

        if (Mathf.Abs(angleDiff) - minAngle > breakAngle + angleEpsilon)
        {
            targetRotateSpeed = Mathf.Clamp(angleDiff, -1.0f, 1.0f) * rotationSpeedMultiplier * maxRotateSpeed;
        }

        // Adjust rotate speed based on target rotate speed.
        if (targetRotateSpeed > rotateSpeed)
        {
            rotateSpeed = Mathf.Min(targetRotateSpeed, rotateSpeed + rotateAcceleration * Time.deltaTime);
        }
        else if (targetRotateSpeed < rotateSpeed)
        {
            rotateSpeed = Mathf.Max(targetRotateSpeed, rotateSpeed - rotateAcceleration * Time.deltaTime);
        }

        // Prevent overshoot by limiting rotateSpeed.
        if (angleDiff < 0.0f && rotateSpeed < 0.0f)
        {
            rotateSpeed = Mathf.Max(rotateSpeed, angleDiff / Time.deltaTime);
        }
        else if (angleDiff > 0.0f && rotateSpeed > 0.0f)
        {
            rotateSpeed = Mathf.Min(rotateSpeed, angleDiff / Time.deltaTime);
        }

        // Cancel special.
        this.cancelSpecial = cancelSpecial;
    }

    // Assumes the direction is not a zero vector.
    void TurnToDirection(Vector3 direction, float minAngle, float rotationSpeedMultiplier, bool cancelSpecial)
    {
        speed = Mathf.Max(0.0f, speed - acceleration * Time.deltaTime);

        // Calculate move delta - prevent overshoot by limiting speed.
        var moveDirection = transform.forward * speed;
        moveDelta = new Vector3(moveDirection.x, moveDelta.y, moveDirection.z);

        // Angle required to stop current rotate speed.
        var breakAngle = (rotateSpeed * rotateSpeed) / (2.0f * rotateAcceleration);

        // Only set target rotate speed if still possible to break.
        var targetRotateSpeed = 0.0f;
        var angleDiff = Vector3.SignedAngle(transform.forward, direction.normalized, Vector3.up);

        if (Mathf.Abs(angleDiff) - minAngle > breakAngle + angleEpsilon)
        {
            targetRotateSpeed = Mathf.Clamp(angleDiff, -1.0f, 1.0f) * rotationSpeedMultiplier * maxRotateSpeed;
        }

        // Adjust rotate speed based on target rotate speed.
        if (targetRotateSpeed > rotateSpeed)
        {
            rotateSpeed = Mathf.Min(targetRotateSpeed, rotateSpeed + rotateAcceleration * Time.deltaTime);
        }
        else if (targetRotateSpeed < rotateSpeed)
        {
            rotateSpeed = Mathf.Max(targetRotateSpeed, rotateSpeed - rotateAcceleration * Time.deltaTime);
        }

        // Prevent overshoot by limiting rotateSpeed.
        if (angleDiff < 0.0f && rotateSpeed < 0.0f)
        {
            rotateSpeed = Mathf.Max(rotateSpeed, angleDiff / Time.deltaTime);
        }
        else if (angleDiff > 0.0f && rotateSpeed > 0.0f)
        {
            rotateSpeed = Mathf.Min(rotateSpeed, angleDiff / Time.deltaTime);
        }

        // Cancel special.
        this.cancelSpecial = cancelSpecial;
    }

    void SetState(State newState, float initialWaitedTime = 0.0f)
    {
        waitedTime = initialWaitedTime;
        state = newState;

        // Stop cancelling special.
        cancelSpecial = false;

        // Disable input if not idle.
        if (state != State.Idle)
        {
            inputEnabled = false;
        }

        switch (state)
        {
            case State.Moving:
                {
                    currentMove = moves[0];
                    break;
                }
            case State.CompletingMove:
                {
                    // Set speed and move delta.
                    speed = 0.0f;
                    moveDelta = new Vector3(0.0f, moveDelta.y, 0.0f);

                    break;
                }
            case State.Idle:
            case State.CompletingTurn:
                {
                    // Set speed, move delta and rotation speed.
                    speed = 0.0f;
                    moveDelta = new Vector3(0.0f, moveDelta.y, 0.0f);
                    rotateSpeed = 0.0f;

                    break;
                }
        }
    }

    void UpdateState()
    {
        if (currentTurnTarget != null)
        {
            SetState(State.Turning);
        }
        else if (moves.Count > 0)
        {
            SetState(State.Moving);
        }
        else
        {
            SetState(State.Idle);
        }
    }

    void CompleteMove()
    {
        // Remove move from queue.
        if (moves.Count > 0)
        {
            moves.RemoveAt(0);
        }

        // Do callbacks.
        currentMove.onComplete?.Invoke();

        UpdateState();
    }

    void CompleteTurn()
    {
        var completeFunc = currentTurnTarget.onComplete;
        currentTurnTarget = null;
       
        completeFunc?.Invoke();
        Debug.Log("Gewonnen");

        UpdateState();
    }


    #region Input Handling
    // Input ------------------------------------------------------------------------------------------


    private void OnMove(InputValue value)
    {
        _movement = value.Get<Vector2>();
    }

    private void OnMoveDpad(InputValue value)
    {
        Vector2 input = value.Get<Vector2>();
        // First value is movement on z-axis, second is whether to jump or not.
        input.Normalize();
        RightLeftJump(input);
    }

    public void RightLeftJump(Vector2 input) 
    {
        if (!inputEnabled)
            return;
        if (input[1]>0){
            //W
            // Check if player is jumping.
            if (!airborne || jumpsInAir > 0)
            {
                if (airborne)
                {
                    jumpsInAir--;
                    if (doubleJumpAudioClip)
                    {
                        audioSource.PlayOneShot(doubleJumpAudioClip);
                    }
                }
                else
                {
                    if (jumpAudioClip)
                    {
                        audioSource.PlayOneShot(jumpAudioClip);
                    }
                }

                moveDelta.y = jumpSpeed;
                animator.SetTrigger(jumpHash);

                airborne = true;
                airborneTime = coyoteDelay;
            }
        }else{
            //S
        }
        input[1]=0; // TODO keep player on line by setting this as difference in actual position an starting position, would be better to prevent x-movement in the first place
        _movement = input;
        
    }

    private void OnMenu()
    {
        print("OnMenu");
    }

    private void OnNorthPress()
    {
        print("OnNorthPress");
    }

    private void OnNorthRelease()
    {
        print("OnNorthRelease");
    }

    private void OnEastPress()
    {
        print("OnEastPress");
    }

    private void OnEastRelease()
    {
        print("OnEastRelease");
    }


    private void OnSouthPress()
    {
        Attack();
       
    }

    public void Attack()
    {
        if (!inputEnabled)
        {
            return;
        }
        if (!hasItem)
        {
            animator.SetTrigger(punchHash);
            castARay(strength, hitRange);
            audioSource.PlayOneShot(throwPunchAudioClip,0.1f);
           
        }
        else
        {
            if (itemType == "batarang" || itemType == "legosteinl")
            {
                animator.SetTrigger(throwHash);
                audioSource.PlayOneShot(boomerangAudioClip);
            }
            else if (itemType == "sword")
            {
                animator.SetTrigger(swordHash);
                if (UnityEngine.Random.Range(0, 2) > 0)
                    audioSource.PlayOneShot(LaserAudio1);
                else
                    audioSource.PlayOneShot(LaserAudio2);
            }
            else if (itemType == "knive")
            {
                animator.SetTrigger(swordHash);
            }
            else if (itemType == "baseballbatl")
            {
                animator.SetTrigger(swordHash);
                audioSource.PlayOneShot(batAudioClip);
                Invoke("usedItem", 0.5f);
            }
            else if (itemType == "ice_wand" || itemType == "hammerl")
            {
                animator.SetTrigger(swordHash);
                Invoke("usedItem", 0.5f);
            }
            else if (itemType == "gun")
            {
                animator.SetTrigger(shootHash);
                audioSource.PlayOneShot(smallGunAudioClip);
                Invoke("usedItem", 0.5f);
                Invoke("shoot", 0.5f);
            }
            else if (itemType == "gunl"){
                animator.SetTrigger(shootHash);
                audioSource.PlayOneShot(bigGunAudioClip);
                Invoke("usedItem", 0.5f);
                Invoke("shoot", 0.5f);
            }
        }
    }

    private void shoot()
    {
        castARay(item.strength, 1000.0f);
    }

    private void castARay(int damageToInduce, float rayDistance){
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit, rayDistance))
        {
            if(hit.collider.tag == "Player"){
                OurMinifigController hit_player = hit.collider.gameObject.GetComponent<OurMinifigController>();
                hit_player.damage += damageToInduce;
                Vector3 hit_direction = hit_player.transform.position - transform.position;
                hit_direction.x = 0f; // do not change x position
                hit_direction.y += 1f; // make the hit player fly slightly upwards
                if (hit_direction.z > 0)
                    hit_direction.z = 1f;
                else
                    hit_direction.z = -1f;
                hit_direction.Normalize();
                float dmg_scale = (hit_player.damage + 10) * 0.01f;
                hit_player._knockback += hit_direction * dmg_scale;
                audioSource.clip = receivePunchAudioClip;
                audioSource.PlayDelayed(0.1f);
                
            }
        }
    }

    private void OnSouthRelease()
    {
        //print("OnSouthRelease");
    }

    private void OnWestPress()
    {
        print("OnWestPress");
    }

    private void OnWestRelease()
    {
        print("OnWestRelease");
    }

    #endregion


    void OnCollisionEnter(Collision collision)
    {
        GameObject gameObj = collision.gameObject;
        if(gameObj.tag=="Item"){
            Item collidingItem = gameObj.GetComponent<Item>();
            if (collidingItem == item)
                return;
            if (collidingItem.isPickedUp && collidingItem.isActive) // item was already picked up by another player
            {
                damage += collidingItem.strength;
                Vector3 hit_direction = transform.position - collidingItem.transform.position;
                hit_direction.x = 0f; // do not change x position
                hit_direction.y = 1f; // fly slightly upwards
                if (hit_direction.z > 0)
                    hit_direction.z = 1f;
                else
                    hit_direction.z = -1f;
                hit_direction.Normalize();
                float dmg_scale = (damage + 10) * 0.01f;
                _knockback += hit_direction * dmg_scale;
                //handle ice_wand
                if (collidingItem.type == "ice_wand")
                {
                    freeze();
                }
                return; //getting hit by item handling?
            }
            if (!collidingItem.isPickedUp && hasItem==false)
            {
                // pick up item
                item = collidingItem;
                hasItem = true;
                itemType = item.type;
                // make item child of hand_R_loc
                var tree = new List<int>() { 0, 1, 0, 1, 0, 0, 0, 0, 0, 0, 2, 0, 0, 1, 0, 0, 2 };
                Transform child = transform;
                foreach (var subtree in tree)
                {
                    child = child.GetChild(subtree);
                }
                item.pickUp(child);
            }
        }
    }

    public void fix()
    {
        if (!hasItem)
            return;
        SetInputEnabled(false);
        float yrot = transform.rotation.eulerAngles.y;
        if (yrot < 90 || yrot > 270)
            transform.rotation = Quaternion.Euler(0, 0, 0);
        else
            transform.rotation = Quaternion.Euler(0, 180, 0);
        item.isActive = true;
    }

    public void release()
    {
        if (!hasItem)
            return;
        SetInputEnabled(true);
        item.isActive = false;
        usedItem();
    }

    public void setHitting(bool hitting)
    {
        if (!hasItem)
            return;
        if (hitting)
        {
            item.isActive = true;
        }
        else
        {
            item.isActive = false;
            usedItem();
        } 
    }

    private void usedItem()
    {
        bool keep = item.Used();
        if (!keep)
        {
            Destroy(item.gameObject);
            hasItem = false;
            item = null;
        }
    }

    public int getPlatform(){
        if(transform.position.z < -8.5) return 0;
        if(transform.position.z > 8.4) return 2;
        if(transform.position.y > 3) return 3;
        return 1;
    }

    private void freeze()
    {
        if (frozen)
        {
            return;
        }
        frozen = true;
        MeshRenderer iceBergMeshRenderer = iceBerg.GetComponent<MeshRenderer>();
        iceBergMeshRenderer.enabled = true;
        inputEnabled = false;
        animator.speed = 0;
        Invoke("unfreeze", 1);
    }

    private void unfreeze()
    {
        inputEnabled = true;
        animator.speed = 1;
        MeshRenderer iceBergMeshRenderer = iceBerg.GetComponent<MeshRenderer>();
        iceBergMeshRenderer.enabled = false;
        frozen = false;
    }
}