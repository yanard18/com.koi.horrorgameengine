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
        /// The string parameter is the name of the completed state.
        /// </summary>
        public event Action<string> OnStateComplete;

        // ── Public state ──────────────────────────────────────────────────

        public string CurrentState { get; private set; }

        // ── Private ───────────────────────────────────────────────────────

        private Vector3    _baseLocalPosition;
        private Vector3    _currentPosOffset;
        private Quaternion _currentRotOffset = Quaternion.identity;

        // Active profile — always starts its timer from 0 on state entry.
        private HeadBobProfile _activeProfile;
        private float          _stateTime;
        private bool           _isOneshot;

        // Previous profile — kept playing during the crossfade so there is no
        // frozen snapshot; both animations run live and blend between them.
        private HeadBobProfile _prevProfile;
        private float          _prevStateTime;

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
            _stateTime += Time.deltaTime;

            if (_prevProfile != null)
                _prevStateTime += Time.deltaTime;

            // One-shot completion.
            if (_isOneshot && _activeProfile != null && _stateTime * _activeProfile.frequency >= 1f)
            {
                var completedState = CurrentState;
                _isOneshot     = false;
                _activeProfile = null;
                OnStateComplete?.Invoke(completedState);
            }

            // Sample active profile (or base pose when null).
            SampleProfile(_activeProfile, _stateTime, _isOneshot, out var targetPos, out var targetRot);

            // Crossfade: blend the previous profile (still playing) into the new one.
            if (_blendT < 1f)
            {
                _blendT = _blendDuration > 0f
                    ? Mathf.MoveTowards(_blendT, 1f, Time.deltaTime / _blendDuration)
                    : 1f;

                SampleProfile(_prevProfile, _prevStateTime, false, out var fromPos, out var fromRot);

                targetPos = Vector3.Lerp(fromPos, targetPos, _blendT);
                targetRot = Quaternion.Slerp(fromRot, targetRot, _blendT);

                if (_blendT >= 1f)
                    _prevProfile = null;
            }

            // Inertial smoothing — gives the bob a sense of weight.
            float smooth = Time.deltaTime * _smoothingSpeed;
            _currentPosOffset = Vector3.Lerp(_currentPosOffset, targetPos, smooth);
            _currentRotOffset = Quaternion.Slerp(_currentRotOffset, targetRot, smooth);

            transform.localPosition = _baseLocalPosition + _currentPosOffset;
            transform.localRotation = _currentRotOffset;
        }

        // ── Public API ────────────────────────────────────────────────────

        /// <summary>
        /// Switch to the named state. The new animation always starts from the
        /// beginning. A crossfade blends the outgoing animation into the new one.
        /// Looping profiles already active are not restarted — safe to call every frame.
        /// </summary>
        public void SetState(string stateName)
        {
            // Don't restart an already-active looping state.
            if (CurrentState == stateName && _activeProfile != null && _activeProfile.loop)
                return;

            var profile = _profiles.Find(p => p != null && p.stateName == stateName);
            if (profile == null)
            {
                Debug.LogWarning($"[HeadBob] Profile '{stateName}' not found.", this);
                return;
            }

            // Hand off the current profile to the crossfade source.
            _prevProfile   = _activeProfile;
            _prevStateTime = _stateTime;
            _blendT        = 0f;
            _blendDuration = profile.transitionDuration;

            // New state always starts from the beginning.
            CurrentState   = stateName;
            _activeProfile = profile;
            _isOneshot     = !profile.loop;
            _stateTime     = 0f;
        }

        // ── Helpers ───────────────────────────────────────────────────────

        private static void SampleProfile(
            HeadBobProfile profile, float time, bool oneshot,
            out Vector3 position, out Quaternion rotation)
        {
            if (profile == null)
            {
                position = Vector3.zero;
                rotation = Quaternion.identity;
                return;
            }

            float phase = oneshot
                ? Mathf.Clamp01(time * profile.frequency)
                : (time * profile.frequency) % 1f;

            profile.Sample(phase, out position, out var euler);
            rotation = Quaternion.Euler(euler);
        }
    }
}
