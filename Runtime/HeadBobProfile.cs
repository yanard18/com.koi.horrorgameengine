using UnityEngine;

namespace KOI.HorrorGameEngine
{
    /// <summary>
    /// A reusable data asset that defines one head-bob motion profile.
    ///
    /// Each of the six AnimationCurves is evaluated over a normalised phase [0, 1].
    /// For looping profiles the phase wraps continuously; for one-shot profiles (e.g. jump)
    /// it is clamped and the system returns to idle/walk once it reaches 1.
    ///
    /// Curve output is dimensionless — final metres / degrees are set by the
    /// <see cref="positionAmplitude"/> and <see cref="rotationAmplitude"/> multipliers.
    ///
    /// Create via: Assets → Create → KOI → Head Bob Profile
    /// </summary>
    [CreateAssetMenu(fileName = "HeadBobProfile", menuName = "KOI/Head Bob Profile", order = 10)]
    public class HeadBobProfile : ScriptableObject
    {
        // ------------------------------------------------------------------
        // Playback
        // ------------------------------------------------------------------

        [Header("Identity")]
        [Tooltip("Name used by HeadBob.SetState() to identify this profile.")]
        public string stateName = "Idle";

        [Header("Playback")]
        [Tooltip("Cycles per second.\n" +
                 "Breathing ≈ 0.25 | Walking ≈ 1.6 | Running ≈ 2.6")]
        [Min(0.01f)]
        public float frequency = 1.6f;

        [Tooltip("When true the profile loops continuously. " +
                 "Disable for one-shot effects such as jump/land impacts.")]
        public bool loop = true;

        [Tooltip("Seconds to blend from the previous state into this one. 0 = instant.")]
        [Min(0f)]
        public float transitionDuration = 0.15f;

        // ------------------------------------------------------------------
        // Position curves
        // ------------------------------------------------------------------

        [Header("Position Curves  (output × positionAmplitude = metres)")]
        [Tooltip("Lateral (X) sway — left/right.  " +
                 "One full sine cycle produces one stride sway.")]
        public AnimationCurve posX = AnimationCurve.Constant(0f, 1f, 0f);

        [Tooltip("Vertical (Y) bob — up/down.  " +
                 "A double-frequency sine produces two bobs per stride (one per step).")]
        public AnimationCurve posY = AnimationCurve.Constant(0f, 1f, 0f);

        [Tooltip("Forward (Z) dip.  " +
                 "Negative values push the head slightly forward at each foot plant.  " +
                 "A negative-abs-sine is the classic choice.")]
        public AnimationCurve posZ = AnimationCurve.Constant(0f, 1f, 0f);

        // ------------------------------------------------------------------
        // Rotation curves
        // ------------------------------------------------------------------

        [Header("Rotation Curves  (output × rotationAmplitude = degrees)")]
        [Tooltip("Pitch (X) — nod up/down.  Usually follows posY.")]
        public AnimationCurve rotX = AnimationCurve.Constant(0f, 1f, 0f);

        [Tooltip("Yaw (Y) — look left/right.  Usually near-zero for head bob.")]
        public AnimationCurve rotY = AnimationCurve.Constant(0f, 1f, 0f);

        [Tooltip("Roll (Z) — tilt left/right.  Usually mirrors posX (lean into each step).")]
        public AnimationCurve rotZ = AnimationCurve.Constant(0f, 1f, 0f);

        // ------------------------------------------------------------------
        // Amplitude
        // ------------------------------------------------------------------

        [Header("Amplitude")]
        [Min(0f)]
        [Tooltip("Global scale applied to all position curves (metres).")]
        public float positionAmplitude = 0.04f;

        [Min(0f)]
        [Tooltip("Global scale applied to all rotation curves (degrees).")]
        public float rotationAmplitude = 1.0f;

        // ------------------------------------------------------------------
        // Sampling
        // ------------------------------------------------------------------

        /// <summary>
        /// Returns the position offset and Euler-angle offset for the given
        /// normalised <paramref name="phase"/> in [0, 1].
        /// </summary>
        public void Sample(float phase, out Vector3 position, out Vector3 eulerAngles)
        {
            float px = posX != null ? posX.Evaluate(phase) : 0f;
            float py = posY != null ? posY.Evaluate(phase) : 0f;
            float pz = posZ != null ? posZ.Evaluate(phase) : 0f;
            position = new Vector3(px, py, pz) * positionAmplitude;

            float rx = rotX != null ? rotX.Evaluate(phase) : 0f;
            float ry = rotY != null ? rotY.Evaluate(phase) : 0f;
            float rz = rotZ != null ? rotZ.Evaluate(phase) : 0f;
            eulerAngles = new Vector3(rx, ry, rz) * rotationAmplitude;
        }

        // ------------------------------------------------------------------
        // Default curve helpers (called from Reset so the asset has sensible
        // values the moment it is created via the CreateAssetMenu)
        // ------------------------------------------------------------------

        private void Reset()
        {
            // Walking defaults.  Designers can override every curve freely.
            frequency          = 1.6f;
            loop               = true;
            positionAmplitude  = 0.04f;
            rotationAmplitude  = 1.2f;

            posX = BuildSine(1, 8);          // single sway, ±1
            posY = BuildSine(2, 16);         // double bob, ±1
            posZ = BuildNegativeAbsSine(2, 16); // forward dip, 0 to −1
            rotX = BuildSine(2, 16);         // pitch follows posY
            rotY = BuildFlat();              // no yaw
            rotZ = BuildSine(1, 8);          // roll follows posX
        }

        // ------------------------------------------------------------------
        // Curve factories
        // ------------------------------------------------------------------

        /// <summary>
        /// Sine with <paramref name="cycles"/> full oscillations over [0, 1].
        /// </summary>
        public static AnimationCurve BuildSine(int cycles = 1, int samplesPerCycle = 8)
        {
            int total = cycles * samplesPerCycle;
            var curve = new AnimationCurve();
            for (int i = 0; i <= total; i++)
            {
                float t = (float)i / total;
                curve.AddKey(t, Mathf.Sin(t * Mathf.PI * 2f * cycles));
            }
            SmoothAll(curve);
            return curve;
        }

        /// <summary>
        /// Negative absolute-sine with <paramref name="cycles"/> oscillations over [0, 1].
        /// Useful for forward-dip: the curve stays ≤ 0, dipping at each foot plant.
        /// </summary>
        public static AnimationCurve BuildNegativeAbsSine(int cycles = 2, int samplesPerCycle = 8)
        {
            int total = cycles * samplesPerCycle;
            var curve = new AnimationCurve();
            for (int i = 0; i <= total; i++)
            {
                float t = (float)i / total;
                curve.AddKey(t, -Mathf.Abs(Mathf.Sin(t * Mathf.PI * 2f * cycles)));
            }
            SmoothAll(curve);
            return curve;
        }

        /// <summary>Zero curve (flat at 0).</summary>
        public static AnimationCurve BuildFlat() =>
            AnimationCurve.Constant(0f, 1f, 0f);

        /// <summary>
        /// A simple impact curve for one-shot effects: rises quickly then settles.
        /// Suitable for jump or land profiles.
        /// </summary>
        public static AnimationCurve BuildImpact()
        {
            var curve = new AnimationCurve(
                new Keyframe(0.00f,  0.00f),
                new Keyframe(0.15f,  1.00f),
                new Keyframe(0.40f, -0.30f),
                new Keyframe(0.65f,  0.10f),
                new Keyframe(1.00f,  0.00f));
            SmoothAll(curve);
            return curve;
        }

        private static void SmoothAll(AnimationCurve curve)
        {
            for (int i = 0; i < curve.length; i++)
                curve.SmoothTangents(i, 0f);
        }
    }
}
