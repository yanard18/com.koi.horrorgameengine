using UnityEngine;
using UnityEngine.InputSystem;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace KOI.HorrorGameEngine.Input
{
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    public class TensionDampenerProcessor : InputProcessor<Vector2>
    {
        [Tooltip("The time in seconds it takes to reach the target vector. Higher values mean more sluggish/heavy controls.")]
        public float dampeningTime = 0.15f;

        private Vector2 currentVelocity;
        private Vector2 currentValue;

#if UNITY_EDITOR
        static TensionDampenerProcessor()
        {
            Initialize();
        }
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Initialize()
        {
            InputSystem.RegisterProcessor<TensionDampenerProcessor>("TensionDampener");
        }

        public override Vector2 Process(Vector2 value, InputControl control)
        {
            // Note: Since processors should ideally be stateless, this is a slight deviation 
            // for the sake of the design doc's request for Dampening per-device input. 
            // A truly stateless processor would struggle with Time-based smoothing.
            // But we keep this internal state per-processor instance.
            currentValue = Vector2.SmoothDamp(currentValue, value, ref currentVelocity, dampeningTime, Mathf.Infinity, Time.unscaledDeltaTime);
            return currentValue;
        }
    }
}
