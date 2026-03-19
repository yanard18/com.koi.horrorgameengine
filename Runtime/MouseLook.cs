using UnityEngine;

namespace KOI.HorrorGameEngine
{
    public class MouseLook : MonoBehaviour
    {
        [SerializeField] private GameObject _playerCharacter;
        [SerializeField] private float _mouseSensitivity = 2f;
        [SerializeField] private float _interpolationSpeed = 15f;
        [SerializeField] private float _minPitch = -90f;
        [SerializeField] private float _maxPitch = 90f;

        private Quaternion _rotationCamera;
        private Quaternion _rotationCharacter;
        private float _pitch;
        private float _yaw;

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            _rotationCamera = transform.localRotation;
            _rotationCharacter = _playerCharacter.transform.rotation;
        }

        private void LateUpdate()
        {
            var mouseX = Input.GetAxisRaw("Mouse X") * _mouseSensitivity;
            var mouseY = Input.GetAxisRaw("Mouse Y") * _mouseSensitivity;

            _yaw += mouseX;
            _pitch -= mouseY;
            _pitch = Mathf.Clamp(_pitch, _minPitch, _maxPitch);

            _rotationCamera = Quaternion.Euler(_pitch, 0f, 0f);
            _rotationCharacter = Quaternion.Euler(0f, _yaw, 0f);

            transform.localRotation = Quaternion.Slerp(transform.localRotation, _rotationCamera, Time.deltaTime * _interpolationSpeed);
            _playerCharacter.transform.rotation = 
                Quaternion.Slerp(_playerCharacter.transform.rotation, _rotationCharacter, Time.deltaTime * _interpolationSpeed);
        }
    }
}