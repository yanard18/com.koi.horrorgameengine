using UnityEngine;
using KOI.HorrorGameEngine;

namespace KOI.HorrorGameEngine.Camera
{
    public class MouseLookPresenter : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private InputReader _inputReader;
        [SerializeField] private MouseLookModel _model;
        [SerializeField] private MouseLookView _view;

        // Public initialization method for Auto-Wiring from the Facade
        public void Initialize(InputReader inputReader, MouseLookModel model, MouseLookView view)
        {
            _inputReader = inputReader;
            _model = model;
            _view = view;

            // Re-bind events in case they were already enabled but null
            OnDisable();
            OnEnable();
        }

        private Vector2 _lookInput;

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OnEnable()
        {
        }

        private void OnDisable()
        {
        }

        private void LateUpdate()
        {
            if (_inputReader != null)
            {
                _lookInput = _inputReader.GetLookInput();
            }

            // Input relies on Unity's new Input System providing values that might be
            // modified by TensionDampener or Jitter processors natively.
            var mouseX = _lookInput.x * _model.MouseSensitivity;
            var mouseY = _lookInput.y * _model.MouseSensitivity;

            _model.Yaw += mouseX;
            _model.Pitch -= mouseY;
            _model.Pitch = Mathf.Clamp(_model.Pitch, _model.MinPitch, _model.MaxPitch);

            var rotationCamera = Quaternion.Euler(_model.Pitch, 0f, 0f);
            var rotationCharacter = Quaternion.Euler(0f, _model.Yaw, 0f);

            _view.ApplyRotations(rotationCamera, rotationCharacter);
        }
    }
}
