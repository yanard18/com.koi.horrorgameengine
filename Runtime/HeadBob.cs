using System;
using System.Collections.Generic;
using UnityEngine;

namespace KOI.HorrorGameEngine
{
    /// <summary>
    /// Procedural head-bob driven by <see cref="HeadBobProfile"/> assets.
    ///
    /// Attach to the CameraHolder (parent pivot of the Camera).
    /// Writes localPosition and localRotation only — does not conflict with MouseLook.
    ///
    ///   Player  (CharacterController + PlayerMovement)
    ///     CameraHolder   ← HeadBob here
    ///       Main Camera  (Camera + MouseLook)
    ///
    /// Drop any number of HeadBobProfile assets into the Profiles list.
    /// Each profile carries its own stateName. Switch between them by calling
    /// <see cref="SetState"/> from any external script.
    /// </summary>
    public class HeadBob : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────

        [Tooltip("All available profiles. Each profile's stateName is used by SetState().")]
        [SerializeField] private List<HeadBobProfile> _profiles = new();

        [Tooltip("State activated on Start. Must match one of the names above.")]
        [SerializeField] private string _defaultState;

        [Tooltip("Output smoothing. Higher = snappier. Recommended: 18–30.")]
        [SerializeField] [Min(1f)] private float _smoothingSpeed = 22f;

        // ── Events ────────────────────────────────────────────────────────

        /// <summary>
        /// Fired when a one-shot profile (loop = false) finishes playing.
        /// Parameter is the name of the completed state.
        /// </summary>
        public event Action<string> OnStateComplete;

        // ── Public state ──────────────────────────────────────────────────

        public string CurrentState { get; private set; }

        // ── Private ───────────────────────────────────────────────────────

        private Vector3    _baseLocalPosition;
        private Vector3    _currentPosOffset;
        private Quaternion _currentRotOffset = Quaternion.identity;

        // Incoming state — timer always starts at 0 on SetState.
        private HeadBobProfile _to;
        private float          _toTime;

        // Outgoing state — keeps playing during the crossfade.
        private HeadBobProfile _from;
        private float          _fromTime;

        // Crossfade
        private float _blendT;
        private float _blendDuration;

        // ── Unity ─────────────────────────────────────────────────────────

        private void Start()
        {
            _baseLocalPosition = transform.localPosition;

            if (!string.IsNullOrEmpty(_defaultState))
                SetState(_defaultState);
        }

        private void LateUpdate()
        {
            // Advance both timers independently.
            _toTime   += Time.deltaTime;
            _fromTime += Time.deltaTime;

            // One-shot completion.
            if (_to != null && !_to.loop && _toTime * _to.frequency >= 1f)
            {
                var completed = CurrentState;
                _to          = null;
                _from        = null;
                _blendT      = 1f;
                CurrentState = null;
                OnStateComplete?.Invoke(completed);
            }

            // Sample the incoming profile.
            Sample(_to, _toTime, out var targetPos, out var targetRot);

            // Crossfade: blend the outgoing profile (still playing) into the incoming.
            if (_blendT < 1f)
            {
                _blendT = Mathf.MoveTowards(_blendT, 1f, Time.deltaTime / _blendDuration);

                Sample(_from, _fromTime, out var fromPos, out var fromRot);
                targetPos = Vector3.Lerp(fromPos, targetPos, _blendT);
                targetRot = Quaternion.Slerp(fromRot, targetRot, _blendT);

                if (_blendT >= 1f)
                    _from = null;
            }

            // Inertial smoothing.
            float smooth      = Time.deltaTime * _smoothingSpeed;
            _currentPosOffset = Vector3.Lerp(_currentPosOffset, targetPos, smooth);
            _currentRotOffset = Quaternion.Slerp(_currentRotOffset, targetRot, smooth);

            transform.localPosition = _baseLocalPosition + _currentPosOffset;
            transform.localRotation = _currentRotOffset;
        }

        // ── Public API ────────────────────────────────────────────────────

        /// <summary>
        /// Switch to the named state. The new animation always starts from phase 0.
        /// A crossfade blends the outgoing animation into the new one.
        /// Looping profiles already active are not restarted — safe to call every frame.
        /// </summary>
        public void SetState(string stateName)
        {
            if (CurrentState == stateName && _to != null && _to.loop)
                return;

            var profile = _profiles.Find(p => p != null && p.stateName == stateName);
            if (profile == null)
            {
                Debug.LogWarning($"[HeadBob] Profile '{stateName}' not found.", this);
                return;
            }

            // Outgoing profile keeps playing from where it was.
            _from     = _to;
            _fromTime = _toTime;

            // Incoming profile always starts from 0.
            _to          = profile;
            _toTime      = 0f;
            CurrentState = stateName;

            // Start crossfade. If duration is 0, skip blend entirely.
            _blendDuration = profile.transitionDuration;
            _blendT        = profile.transitionDuration > 0f ? 0f : 1f;
        }

        // ── Helpers ───────────────────────────────────────────────────────

        /// <summary>Samples a profile at the correct phase for its type. Null profile returns base pose.</summary>
        private static void Sample(HeadBobProfile profile, float time,
            out Vector3 position, out Quaternion rotation)
        {
            if (profile == null)
            {
                position = Vector3.zero;
                rotation = Quaternion.identity;
                return;
            }

            float raw   = time * profile.frequency;
            float phase = profile.loop ? raw % 1f : Mathf.Clamp01(raw);

            profile.Sample(phase, out position, out var euler);
            rotation = Quaternion.Euler(euler);
        }
    }
}
