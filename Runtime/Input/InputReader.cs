using UnityEngine;
using System;
using UnityEngine.InputSystem;

namespace KOI.HorrorGameEngine
{
    [CreateAssetMenu(fileName = "InputReader", menuName = "Horror System/Input/Input Reader")]
    public class InputReader : ScriptableObject
    {
        // Public Event Channels
        public event Action<Vector2> MoveEvent = delegate { };
        public event Action<Vector2> LookEvent = delegate { };
        public event Action JumpEvent = delegate { };
        public event Action JumpCanceledEvent = delegate { }; 

        // Internal native actions
        private InputAction _moveAction;
        private InputAction _lookAction;
        private InputAction _jumpAction;

        public void EnableInput()
        {
            if (_moveAction == null)
            {
                // Hardcode Move (Left Stick / WASD)
                _moveAction = new InputAction("Move", type: InputActionType.Value, expectedControlType: "Vector2");
                
                // Add WASD keys
                _moveAction.AddCompositeBinding("Dpad")
                    .With("Up", "<Keyboard>/w")
                    .With("Down", "<Keyboard>/s")
                    .With("Left", "<Keyboard>/a")
                    .With("Right", "<Keyboard>/d");
                
                // Add arrow keys
                _moveAction.AddCompositeBinding("Dpad")
                    .With("Up", "<Keyboard>/upArrow")
                    .With("Down", "<Keyboard>/downArrow")
                    .With("Left", "<Keyboard>/leftArrow")
                    .With("Right", "<Keyboard>/rightArrow");
                
                // Add gamepad left stick
                _moveAction.AddBinding("<Gamepad>/leftStick");

                // Hardcode Look (Right Stick / Mouse)
                // Use PassThrough for delta to guarantee continuous updates
                _lookAction = new InputAction("Look", type: InputActionType.PassThrough, expectedControlType: "Vector2");
                _lookAction.AddBinding("<Mouse>/delta");
                _lookAction.AddBinding("<Gamepad>/rightStick");

                // Hardcode Jump (Space / Gamepad South)
                _jumpAction = new InputAction("Jump", type: InputActionType.Button, expectedControlType: "Button");
                _jumpAction.AddBinding("<Keyboard>/space");
                _jumpAction.AddBinding("<Gamepad>/buttonSouth");

                // Route them to our MVP events
                _moveAction.performed += OnMove;
                _moveAction.canceled += OnMove;

                _lookAction.performed += OnLook;
                _lookAction.canceled += OnLook;

                _jumpAction.performed += OnJump;
                _jumpAction.canceled += OnJump;
            }

            _moveAction.Enable();
            _lookAction.Enable();
            _jumpAction.Enable();

            if (Keyboard.current != null) InputSystem.EnableDevice(Keyboard.current);
            if (Mouse.current != null) InputSystem.EnableDevice(Mouse.current);
        }

        public void DisableInput()
        {
            _moveAction?.Disable();
            _lookAction?.Disable();
            _jumpAction?.Disable();
        }

        // Translation logic
        private void OnMove(InputAction.CallbackContext context) => MoveEvent.Invoke(context.ReadValue<Vector2>());
        private void OnLook(InputAction.CallbackContext context) => LookEvent.Invoke(context.ReadValue<Vector2>());
        private void OnJump(InputAction.CallbackContext context)
        {
            if (context.phase == InputActionPhase.Performed) JumpEvent.Invoke();
            else if (context.phase == InputActionPhase.Canceled) JumpCanceledEvent.Invoke();
        }
        
        // Public method to get move input for debugging
        public Vector2 GetMoveInput()
        {
            if (_moveAction != null && _moveAction.enabled)
            {
                return _moveAction.ReadValue<Vector2>();
            }
            return Vector2.zero;
        }

        public Vector2 GetLookInput()
        {
            if (_lookAction != null && _lookAction.enabled)
            {
                var val = _lookAction.ReadValue<Vector2>();
                if (val.sqrMagnitude > 0) Debug.Log($"GetLookInput -> {val}");
                return val;
            }
            return Vector2.zero;
        }

        public bool GetJumpInput()
        {
            if (_jumpAction != null && _jumpAction.enabled)
            {
                bool val = _jumpAction.IsPressed();
                if (val) Debug.Log($"GetJumpInput -> {val}");
                return val;
            }
            return false;
        }
    }
}
