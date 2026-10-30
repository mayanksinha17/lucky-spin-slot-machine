using UnityEngine;
using UnityEngine.UI;

namespace LuckySpin.SlotMachine.Utilities
{
    /// <summary>
    /// Ensures the UI Canvas dynamically adapts to 16:9 widescreen or arbitrary browser window aspect ratios in WebGL.
    /// </summary>
    [RequireComponent(typeof(CanvasScaler))]
    public class ResolutionFitter : MonoBehaviour
    {
        [Tooltip("Target reference width for UI design.")]
        [SerializeField] private float referenceWidth = 1920f;

        [Tooltip("Target reference height for UI design.")]
        [SerializeField] private float referenceHeight = 1080f;

        private CanvasScaler _scaler;

        private void Awake()
        {
            _scaler = GetComponent<CanvasScaler>();
            ConfigureScaler();
        }

        private void Start()
        {
            UpdateMatchMode();
        }

        private void Update()
        {
            // Detect window resize dynamically in WebGL / windowed mode
            if (Screen.width > 0 && Screen.height > 0)
            {
                UpdateMatchMode();
            }
        }

        private void ConfigureScaler()
        {
            if (_scaler == null) return;
            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _scaler.referenceResolution = new Vector2(referenceWidth, referenceHeight);
            _scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        }

        private void UpdateMatchMode()
        {
            if (_scaler == null) return;
            float currentAspect = (float)Screen.width / Screen.height;
            float targetAspect = referenceWidth / referenceHeight;

            // If screen is wider than 16:9, fit to height (1.0). If taller, fit to width (0.0).
            _scaler.matchWidthOrHeight = currentAspect >= targetAspect ? 1.0f : 0.0f;
        }
    }
}

