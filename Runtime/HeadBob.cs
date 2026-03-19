using UnityEngine;

namespace KOI.HorrorGameEngine
{
    /// <summary>
    /// Procedural head-bob driven by <see cref="HeadBobProfile"/> assets.
    ///
    /// Attach to the CameraHolder (parent pivot of the Camera).
    /// Writes localPosition and localRotation; does not conflict with MouseLook.
    ///
    ///   Player  (CharacterController + PlayerMovement)
    ///     CameraHolder   ← HeadBob here
    ///       Main Camera  (Camera + MouseLook)
    /// </summary>
    public class HeadBob : MonoBehaviour
    {
        public enum State { Idle, Walk, Jump }
        public State CurrentState { get; private set; } = State.Idle;

        // ── Inspector ─────────────────────────────────────────────────────

        [Header("References")]
        [SerializeField] private CharacterController _characterController;

        [Header("Profiles")]
        [SerializeField] private HeadBobProfile _idleProfile;
        [SerializeField] private HeadBobProfile _walkProfile;
        [SerializeField] private HeadBobProfile _jumpProfile;

        [Header("Settings")]
        [Tooltip("Horizontal speed (m/s) required to switch to the walk profile.")]
        [SerializeField] private float _walkThreshold = 0.5f;

        [Tooltip("Automatically trigger the jump profile when the player leaves the ground.")]
        [SerializeField] private bool _autoDetectJump = true;

        [Tooltip("Output smoothing. Higher = snappier. Recommended: 18–30.")]
        [SerializeField] [Min(1f)] private float _smoothingSpeed = 22f;

        // ── Private state ─────────────────────────────────────────────────

        private Vector3    _baseLocalPosition;
        private Vector3    _currentPosOffset;
        private Quaternion _currentRotOffset = Quaternion.identity;

        // Shared clock for idle/walk — never resets, so switching between them
        // doesn't restart the animation mid-cycle.
        private float _continuousTime;

        // Jump is one-shot and always starts at phase 0.
        private bool  _inJump;
        private float _jumpElapsed;

        private bool _prevGrounded;

        // ── Unity ─────────────────────────────────────────────────────────

        private void Start()
        {
            _baseLocalPosition = transform.localPosition;

            if (_characterController == null)
                _characterController = GetComponentInParent<CharacterController>();

            if (_characterController == null)
                Debug.LogWarning("[HeadBob] No CharacterController found.", this);
            else
                _prevGrounded = _characterController.isGrounded;
        }

        private void LateUpdate()
        {
            if (_characterController == null) return;

            _continuousTime += Time.deltaTime;

            UpdateState();

            var profile = ActiveProfile;

            // When profile is null smoothly return to the base pose.
            var targetPos = Vector3.zero;
            var targetRot = Quaternion.identity;

            if (profile != null)
            {
                float phase = _inJump
                    ? Mathf.Clamp01(_jumpElapsed * profile.frequency)
                    : (_continuousTime * profile.frequency) % 1f;

                profile.Sample(phase, out targetPos, out var euler);
                targetRot = Quaternion.Euler(euler);
            }

            float smooth = Time.deltaTime * _smoothingSpeed;
            _currentPosOffset = Vector3.Lerp(_currentPosOffset, targetPos, smooth);
            _currentRotOffset = Quaternion.Slerp(_currentRotOffset, targetRot, smooth);

            transform.localPosition = _baseLocalPosition + _currentPosOffset;
            transform.localRotation = _currentRotOffset;
        }

        // ── Public API ────────────────────────────────────────────────────

        /// <summary>Manually trigger the jump one-shot profile.</summary>
        public void TriggerJump()
        {
            if (_jumpProfile == null) return;
            _inJump      = true;
            _jumpElapsed = 0f;
            CurrentState = State.Jump;
        }

        // ── Private ───────────────────────────────────────────────────────

        private void UpdateState()
        {
            bool grounded = _characterController.isGrounded;

            if (_inJump)
            {
                _jumpElapsed += Time.deltaTime;
                if (_jumpElapsed * _jumpProfile.frequency >= 1f)
                    _inJump = false;
            }
            else
            {
                if (_autoDetectJump && _prevGrounded && !grounded)
                    TriggerJump();
            }

            _prevGrounded = grounded;

            if (!_inJump)
            {
                float hSpeed = new Vector3(
                    _characterController.velocity.x, 0f, _characterController.velocity.z).magnitude;

                CurrentState = (grounded && hSpeed >= _walkThreshold) ? State.Walk : State.Idle;
            }
        }

        private HeadBobProfile ActiveProfile => CurrentState switch
        {
            State.Idle => _idleProfile,
            State.Walk => _walkProfile,
            State.Jump => _jumpProfile,
            _          => null,
        };
    }
}
