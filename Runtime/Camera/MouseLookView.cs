using UnityEngine;

namespace KOI.HorrorGameEngine.Camera
{
    public class MouseLookView : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private Transform _playerCharacterTransform;

        public void Initialize(Transform playerBody)
        {
            _playerCharacterTransform = playerBody;
        }

        public void ApplyRotations(Quaternion cameraLocalRotation, Quaternion characterRotation)
        {
            transform.localRotation = cameraLocalRotation;
            if (_playerCharacterTransform != null)
            {
                _playerCharacterTransform.rotation = characterRotation;
            }
        }
    }
}
