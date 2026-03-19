using System;
using UnityEngine;

namespace KOI.HorrorGameEngine
{
    /// <summary>
    /// Procedural head-bob system driven by <see cref="HeadBobProfile"/> assets.
    ///
    /// Attach to the CameraHolder — the parent pivot of the Camera GameObject.
    /// The component manipulates <c>localPosition</c> and <c>localRotation</c>
    /// of that object, keeping it decoupled from <see cref="MouseLook"/>.
    ///
    /// Recommended hierarchy:
    /// <code>
    ///   Player  (CharacterController + PlayerMovement)
    ///     CameraHolder   ← HeadBob lives here
    ///       Main Camera  (Camera + MouseLook)
    /// </code>
    ///
    /// Motion is defined by three <see cref="HeadBobProfile"/> assets — one each
    /// for idle, walk, and jump.  Each profile owns six AnimationCurves (posX/Y/Z
    /// and rotX/Y/Z) evaluated over a normalised phase [0, 1], giving artists full
    /// control over the waveform shape without touching code.
    ///
    /// The jump profile is a <em>one-shot</em> effect: it plays from phase 0 to 1
    /// once and then the system automatically returns to idle or walk.
    /// </summary>
    public class HeadBob : MonoBehaviour
    {
        // ------------------------------------------------------------------
        // Public state
        // ------------------------------------------------------------------

        public enum State { Idle, Walk, Jump }

        /// <summary>Current playback state.</summary>
        public State CurrentState { get; private set; } = State.Idle;

        // ------------------------------------------------------------------
        // Events
        // ------------------------------------------------------------------

        /// <summary>
        /// Fired each time a foot plants on the ground (walk profile only).
        /// Parameter is <c>true</c> for the left foot.
        /// Use this to drive footstep audio, particles, or screen-shake.
        /// </summary>
        public event Action<bool> OnFootstep;

        /// <summary>Fired when the jump one-shot profile finishes playing.</summary>
        public event Action OnJumpComplete;

        // ------------------------------------------------------------------
        // Inspector
        // ------------------------------------------------------------------

        [Header("References")]
        [Tooltip("Source of velocity and ground state.  " +
                 "Auto-resolved from parent objects if left empty.")]
        [SerializeField] private CharacterController _characterController;

        [Header("Profiles")]
        [Tooltip("Motion played while the player is standing still.")]
        [SerializeField] private HeadBobProfile _idleProfile;

        [Tooltip("Motion played while the player is walking.")]
        [SerializeField] private HeadBobProfile _walkProfile;

        [Tooltip("One-shot motion played when the player jumps or leaves the ground. " +
                 "Automatically returns to idle/walk on completion.")]
        [SerializeField] private HeadBobProfile _jumpProfile;

        [Header("Thresholds")]
        [Tooltip("Horizontal speed (m/s) at or above which the walk profile is used.")]
        [SerializeField] private float _walkThreshold = 0.5f;

        [Tooltip("When enabled the component watches the CharacterController's isGrounded " +
                 "state and calls TriggerJump() automatically on lift-off.")]
        [SerializeField] private bool _autoDetectJump = true;

        [Header("Output Smoothing")]
        [Tooltip("How tightly position and rotation track the sampled target.  " +
                 "Higher values are snappier; lower values feel floaty.  " +
                 "Recommended: 18–30.")]
        [SerializeField] [Min(1f)] private float _smoothingSpeed = 22f;

        // ------------------------------------------------------------------
        // Private state
        // ------------------------------------------------------------------

        // The base transform values — restored each frame before adding the bob offset.
        private Vector3 _baseLocalPosition;

        // A continuously advancing clock shared by idle and walk so that
        // switching between them never resets the phase mid-cycle.
        private float _continuousTime;

        // Jump uses its own elapsed timer so it always starts at phase 0.
        private bool  _inJump;
        private float _jumpElapsed;

        // Smoothed output applied to the transform.
        private Vector3    _currentPosOffset;
        private Quaternion _currentRotOffset = Quaternion.identity;

        // Footstep tracking — fires once at phase wrap and once at phase 0.5.
        private float _prevPhase;
        private bool  _wasLeftFoot;

        // Jump auto-detection.
        private bool _prevGrounded;

        // ------------------------------------------------------------------
        // Unity lifecycle
        // ------------------------------------------------------------------

        private void Start()
        {
            _baseLocalPosition = transform.localPosition;

            if (_characterController == null)
                _characterController = GetComponentInParent<CharacterController>();

            if (_characterController == null)
                Debug.LogWarning("[HeadBob] No CharacterController found — assign one in the Inspector.", this);
            else
                _prevGrounded = _characterController.isGrounded;
        }

        private void LateUpdate()
        {
            if (_characterController == null) return;

            _continuousTime += Time.deltaTime;

            DetectJump();
            AdvanceJump();

            var profile = ResolveProfile();

            if (profile == null)
            {
                // No profile assigned — smoothly return to base pose.
                _currentPosOffset = Vector3.Lerp(_currentPosOffset, Vector3.zero,
                    Time.deltaTime * _smoothingSpeed);
                _currentRotOffset = Quaternion.Slerp(_currentRotOffset, Quaternion.identity,
                    Time.deltaTime * _smoothingSpeed);
                transform.localPosition = _baseLocalPosition + _currentPosOffset;
                transform.localRotation = _currentRotOffset;
                return;
            }

            float phase = ComputePhase(profile);

            FireFootstepEvents(profile, phase);

            profile.Sample(phase, out var targetPos, out var targetEuler);
            var targetRot = Quaternion.Euler(targetEuler);

            float smooth = Time.deltaTime * _smoothingSpeed;
            _currentPosOffset = Vector3.Lerp(_currentPosOffset, targetPos, smooth);
            _currentRotOffset = Quaternion.Slerp(_currentRotOffset, targetRot, smooth);

            transform.localPosition = _baseLocalPosition + _currentPosOffset;
            transform.localRotation = _currentRotOffset;

            _prevPhase = phase;
        }

        // ------------------------------------------------------------------
        // Public API
        // ------------------------------------------------------------------

        /// <summary>
        /// Start playback of the jump profile from the beginning.
        /// Has no effect if the jump profile is not assigned.
        /// </summary>
        public void TriggerJump()
        {
            if (_jumpProfile == null) return;
            _inJump      = true;
            _jumpElapsed = 0f;
            _prevPhase   = 0f;
            CurrentState = State.Jump;
        }

        // ------------------------------------------------------------------
        // State helpers
        // ------------------------------------------------------------------

        private void DetectJump()
        {
            if (!_autoDetectJump) return;

            bool grounded = _characterController.isGrounded;

            // Lift-off: was grounded last frame, airborne this frame.
            if (_prevGrounded && !grounded && !_inJump)
                TriggerJump();

            _prevGrounded = grounded;
        }

        private void AdvanceJump()
        {
            if (!_inJump) return;

            _jumpElapsed += Time.deltaTime;

            // Check for jump completion.
            if (_jumpProfile != null && _jumpElapsed * _jumpProfile.frequency >= 1f)
            {
                _inJump = false;
                OnJumpComplete?.Invoke();
                UpdateWalkIdleState(); // resolve the new non-jump state immediately
            }
        }

        /// <summary>
        /// Updates <see cref="CurrentState"/> between Idle and Walk based on velocity.
        /// Called every LateUpdate when not in Jump, and once when Jump finishes.
        /// </summary>
        private void UpdateWalkIdleState()
        {
            var vel    = _characterController.velocity;
            var hSpeed = new Vector3(vel.x, 0f, vel.z).magnitude;
            bool isWalking = _characterController.isGrounded && hSpeed >= _walkThreshold;
            CurrentState = isWalking ? State.Walk : State.Idle;
        }

        // ------------------------------------------------------------------
        // Phase & profile resolution
        // ------------------------------------------------------------------

        private HeadBobProfile ResolveProfile()
        {
            if (!_inJump)
                UpdateWalkIdleState();

            return CurrentState switch
            {
                State.Idle => _idleProfile,
                State.Walk => _walkProfile,
                State.Jump => _jumpProfile,
                _          => null,
            };
        }

        /// <summary>
        /// Returns the normalised phase [0, 1] for the current profile.
        /// Looping profiles wrap; the jump one-shot is clamped.
        /// </summary>
        private float ComputePhase(HeadBobProfile profile)
        {
            if (_inJump)
            {
                // One-shot: phase 0 → 1, then stays at 1 until AdvanceJump resets it.
                return Mathf.Clamp01(_jumpElapsed * profile.frequency);
            }

            // Idle and Walk share _continuousTime so that switching between them
            // never produces a sudden phase jump.
            return (_continuousTime * profile.frequency) % 1f;
        }

        // ------------------------------------------------------------------
        // Footstep events
        // ------------------------------------------------------------------

        /// <summary>
        /// Fires <see cref="OnFootstep"/> at phase 0 (cycle wrap) and phase 0.5
        /// while the walk profile is active, corresponding to left and right foot plants.
        /// </summary>
        private void FireFootstepEvents(HeadBobProfile profile, float phase)
        {
            if (!profile.loop || CurrentState != State.Walk) return;

            // Phase wrap: previous phase was near 1, current is near 0.
            bool wrapped    = _prevPhase > 0.8f && phase < 0.2f;
            // Mid-cycle crossing: crossed the 0.5 boundary.
            bool crossedMid = _prevPhase < 0.5f && phase >= 0.5f;

            if (wrapped || crossedMid)
            {
                OnFootstep?.Invoke(_wasLeftFoot);
                _wasLeftFoot = !_wasLeftFoot;
            }
        }
    }
}
