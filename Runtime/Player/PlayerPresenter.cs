using UnityEngine;
using KOI.HorrorGameEngine;

namespace KOI.HorrorGameEngine.Player
{
    public class PlayerPresenter : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private InputReader _inputReader;
        [SerializeField] private PlayerModel _model;
        [SerializeField] private PlayerView _view;

        // Public initialization method for Auto-Wiring from the Facade
        public void Initialize(InputReader inputReader, PlayerModel model, PlayerView view)
        {
            _inputReader = inputReader;
            _model = model;
            _view = view;

            // Re-bind events in case they were already enabled but null
            OnDisable();
            OnEnable();
        }

        private Vector2 _currentMoveInput;
        private bool _isJumpPressed;

        private void OnEnable()
        {
            if (_inputReader != null)
            {
                _inputReader.MoveEvent += OnMoveReceived;
                _inputReader.JumpEvent += OnJumpPressed;
                _inputReader.JumpCanceledEvent += OnJumpCanceled;
            }
        }

        private void OnDisable()
        {
            if (_inputReader != null)
            {
                _inputReader.MoveEvent -= OnMoveReceived;
                _inputReader.JumpEvent -= OnJumpPressed;
                _inputReader.JumpCanceledEvent -= OnJumpCanceled;
            }
        }

        private void OnMoveReceived(Vector2 input)
        {
            _currentMoveInput = input;
        }

        private void OnJumpPressed()
        {
            _isJumpPressed = true;
        }

        private void OnJumpCanceled()
        {
            _isJumpPressed = false;
        }

        private void Update()
        {
            if (_inputReader != null)
            {
                // Unify polling to prevent event drops
                _currentMoveInput = _inputReader.GetMoveInput();
                if (_inputReader.GetJumpInput()) 
                {
                    _isJumpPressed = true;
                }
            }
            
            ProcessMovement();
            ProcessGravityAndJump();
            
            _view.ApplyMovement((_model.TargetKinematicVelocity + _model.CurrentVelocity) * Time.deltaTime);

            if (_inputReader != null && !_inputReader.GetJumpInput())
            {
                _isJumpPressed = false;
            }
        }

        private void ProcessMovement()
        {
            var tf = _view.GetTransform();
            var inputDirection = (tf.right * _currentMoveInput.x + tf.forward * _currentMoveInput.y).normalized;
            
            var isGrounded = _view.IsGrounded;
            var currentAcceleration = isGrounded ? _model.GroundAcceleration : _model.AirAcceleration;
            var currentDeceleration = isGrounded ? _model.GroundDeceleration : _model.AirDeceleration;
            var maxCurrentSpeed = isGrounded ? _model.MaxSpeed : _model.MaxAirSpeed;

            if (inputDirection.sqrMagnitude > 0)
            {
                var target = inputDirection * maxCurrentSpeed;
                _model.TargetKinematicVelocity = Vector3.MoveTowards(_model.TargetKinematicVelocity, target, currentAcceleration * Time.deltaTime);
            }
            else
            {
                _model.TargetKinematicVelocity = Vector3.MoveTowards(_model.TargetKinematicVelocity, Vector3.zero, currentDeceleration * Time.deltaTime);
            }
        }

        private void ProcessGravityAndJump()
        {
            // Add a tiny tolerance to grounded check to prevent single-frame drops preventing jump
            bool isGrounded = _view.IsGrounded;

            if (isGrounded && _model.CurrentVelocity.y < 0)
            {
                // Push slightly down to stick to ground
                _model.CurrentVelocity.y = -2f;
            }

            // We log here if they attempted to jump to see if it's the model blocking them
            if (_isJumpPressed)
            {
                if (_model.CanJump && isGrounded)
                {
                    _model.CurrentVelocity.y = Mathf.Sqrt(_model.JumpHeight * -2f * _model.Gravity);
                    _isJumpPressed = false; // Reset state
                }
            }

            _model.CurrentVelocity.y += _model.Gravity * Time.deltaTime;
        }
    }
}
