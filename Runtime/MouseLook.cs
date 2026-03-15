using UnityEngine;

namespace KOI.HorrorGameEngine
{
    public class MouseLook : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Suggested range: 1 to 5")]
        [SerializeField] private float _mouseSensitivity = 2f;
        [SerializeField] private bool _invertY = false;

        [Header("Mouse Smoothing")]
        [SerializeField] private bool _useSmoothing = true;
        [Tooltip("Higher = smoother but slightly more delayed. 0.03 to 0.05 is the sweet spot.")]
        [SerializeField] private float _smoothTime = 0.03f;

        [Header("References")]
        [Tooltip("Drag the parent Player object here")]
        [SerializeField] private Transform _playerBody;

        private float _xRotation;
        
        private Vector2 _currentMouseDelta;
        private Vector2 _currentMouseVelocity;

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update() 
        {
            var targetMouseDelta = new Vector2(
                Input.GetAxisRaw("Mouse X"), 
                Input.GetAxisRaw("Mouse Y")
            );

            if (_useSmoothing)
            {
                _currentMouseDelta = Vector2.SmoothDamp(
                    _currentMouseDelta, 
                    targetMouseDelta, 
                    ref _currentMouseVelocity, 
                    _smoothTime
                );
            }
            else
            {
                _currentMouseDelta = targetMouseDelta;
            }

            var mouseX = _currentMouseDelta.x * _mouseSensitivity;
            var mouseY = _currentMouseDelta.y * _mouseSensitivity;

            _xRotation += _invertY ? mouseY : -mouseY;
            _xRotation = Mathf.Clamp(_xRotation, -90f, 90f);
            
            transform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);

            if (_playerBody != null)
            {
                _playerBody.Rotate(Vector3.up * mouseX);
            }
        }
    }
}