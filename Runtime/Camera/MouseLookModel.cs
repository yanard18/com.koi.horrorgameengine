using UnityEngine;

namespace KOI.HorrorGameEngine.Camera
{
    [CreateAssetMenu(fileName = "MouseLookModel", menuName = "Horror System/Camera/Mouse Look Model")]
    public class MouseLookModel : ScriptableObject
    {
        [Header("Look Settings")]
        public float MouseSensitivity = 2f;
        public float MinPitch = -90f;
        public float MaxPitch = 90f;

        [System.NonSerialized] public float Pitch;
        [System.NonSerialized] public float Yaw;
    }
}
