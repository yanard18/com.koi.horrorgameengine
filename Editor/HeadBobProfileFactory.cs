using UnityEditor;
using UnityEngine;

namespace KOI.HorrorGameEngine.Editor
{
    /// <summary>
    /// Generates the three example <see cref="HeadBobProfile"/> assets
    /// (Idle, Walk, Jump) and saves them into the folder that is selected in
    /// the Project window, or into <c>Assets/HeadBobProfiles/</c> if nothing
    /// is selected.
    ///
    /// Invoke via: <b>Assets → Create → KOI → Head Bob Example Profiles</b>
    /// </summary>
    internal static class HeadBobProfileFactory
    {
        [MenuItem("Assets/Create/KOI/Head Bob Example Profiles", priority = 11)]
        private static void CreateExampleProfiles()
        {
            string folder = GetSelectedFolder();

            CreateIdle(folder);
            CreateWalk(folder);
            CreateJump(folder);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[HeadBob] Created example profiles in '{folder}'.");
        }

        // ------------------------------------------------------------------
        // Profile builders
        // ------------------------------------------------------------------

        /// <summary>
        /// Breathing / idle profile — very slow, barely perceptible motion that
        /// simulates the camera rising and falling with each breath.
        /// </summary>
        private static void CreateIdle(string folder)
        {
            var p = ScriptableObject.CreateInstance<HeadBobProfile>();

            p.frequency         = 0.25f;   // one breath every four seconds
            p.loop              = true;
            p.positionAmplitude = 0.003f;  // 3 mm max displacement
            p.rotationAmplitude = 0.20f;   // 0.2° max tilt

            // Y: single gentle sine — head rises on inhale, falls on exhale
            p.posY = HeadBobProfile.BuildSine(1, 12);

            // X: very slight sway — barely noticeable
            p.posX = ScaledSine(1, 8, 0.4f);

            // Z: minimal forward movement on exhale
            p.posZ = HeadBobProfile.BuildNegativeAbsSine(1, 8);

            // Pitch: tiny nod following the vertical bob
            p.rotX = ScaledSine(1, 12, 0.5f);

            // Yaw: none
            p.rotY = HeadBobProfile.BuildFlat();

            // Roll: barely perceptible lean matching the X sway
            p.rotZ = ScaledSine(1, 8, 0.3f);

            SaveAsset(p, folder, "HBP_Idle");
        }

        /// <summary>
        /// Walking profile — biomechanically-correct figure-8 motion.
        /// X sways once per stride; Y bobs twice (one per step); Z dips at each foot plant.
        /// </summary>
        private static void CreateWalk(string folder)
        {
            var p = ScriptableObject.CreateInstance<HeadBobProfile>();

            p.frequency         = 1.6f;    // 1.6 strides/s — comfortable walking pace
            p.loop              = true;
            p.positionAmplitude = 0.04f;   // 4 cm max displacement
            p.rotationAmplitude = 1.2f;    // 1.2° max tilt

            // X: single sine — head sways left then right once per stride
            p.posX = HeadBobProfile.BuildSine(1, 8);

            // Y: double sine — head bobs up–down twice per stride (one per foot)
            p.posY = HeadBobProfile.BuildSine(2, 16);

            // Z: negative abs-sine — forward dip at each foot plant
            p.posZ = HeadBobProfile.BuildNegativeAbsSine(2, 16);

            // Pitch: nod follows Y bob
            p.rotX = ScaledSine(2, 16, 0.6f);

            // Yaw: none
            p.rotY = HeadBobProfile.BuildFlat();

            // Roll: lean into each step, mirrors X sway
            p.rotZ = HeadBobProfile.BuildSine(1, 8);

            SaveAsset(p, folder, "HBP_Walk");
        }

        /// <summary>
        /// Jump one-shot profile — plays once from phase 0 to 1 and then the
        /// system automatically returns to idle or walk.
        ///
        /// Shape (posY):
        ///   t=0.00  takeoff — slight upward lurch
        ///   t=0.40  apex    — near-zero movement (weightlessness)
        ///   t=0.65  impact  — strong downward dip
        ///   t=0.80  bounce  — small recovery pop
        ///   t=1.00  settle  — returns to zero
        ///
        /// At frequency = 0.9 Hz the full animation takes ~1.11 seconds.
        /// </summary>
        private static void CreateJump(string folder)
        {
            var p = ScriptableObject.CreateInstance<HeadBobProfile>();

            p.frequency         = 0.9f;    // ~1.1 s total duration
            p.loop              = false;   // ONE-SHOT — critical!
            p.positionAmplitude = 0.06f;   // 6 cm max — exaggerated for feel
            p.rotationAmplitude = 2.0f;    // 2° max tilt

            // Y: takeoff lurch → weightless float → hard landing → small bounce → settle
            p.posY = BuildCurve(
                (0.00f,  0.00f),
                (0.10f,  0.50f),   // takeoff lurch — camera pops up
                (0.38f,  0.10f),   // apex / weightlessness
                (0.60f,  0.05f),   // beginning of fall
                (0.68f, -1.00f),   // IMPACT — strong downward dip
                (0.80f,  0.25f),   // bounce-back
                (0.90f, -0.05f),   // secondary micro-dip
                (1.00f,  0.00f)    // settle
            );

            // X: slight outward sway at landing to add physicality
            p.posX = BuildCurve(
                (0.00f,  0.00f),
                (0.65f,  0.20f),   // small sideways jolt at impact
                (0.85f, -0.10f),
                (1.00f,  0.00f)
            );

            // Z: head lurches forward at takeoff and dips on landing
            p.posZ = BuildCurve(
                (0.00f,  0.00f),
                (0.12f, -0.40f),   // lean forward at takeoff
                (0.40f,  0.00f),
                (0.68f,  0.60f),   // pushed back on impact
                (1.00f,  0.00f)
            );

            // Pitch (rotX): look slightly up during jump, snap down at landing
            p.rotX = BuildCurve(
                (0.00f,  0.00f),
                (0.15f, -0.50f),   // slight upward look at takeoff
                (0.40f,  0.00f),
                (0.68f,  1.00f),   // snap forward / downward on impact
                (0.85f, -0.20f),
                (1.00f,  0.00f)
            );

            // Yaw: none
            p.rotY = HeadBobProfile.BuildFlat();

            // Roll (rotZ): brief tilt at impact
            p.rotZ = BuildCurve(
                (0.00f,  0.00f),
                (0.68f,  0.30f),
                (0.85f, -0.10f),
                (1.00f,  0.00f)
            );

            SaveAsset(p, folder, "HBP_Jump");
        }

        // ------------------------------------------------------------------
        // Curve helpers
        // ------------------------------------------------------------------

        /// <summary>
        /// Returns a sine curve scaled so that peak value equals
        /// <paramref name="scale"/> instead of 1.
        /// </summary>
        private static AnimationCurve ScaledSine(int cycles, int samplesPerCycle, float scale)
        {
            int total = cycles * samplesPerCycle;
            var curve = new AnimationCurve();
            for (int i = 0; i <= total; i++)
            {
                float t = (float)i / total;
                curve.AddKey(t, Mathf.Sin(t * Mathf.PI * 2f * cycles) * scale);
            }
            SmoothAll(curve);
            return curve;
        }

        /// <summary>
        /// Builds an AnimationCurve from a list of (time, value) pairs.
        /// Tangents are smoothed automatically.
        /// </summary>
        private static AnimationCurve BuildCurve(params (float t, float v)[] points)
        {
            var curve = new AnimationCurve();
            foreach (var (t, v) in points)
                curve.AddKey(t, v);
            SmoothAll(curve);
            return curve;
        }

        private static void SmoothAll(AnimationCurve curve)
        {
            for (int i = 0; i < curve.length; i++)
                curve.SmoothTangents(i, 0f);
        }

        // ------------------------------------------------------------------
        // Asset IO
        // ------------------------------------------------------------------

        private static void SaveAsset(ScriptableObject asset, string folder, string name)
        {
            string path = $"{folder}/{name}.asset";
            AssetDatabase.CreateAsset(asset, path);
        }

        /// <summary>
        /// Returns the path of the folder currently selected in the Project
        /// window, falling back to <c>Assets/HeadBobProfiles</c>.
        /// </summary>
        private static string GetSelectedFolder()
        {
            string selected = AssetDatabase.GetAssetPath(Selection.activeObject);

            if (!string.IsNullOrEmpty(selected))
            {
                if (System.IO.Directory.Exists(selected))
                    return selected;

                // Selection is a file — use its parent folder.
                int lastSlash = selected.LastIndexOf('/');
                if (lastSlash >= 0)
                    return selected[..lastSlash];
            }

            // Fallback: create the folder if it doesn't exist.
            const string fallback = "Assets/HeadBobProfiles";
            if (!AssetDatabase.IsValidFolder(fallback))
                AssetDatabase.CreateFolder("Assets", "HeadBobProfiles");
            return fallback;
        }
    }
}
