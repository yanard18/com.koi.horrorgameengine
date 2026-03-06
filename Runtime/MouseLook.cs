using UnityEngine;

namespace KOI.HorrorGameEngine
{
    public class MouseLook : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Suggested range: 1 to 5")]
        [SerializeField] private float _mouseSensitivity = 2f;
        [SerializeField] private bool _invertY = false;

        [Header("References")]
        [Tooltip("Drag the parent Player object here")]
        [SerializeField] private Transform _playerBody;

        private float _xRotation;

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void LateUpdate()
        {
            var mouseX = Input.GetAxisRaw("Mouse X") * _mouseSensitivity;
            var mouseY = Input.GetAxisRaw("Mouse Y") * _mouseSensitivity;

            if (_invertY)
            {
                _xRotation += mouseY;
            }
            else
            {
                _xRotation -= mouseY;
            }

            _xRotation = Mathf.Clamp(_xRotation, -90f, 90f);
            
            transform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);

            if (_playerBody != null)
            {
                _playerBody.Rotate(Vector3.up * mouseX);
            }
        }
    }
}