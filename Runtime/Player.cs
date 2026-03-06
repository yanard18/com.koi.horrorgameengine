using UnityEngine;

namespace KOI.HorrorGameEngine
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float _moveSpeed = 12f;
        [SerializeField] private float _gravity = -9.81f;
        [SerializeField] private float _jumpHeight = 3f;

        private CharacterController _controller;
        private Vector3 _velocity;

        private void Start()
        {
            _controller = GetComponent<CharacterController>();
        }

        private void Update()
        {
            if (_controller.isGrounded && _velocity.y < 0)
            {
                _velocity.y = -2f; 
            }

            var x = Input.GetAxis("Horizontal");
            var z = Input.GetAxis("Vertical");

            var move = transform.right * x + transform.forward * z;

            _controller.Move(move * _moveSpeed * Time.deltaTime);

            if (Input.GetButtonDown("Jump") && _controller.isGrounded)
            {
                _velocity.y = Mathf.Sqrt(_jumpHeight * -2f * _gravity);
            }

            _velocity.y += _gravity * Time.deltaTime;

            _controller.Move(_velocity * Time.deltaTime);
        }
    }
}