using UnityEngine;

namespace KOI.HorrorGameEngine.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerView : MonoBehaviour
    {
        private CharacterController _controller;

        public bool IsGrounded => _controller.isGrounded;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
        }

        public void ApplyMovement(Vector3 movementVector)
        {
            _controller.Move(movementVector);
        }

        public Transform GetTransform()
        {
            return transform;
        }
    }
}