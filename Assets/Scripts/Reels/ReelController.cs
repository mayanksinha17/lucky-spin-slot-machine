#pragma warning disable 0649, 0169, 0414
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LuckySpin.SlotMachine.Data;
using LuckySpin.SlotMachine.Animations;

namespace LuckySpin.SlotMachine.Reels
{
    public enum ReelSpinState
    {
        Idle = 0,
        Accelerating = 1,
        Spinning = 2,
        Decelerating = 3,
        Settling = 4
    }

    /// <summary>
    /// Professional slot reel controller with smooth acceleration, continuous wrap-around looping,
    /// eased deceleration with mechanical bounce overshoot (EaseOutBack), and pixel-perfect final alignment.
    /// </summary>
    public class ReelController : MonoBehaviour
    {
        [Header("Reel Identification")]
        [SerializeField] private int reelIndex = 0;
        [SerializeField] private RectTransform symbolContainer;
        [SerializeField] private GameObject symbolItemPrefab;

        [Header("Layout & Sizing")]
        [SerializeField] private float symbolWidth = 150f;
        [SerializeField] private float symbolHeight = 150f;
        [SerializeField] private float verticalPitch = 150f;
        [SerializeField] private int totalSlotCount = 5;

        [Header("Animation Dynamics")]
        [Tooltip("Terminal spin speed in pixels per second.")]
        [SerializeField] private float maxSpinSpeed = 1600f;

        [Tooltip("Acceleration ramp duration in seconds.")]
        [SerializeField] private float accelerationTime = 0.35f;

        [Tooltip("Deceleration and settling duration in seconds.")]
        [SerializeField] private float decelerationTime = 0.65f;

        [Tooltip("Mechanical bounce overshoot magnitude in pixels.")]
        [SerializeField] private float bounceOvershoot = 22f;

        [Header("Dependencies")]
        [SerializeField] private SymbolDatabase symbolDatabase;

        [Header("Runtime State")]
        [SerializeField] private ReelSpinState spinState = ReelSpinState.Idle;
        [SerializeField] private float currentSpeed = 0f;
        [SerializeField] private SymbolData currentPaylineSymbol;
        [SerializeField] private SymbolItemView[] symbolSlots;

        private Coroutine spinRoutine;
        private Action stopCallback;

        public int ReelIndex { get { return reelIndex; } }
        public bool IsSpinning { get { return spinState != ReelSpinState.Idle; } }
        public ReelSpinState SpinState { get { return spinState; } }
        public SymbolData CurrentPaylineSymbol { get { return currentPaylineSymbol; } }
        public SymbolItemView[] SymbolSlots { get { return symbolSlots; } }
        public float VerticalPitch { get { return verticalPitch; } }
        public float SymbolHeight { get { return symbolHeight; } }
        public float SymbolWidth { get { return symbolWidth; } }
        public float DecelerationTime { get { return decelerationTime; } set { decelerationTime = value; } }
        public float AccelerationTime { get { return accelerationTime; } set { accelerationTime = value; } }

        public event Action<int> OnReelSpinStarted;
        public event Action<int, SymbolData> OnReelStopped;

        private void Awake()
        {
            if (symbolContainer == null)
            {
                symbolContainer = GetComponent<RectTransform>();
            }
            EnsureSlotsInitialized();
        }

        private void Start()
        {
            EnsureSlotsInitialized();
            ApplyLayoutAlignment();
            PopulateInitialSymbols();
        }

        public void Initialize(int index, SymbolDatabase db)
        {
            reelIndex = index;
            symbolDatabase = db;
            EnsureSlotsInitialized();
            ApplyLayoutAlignment();
            PopulateInitialSymbols();
        }

        public void PopulateInitialSymbols()
        {
            if (symbolSlots == null || symbolSlots.Length == 0) return;

            int centerIndex = symbolSlots.Length / 2;
            for (int i = 0; i < symbolSlots.Length; i++)
            {
                if (symbolSlots[i] == null) continue;
                if (symbolSlots[i].CurrentData == null)
                {
                    if (i == centerIndex && currentPaylineSymbol != null)
                    {
                        symbolSlots[i].SetSymbol(currentPaylineSymbol);
                    }
                    else if (symbolDatabase != null && symbolDatabase.Count > 0)
                    {
                        int symIdx = (reelIndex * 2 + i) % symbolDatabase.Count;
                        symbolSlots[i].SetSymbol(symbolDatabase.GetSymbol(symIdx));
                    }
                }
            }

            if (currentPaylineSymbol == null && symbolSlots[centerIndex] != null && symbolSlots[centerIndex].CurrentData != null)
            {
                currentPaylineSymbol = symbolSlots[centerIndex].CurrentData;
            }
        }

        public void SetSlots(SymbolItemView[] slots)
        {
            symbolSlots = slots;
            ApplyLayoutAlignment();
        }

        public void EnsureSlotsInitialized()
        {
            if (symbolSlots == null || symbolSlots.Length == 0)
            {
                SymbolItemView[] children = GetComponentsInChildren<SymbolItemView>(true);
                if (children != null && children.Length > 0)
                {
                    symbolSlots = children;
                }
                else if (symbolItemPrefab != null && symbolContainer != null)
                {
                    symbolSlots = new SymbolItemView[totalSlotCount];
                    for (int i = 0; i < totalSlotCount; i++)
                    {
                        GameObject obj = Instantiate(symbolItemPrefab, symbolContainer);
                        obj.name = string.Format("Slot_{0}", i);
                        symbolSlots[i] = obj.GetComponent<SymbolItemView>();
                    }
                }
            }
        }

        public void ApplyLayoutAlignment()
        {
            if (symbolSlots == null || symbolSlots.Length == 0) return;

            int centerIndex = symbolSlots.Length / 2;
            for (int i = 0; i < symbolSlots.Length; i++)
            {
                if (symbolSlots[i] != null)
                {
                    float yPos = (centerIndex - i) * verticalPitch;
                    symbolSlots[i].SetSize(symbolWidth, symbolHeight);
                    symbolSlots[i].SetVerticalPosition(yPos);
                }
            }
        }

        public void ReplaceSymbol(int slotIndex, SymbolData newSymbol)
        {
            if (symbolSlots != null && slotIndex >= 0 && slotIndex < symbolSlots.Length)
            {
                if (symbolSlots[slotIndex] != null)
                {
                    symbolSlots[slotIndex].SetSymbol(newSymbol);
                }

                int centerIndex = symbolSlots.Length / 2;
                if (slotIndex == centerIndex)
                {
                    currentPaylineSymbol = newSymbol;
                }
            }
        }

        public void SetVisibleSymbols(SymbolData top, SymbolData center, SymbolData bottom)
        {
            EnsureSlotsInitialized();
            if (symbolSlots == null || symbolSlots.Length < 3) return;

            int centerIndex = symbolSlots.Length / 2;
            int topIndex = centerIndex - 1;
            int bottomIndex = centerIndex + 1;

            if (topIndex >= 0 && topIndex < symbolSlots.Length) ReplaceSymbol(topIndex, top);
            ReplaceSymbol(centerIndex, center);
            if (bottomIndex >= 0 && bottomIndex < symbolSlots.Length) ReplaceSymbol(bottomIndex, bottom);

            currentPaylineSymbol = center;
        }

        public void SetCenterPaylineSymbol(SymbolData centerSymbol)
        {
            currentPaylineSymbol = centerSymbol;
            EnsureSlotsInitialized();
            if (symbolSlots == null || symbolSlots.Length == 0) return;

            int centerIndex = symbolSlots.Length / 2;
            ReplaceSymbol(centerIndex, centerSymbol);
        }

        public void SetPaylineHighlight(bool highlighted)
        {
            if (symbolSlots == null || symbolSlots.Length == 0) return;
            int centerIndex = symbolSlots.Length / 2;
            if (symbolSlots[centerIndex] != null)
            {
                symbolSlots[centerIndex].SetHighlight(highlighted);
            }
        }

        /// <summary>
        /// Starts reel spinning with gradual acceleration.
        /// </summary>
        public void StartSpinning()
        {
            if (IsSpinning) return;

            spinState = ReelSpinState.Accelerating;
            SetPaylineHighlight(false);
            if (spinRoutine != null) StopCoroutine(spinRoutine);
            spinRoutine = StartCoroutine(SpinRoutine());

            if (OnReelSpinStarted != null)
            {
                OnReelSpinStarted(reelIndex);
            }
        }

        private IEnumerator SpinRoutine()
        {
            spinState = ReelSpinState.Accelerating;
            currentSpeed = 0f;
            float elapsed = 0f;

            // 1. Acceleration Phase (0 -> maxSpinSpeed)
            while (elapsed < accelerationTime)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / accelerationTime);
                currentSpeed = maxSpinSpeed * UIAnimationHelper.EaseInQuad(t);
                ScrollSlots(currentSpeed * Time.deltaTime);
                yield return null;
            }

            // 2. Full Speed Spinning Phase
            spinState = ReelSpinState.Spinning;
            currentSpeed = maxSpinSpeed;

            while (spinState == ReelSpinState.Spinning)
            {
                ScrollSlots(currentSpeed * Time.deltaTime);
                yield return null;
            }
        }

        /// <summary>
        /// Moves all symbol slots downward and wraps them around when exiting the view aperture.
        /// </summary>
        public void ScrollSlots(float deltaDistance)
        {
            if (symbolSlots == null || symbolSlots.Length == 0) return;

            int count = symbolSlots.Length;
            float totalStripHeight = count * verticalPitch;
            int centerIndex = count / 2;
            float bottomLimit = -((count - centerIndex) * verticalPitch);

            for (int i = 0; i < count; i++)
            {
                if (symbolSlots[i] == null) continue;

                Vector2 pos = symbolSlots[i].RectTransform.anchoredPosition;
                pos.y -= deltaDistance;

                // Check wrap-around
                if (pos.y < bottomLimit)
                {
                    pos.y += totalStripHeight;
                    // Assign a new random symbol as it cycles to top
                    if (symbolDatabase != null)
                    {
                        symbolSlots[i].SetSymbol(symbolDatabase.GetRandomSymbol());
                    }
                }

                symbolSlots[i].RectTransform.anchoredPosition = pos;
            }
        }

        /// <summary>
        /// Initiates smooth deceleration and settles precisely on the predetermined target symbol with mechanical recoil.
        /// </summary>
        public void StopAt(SymbolData predeterminedOutcome, Action onComplete = null)
        {
            currentPaylineSymbol = predeterminedOutcome;
            stopCallback = onComplete;

            if (spinRoutine != null) StopCoroutine(spinRoutine);

            if (decelerationTime <= 0f)
            {
                StopImmediately(predeterminedOutcome, onComplete);
                return;
            }

            spinRoutine = StartCoroutine(DecelerateRoutine(predeterminedOutcome));
        }

        /// <summary>
        /// Instantly snaps the reel to the predetermined outcome, concluding any active spin.
        /// </summary>
        public void StopImmediately(SymbolData predeterminedOutcome, Action onComplete = null)
        {
            if (spinRoutine != null) StopCoroutine(spinRoutine);
            spinRoutine = null;

            currentPaylineSymbol = predeterminedOutcome;
            SetCenterPaylineSymbol(predeterminedOutcome);
            ApplyLayoutAlignment();

            spinState = ReelSpinState.Idle;
            currentSpeed = 0f;

            if (onComplete != null) onComplete();
            if (stopCallback != null)
            {
                Action cb = stopCallback;
                stopCallback = null;
                cb();
            }

            if (OnReelStopped != null)
            {
                OnReelStopped(reelIndex, currentPaylineSymbol);
            }
        }

        private IEnumerator DecelerateRoutine(SymbolData targetOutcome)
        {
            spinState = ReelSpinState.Decelerating;
            int count = symbolSlots != null ? symbolSlots.Length : 5;
            int centerIndex = count / 2;

            // Assign target outcome to center slot and ensure neighbors are populated
            if (symbolSlots != null && centerIndex >= 0 && centerIndex < symbolSlots.Length)
            {
                SetCenterPaylineSymbol(targetOutcome);
                if (symbolDatabase != null)
                {
                    for (int i = 0; i < symbolSlots.Length; i++)
                    {
                        if (i != centerIndex && symbolSlots[i] != null && symbolSlots[i].CurrentData == null)
                        {
                            symbolSlots[i].SetSymbol(symbolDatabase.GetRandomSymbol());
                        }
                    }
                }
            }

            // Record initial slot offsets for smooth interpolation
            float[] startY = new float[count];
            float[] targetY = new float[count];
            for (int i = 0; i < count; i++)
            {
                startY[i] = symbolSlots != null && symbolSlots[i] != null ? symbolSlots[i].RectTransform.anchoredPosition.y : 0f;
                targetY[i] = (centerIndex - i) * verticalPitch;
            }

            float elapsed = 0f;
            spinState = ReelSpinState.Settling;

            while (elapsed < decelerationTime)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / decelerationTime);

                // EaseOutBack creates physical bounce past target, then springs back
                float curve = UIAnimationHelper.EaseOutBack(t, 1.3f);

                for (int i = 0; i < count; i++)
                {
                    if (symbolSlots != null && symbolSlots[i] != null)
                    {
                        float currentY = Mathf.LerpUnclamped(startY[i], targetY[i], curve);
                        symbolSlots[i].SetVerticalPosition(currentY);
                    }
                }

                yield return null;
            }

            // Pixel-perfect snap to exact target coordinates
            ApplyLayoutAlignment();
            SetCenterPaylineSymbol(targetOutcome);

            spinState = ReelSpinState.Idle;
            currentSpeed = 0f;

            if (stopCallback != null)
            {
                stopCallback();
                stopCallback = null;
            }

            if (OnReelStopped != null)
            {
                OnReelStopped(reelIndex, currentPaylineSymbol);
            }
        }
    }
}
