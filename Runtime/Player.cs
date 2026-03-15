using UnityEngine;

namespace KOI.HorrorGameEngine
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float _maxSpeed = 12f;
        [SerializeField] private float _groundAcceleration = 50f;
        [SerializeField] private float _groundDeceleration = 50f;
        
        [Header("Air Settings")]
        [SerializeField] private float _maxAirSpeed = 12f;
        [SerializeField] private float _airAcceleration = 20f;
        [SerializeField] private float _airDeceleration = 5f;

        [Header("Jump Settings")]
        [SerializeField] private bool _canJump = true;
        [SerializeField] private float _jumpHeight = 3f;
        [SerializeField] private float _gravity = -19.62f;
        [SerializeField] private float _groundCoyoteTime = 0.15f;
        [SerializeField] private float _jumpCoyoteTime = 0.1f;

        private CharacterController _controller;
        private Vector3 _velocity;
        private Vector3 _moveVelocity;

        private float _lastGroundedTime;
        private float _lastJumpPressedTime;

        private void Start()
        {
            _controller = GetComponent<CharacterController>();
            _lastGroundedTime = -1f;
            _lastJumpPressedTime = -1f;
        }

        private void Update()
        {
            HandleTimers();
            HandleMovement();
            HandleGravityAndJump();
            
            _controller.Move((_moveVelocity + _velocity) * Time.deltaTime);
        }

        private void HandleTimers()
        {
            if (_controller.isGrounded)
            {
                _lastGroundedTime = Time.time;
            }

            if (Input.GetButtonDown("Jump"))
            {
                _lastJumpPressedTime = Time.time;
            }
        }

        private void HandleMovement()
        {
            var x = Input.GetAxisRaw("Horizontal");
            var z = Input.GetAxisRaw("Vertical");

            var inputDirection = (transform.right * x + transform.forward * z).normalized;

            var currentAcceleration = _controller.isGrounded ? _groundAcceleration : _airAcceleration;
            var currentDeceleration = _controller.isGrounded ? _groundDeceleration : _airDeceleration;
            var maxCurrentSpeed = _controller.isGrounded ? _maxSpeed : _maxAirSpeed;

            if (inputDirection.magnitude > 0)
            {
                var targetVelocity = inputDirection * maxCurrentSpeed;
                _moveVelocity = Vector3.MoveTowards(_moveVelocity, targetVelocity, currentAcceleration * Time.deltaTime);
            }
            else
            {
                _moveVelocity = Vector3.MoveTowards(_moveVelocity, Vector3.zero, currentDeceleration * Time.deltaTime);
            }
        }

        private void HandleGravityAndJump()
        {
            if (_controller.isGrounded && _velocity.y < 0)
            {
                _velocity.y = -2f; 
            }

            var isGroundCoyoteTimeValid = (Time.time - _lastGroundedTime) <= _groundCoyoteTime;
            var isJumpCoyoteTimeValid = (Time.time - _lastJumpPressedTime) <= _jumpCoyoteTime;

            if (_canJump && isGroundCoyoteTimeValid && isJumpCoyoteTimeValid)
            {
                _velocity.y = Mathf.Sqrt(_jumpHeight * -2f * _gravity);
                
                _lastJumpPressedTime = -1f;
                _lastGroundedTime = -1f;
            }

            _velocity.y += _gravity * Time.deltaTime;
        }
    }
}