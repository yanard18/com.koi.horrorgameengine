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
            var folder = GetSelectedFolder();

            CreateIdle(folder);
            CreateWalk(folder);
            CreateJump(folder);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[HeadBob] Created example profiles in '{folder}'.");
        }

        /// <summary>
        /// Breathing / idle profile — very slow, barely perceptible motion that
        /// simulates the camera rising and falling with each breath.
        /// </summary>
        private static void CreateIdle(string folder)
        {
            var p = ScriptableObject.CreateInstance<HeadBobProfile>();

            p.frequency         = 0.25f;
            p.loop              = true;
            p.positionAmplitude = 0.003f;
            p.rotationAmplitude = 0.20f;

            p.posY = HeadBobProfile.BuildSine(1, 12);
            p.posX = ScaledSine(1, 8, 0.4f);
            p.posZ = HeadBobProfile.BuildNegativeAbsSine(1, 8);
            p.rotX = ScaledSine(1, 12, 0.5f);
            p.rotY = HeadBobProfile.BuildFlat();
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

            p.frequency         = 1.6f;
            p.loop              = true;
            p.positionAmplitude = 0.04f;
            p.rotationAmplitude = 1.2f;

            p.posX = HeadBobProfile.BuildSine(1, 8);
            p.posY = HeadBobProfile.BuildSine(2, 16);
            p.posZ = HeadBobProfile.BuildNegativeAbsSine(2, 16);
            p.rotX = ScaledSine(2, 16, 0.6f);
            p.rotY = HeadBobProfile.BuildFlat();
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

            p.frequency         = 0.9f;
            p.loop              = false;
            p.positionAmplitude = 0.06f;
            p.rotationAmplitude = 2.0f;

            p.posY = BuildCurve(
                (0.00f,  0.00f),
                (0.10f,  0.50f),
                (0.38f,  0.10f),
                (0.60f,  0.05f),
                (0.68f, -1.00f),
                (0.80f,  0.25f),
                (0.90f, -0.05f),
                (1.00f,  0.00f)
            );

            p.posX = BuildCurve(
                (0.00f,  0.00f),
                (0.65f,  0.20f),
                (0.85f, -0.10f),
                (1.00f,  0.00f)
            );

            p.posZ = BuildCurve(
                (0.00f,  0.00f),
                (0.12f, -0.40f),
                (0.40f,  0.00f),
                (0.68f,  0.60f),
                (1.00f,  0.00f)
            );

            p.rotX = BuildCurve(
                (0.00f,  0.00f),
                (0.15f, -0.50f),
                (0.40f,  0.00f),
                (0.68f,  1.00f),
                (0.85f, -0.20f),
                (1.00f,  0.00f)
            );

            p.rotY = HeadBobProfile.BuildFlat();

            p.rotZ = BuildCurve(
                (0.00f,  0.00f),
                (0.68f,  0.30f),
                (0.85f, -0.10f),
                (1.00f,  0.00f)
            );

            SaveAsset(p, folder, "HBP_Jump");
        }

        /// <summary>
        /// Returns a sine curve scaled so that peak value equals
        /// <paramref name="scale"/> instead of 1.
        /// </summary>
        private static AnimationCurve ScaledSine(int cycles, int samplesPerCycle, float scale)
        {
            var total = cycles * samplesPerCycle;
            var curve = new AnimationCurve();
            for (var i = 0; i <= total; i++)
            {
                var t = (float)i / total;
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
            for (var i = 0; i < curve.length; i++)
                curve.SmoothTangents(i, 0f);
        }

        private static void SaveAsset(ScriptableObject asset, string folder, string name)
        {
            var path = $"{folder}/{name}.asset";
            AssetDatabase.CreateAsset(asset, path);
        }

        /// <summary>
        /// Returns the path of the folder currently selected in the Project
        /// window, falling back to <c>Assets/HeadBobProfiles</c>.
        /// </summary>
        private static string GetSelectedFolder()
        {
            var selected = AssetDatabase.GetAssetPath(Selection.activeObject);

            if (!string.IsNullOrEmpty(selected))
            {
                if (System.IO.Directory.Exists(selected))
                    return selected;

                var lastSlash = selected.LastIndexOf('/');
                if (lastSlash >= 0)
                    return selected[..lastSlash];
            }

            const string fallback = "Assets/HeadBobProfiles";
            if (!AssetDatabase.IsValidFolder(fallback))
                AssetDatabase.CreateFolder("Assets", "HeadBobProfiles");
            return fallback;
        }
    }
}
