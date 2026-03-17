using UnityEngine;
using KOI.HorrorGameEngine.Player;
using KOI.HorrorGameEngine.Camera;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace KOI.HorrorGameEngine
{
    [RequireComponent(typeof(CharacterController))]
    [AddComponentMenu("Horror System/Horror Player Controller (Beginner Friendly)")]
    public class HorrorPlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("How fast the player moves linearly.")]
        public float WalkSpeed = 12f;
        
        [Tooltip("Can the player jump?")]
        public bool CanJump = true;
        
        [Tooltip("How high the player jumps in Unity units.")]
        public float JumpHeight = 3f;

        [Header("Aiming Settings")]
        [Tooltip("The speed of the camera rotation based on mouse movement.")]
        public float MouseSensitivity = 2f;

        [Header("Advanced / Behind the scenes (Auto-Assigned)")]
        [SerializeField] private InputReader _inputReader;
        [SerializeField] private PlayerModel _playerModel;
        [SerializeField] private MouseLookModel _lookModel;

        private void Start()
        {
            if (_playerModel != null)
            {
                _playerModel.MaxSpeed = WalkSpeed;
                _playerModel.CanJump = CanJump;
                _playerModel.JumpHeight = JumpHeight;
            }
            if (_lookModel != null)
            {
                _lookModel.MouseSensitivity = MouseSensitivity;
            }
            AutoWireComponents();
        }

        private void OnEnable()
        {
            if (_inputReader != null)
            {
                _inputReader.EnableInput();
            }
        }

        private void OnDisable()
        {
            if (_inputReader != null)
            {
                _inputReader.DisableInput();
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Sync values to the ScriptableObjects live in the Editor!
            if (_playerModel != null)
            {
                _playerModel.MaxSpeed = WalkSpeed;
                _playerModel.CanJump = CanJump;
                _playerModel.JumpHeight = JumpHeight;
                EditorUtility.SetDirty(_playerModel);
            }

            if (_lookModel != null)
            {
                _lookModel.MouseSensitivity = MouseSensitivity;
                EditorUtility.SetDirty(_lookModel);
            }
        }
#endif

        [ContextMenu("Setup / Auto-Wire System")]
        private void AutoWireComponents()
        {
#if UNITY_EDITOR
            // 1. Generate Scriptable Objects if they don't exist
            EnsureDataAssetsExist();
#endif

            if (_inputReader == null || _playerModel == null || _lookModel == null)
            {
                Debug.LogError("PlayerController: Data Assets are missing and could not be generated.");
                return;
            }

            // Immediately enable inputs now that we've ensured they exist
            _inputReader.EnableInput();

            // 2. Setup locomotion view & presenter
            var playerView = gameObject.GetComponent<PlayerView>();
            if (playerView == null) playerView = gameObject.AddComponent<PlayerView>();

            var playerPresenter = gameObject.GetComponent<PlayerPresenter>();
            if (playerPresenter == null) playerPresenter = gameObject.AddComponent<PlayerPresenter>();

            playerPresenter.Initialize(_inputReader, _playerModel, playerView);

            // 3. Setup camera view & presenter
            var cam = GetComponentInChildren<UnityEngine.Camera>();
            if (cam == null)
            {
                Debug.LogError("PlayerController: You must have a Camera object as a child of this GameObject!");
                return;
            }

            var lookView = cam.gameObject.GetComponent<MouseLookView>();
            if (lookView == null) lookView = cam.gameObject.AddComponent<MouseLookView>();
            
            // The view needs to know what physical body to turn left/right
            lookView.Initialize(this.transform);

            var lookPresenter = cam.gameObject.GetComponent<MouseLookPresenter>();
            if (lookPresenter == null) lookPresenter = cam.gameObject.AddComponent<MouseLookPresenter>();

            lookPresenter.Initialize(_inputReader, _lookModel, lookView);
        }

#if UNITY_EDITOR
        private void EnsureDataAssetsExist()
        {
            string folderPath = "Assets/HorrorEngineData";
            
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder("Assets", "HorrorEngineData");
            }

            if (_inputReader == null)
            {
                _inputReader = LoadOrCreateAsset<InputReader>($"{folderPath}/AutoInputReader.asset");
            }

            if (_playerModel == null)
            {
                _playerModel = LoadOrCreateAsset<PlayerModel>($"{folderPath}/AutoPlayerModel.asset");
                // Sync initial values
                _playerModel.MaxSpeed = WalkSpeed;
                _playerModel.CanJump = CanJump;
                _playerModel.JumpHeight = JumpHeight;
                EditorUtility.SetDirty(_playerModel);
            }

            if (_lookModel == null)
            {
                _lookModel = LoadOrCreateAsset<MouseLookModel>($"{folderPath}/AutoMouseLookModel.asset");
                // Sync initial values
                _lookModel.MouseSensitivity = MouseSensitivity;
                EditorUtility.SetDirty(_lookModel);
            }
            
            AssetDatabase.SaveAssets();
        }

        private T LoadOrCreateAsset<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }
            return asset;
        }
#endif
    }
}
