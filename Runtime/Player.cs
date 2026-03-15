using UnityEngine;

namespace KOI.HorrorGameEngine
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        private const float TIMER_INACTIVE   = -1f;
        private const float GROUNDED_GRAVITY = -2f;   // small constant to keep CC grounded

        [Header("Movement Settings")]
        [SerializeField] private float _maxSpeed            = 12f;
        [SerializeField] private float _groundAcceleration  = 50f;
        [SerializeField] private float _groundDeceleration  = 50f;

        [Header("Air Settings")]
        [SerializeField] private float _maxAirSpeed         = 12f;
        [SerializeField] private float _airAcceleration     = 20f;
        [SerializeField] private float _airDeceleration     = 5f;

        [Header("Jump Settings")]
        [SerializeField] private float _jumpHeight          = 3f;
        [SerializeField] private float _gravity             = -19.62f;
        [SerializeField] private float _groundCoyoteTime    = 0.15f;
        [SerializeField] private float _jumpBufferTime      = 0.1f;

        private enum MovementState { Grounded, Airborne }

        private MovementState _state = MovementState.Airborne;

        private CharacterController _controller;

        // Horizontal locomotion (XZ), driven by input
        private Vector3 _moveVelocity;

        // Vertical velocity (Y), driven by gravity / jump
        private float _verticalVelocity;

        // Cached per-frame so all methods agree on one value
        private bool _isGrounded;

        // Timers
        private float _lastGroundedTime     = TIMER_INACTIVE;
        private float _lastJumpPressedTime  = TIMER_INACTIVE;

        private Vector2 _rawInput;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
        }

        private void Update()
        {
            ReadInput();

            _isGrounded = _controller.isGrounded;

            // Tick coyote / buffer timers
            UpdateTimers();

            // 4. Transition state machine
            UpdateState();

            // 5. Apply horizontal movement (input → velocity)
            UpdateMoveVelocity();

            // 6. Apply gravity, then jump impulse (order matters — see note)
            UpdateVerticalVelocity();

            // 7. Commit to CharacterController
            var finalVelocity = _moveVelocity + Vector3.up * _verticalVelocity;
            _controller.Move(finalVelocity * Time.deltaTime);
        }

        // ─────────────────────────────────────────────
        //  Input
        // ─────────────────────────────────────────────
        /// <summary>
        /// Reads raw input into plain data fields.
        /// Swap this out for InputSystem when you migrate.
        /// </summary>
        private void ReadInput()
        {
            _rawInput = new Vector2(
                Input.GetAxisRaw("Horizontal"),
                Input.GetAxisRaw("Vertical")
            );

            if (Input.GetButtonDown("Jump"))
            {
                _lastJumpPressedTime = Time.time;
            }
        }

        // ─────────────────────────────────────────────
        //  Timers
        // ─────────────────────────────────────────────
        private void UpdateTimers()
        {
            if (_isGrounded)
            {
                _lastGroundedTime = Time.time;
            }
        }

        // ─────────────────────────────────────────────
        //  State machine
        // ─────────────────────────────────────────────
        private void UpdateState()
        {
            _state = _isGrounded ? MovementState.Grounded : MovementState.Airborne;
        }

        // ─────────────────────────────────────────────
        //  Horizontal movement
        // ─────────────────────────────────────────────
        private void UpdateMoveVelocity()
        {
            bool  onGround   = _state == MovementState.Grounded;
            float accel      = onGround ? _groundAcceleration : _airAcceleration;
            float decel      = onGround ? _groundDeceleration : _airDeceleration;
            float maxSpeed   = onGround ? _maxSpeed           : _maxAirSpeed;

            var inputDirection = new Vector3(_rawInput.x, 0f, _rawInput.y);

            // Normalise only when length > 1 to preserve analogue stick feel
            if (inputDirection.sqrMagnitude > 1f)
            {
                inputDirection.Normalize();
            }

            // World-space direction relative to character orientation
            var worldDirection = transform.TransformDirection(inputDirection);
            worldDirection.y = 0f; // strip any vertical component from the transform

            if (worldDirection.sqrMagnitude > 0f)
            {
                // Project onto slope so movement doesn't fight gravity on ramps
                var targetVelocity = ProjectOntoSlope(worldDirection) * maxSpeed;
                _moveVelocity = Vector3.MoveTowards(
                    _moveVelocity, targetVelocity, accel * Time.deltaTime);
            }
            else
            {
                _moveVelocity = Vector3.MoveTowards(
                    _moveVelocity, Vector3.zero, decel * Time.deltaTime);
            }
        }

        /// <summary>
        /// Projects direction onto the surface below if grounded, so the player
        /// slides smoothly on slopes rather than hovering or stuttering.
        /// Falls back to the flat direction when airborne or no hit.
        /// </summary>
        private Vector3 ProjectOntoSlope(Vector3 direction)
        {
            if (_state != MovementState.Grounded) return direction;

            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit,
                                _controller.height * 0.5f + 0.3f))
            {
                return Vector3.ProjectOnPlane(direction, hit.normal).normalized;
            }

            return direction;
        }

        /// <summary>
        /// Gravity is applied FIRST, then the jump check follows.
        /// This means:
        ///   - On a normal frame:    gravity accumulates continuously.
        ///   - On a jump frame:      gravity runs, then the impulse overwrites _verticalVelocity,
        ///                           so we don't double-subtract gravity from the impulse.
        ///   - On grounded frames:   we clamp to GROUNDED_GRAVITY before gravity runs,
        ///                           so the CC stays properly grounded.
        /// </summary>
        private void UpdateVerticalVelocity()
        {
            // Clamp downward velocity when grounded so it doesn't accumulate
            if (_isGrounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = GROUNDED_GRAVITY;
            }

            // Apply gravity this frame (before jump, so impulse isn't immediately reduced)
            _verticalVelocity += _gravity * Time.deltaTime;

            // Jump: both coyote windows must be open
            if (CanJump())
            {
                // Derive impulse from desired apex height: v = sqrt(h * -2g)
                _verticalVelocity = Mathf.Sqrt(_jumpHeight * -2f * _gravity);

                // Consume both timers so we can't jump again until re-triggered
                _lastJumpPressedTime = TIMER_INACTIVE;
                _lastGroundedTime    = TIMER_INACTIVE;
            }
        }

        /// <summary>
        /// Returns true when both coyote windows are valid.
        /// Encapsulated here so the condition has a single authoritative home.
        /// </summary>
        private bool CanJump()
        {
            bool groundCoyote = (Time.time - _lastGroundedTime)    <= _groundCoyoteTime;
            bool jumpBuffer   = (Time.time - _lastJumpPressedTime) <= _jumpBufferTime;
            return groundCoyote && jumpBuffer;
        }
    }
}