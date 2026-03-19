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
    public class JitterInjectionProcessor : InputProcessor<Vector2>
    {
        [Tooltip("The maximum intensity multiplier of the perlin noise jitter.")]
        public float jitterIntensity = 0.5f;

        [Tooltip("The speed pattern at which the noise scrolls.")]
        public float jitterSpeed = 10f;

#if UNITY_EDITOR
        static JitterInjectionProcessor()
        {
            Initialize();
        }
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Initialize()
        {
            InputSystem.RegisterProcessor<JitterInjectionProcessor>("JitterInjection");
        }

        public override Vector2 Process(Vector2 value, InputControl control)
        {
            // Inject algorithmic mathematical noise (simulating trembling/panic)
            float time = Time.unscaledTime * jitterSpeed;
            float noiseX = (Mathf.PerlinNoise(time, 0) * 2f - 1f) * jitterIntensity;
            float noiseY = (Mathf.PerlinNoise(0, time) * 2f - 1f) * jitterIntensity;

            return new Vector2(value.x + noiseX, value.y + noiseY);
        }
    }
}
