using UnityEngine;

namespace KOI.HorrorGameEngine.Player
{
    [CreateAssetMenu(fileName = "PlayerModel", menuName = "Horror System/Player/Model")]
    public class PlayerModel : ScriptableObject
    {
        [Header("Movement Stats")]
        public float MaxSpeed = 12f;
        public float GroundAcceleration = 50f;
        public float GroundDeceleration = 50f;

        [Header("Air Stats")]
        public float MaxAirSpeed = 12f;
        public float AirAcceleration = 20f;
        public float AirDeceleration = 5f;

        [Header("Jump Context")]
        public bool CanJump = true;
        public float JumpHeight = 3f;
        public float Gravity = -19.62f;

        // Current volatile states (Could reside here or just in presenter logic)
        // For strict pure MVP, model just holds the data struct or configurations
        [System.NonSerialized] public Vector3 CurrentVelocity;
        [System.NonSerialized] public Vector3 TargetKinematicVelocity;
    }
}
