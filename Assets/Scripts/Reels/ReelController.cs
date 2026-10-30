#pragma warning disable 0649, 0169, 0414
using System;
using System.Collections.Generic;
using UnityEngine;
using LuckySpin.SlotMachine.Data;

namespace LuckySpin.SlotMachine.Reels
{
    /// <summary>
    /// Reusable controller managing an individual slot reel, its symbol layout, alignment, and display state.
    /// </summary>
    public class ReelController : MonoBehaviour
    {
        [Header("Reel Configuration")]
        [Tooltip("Zero-based index of this reel (0 = Left, 1 = Center, 2 = Right).")]
        [SerializeField] private int reelIndex = 0;

        [Tooltip("Container Transform holding the symbol slot items.")]
        [SerializeField] private RectTransform symbolContainer;

        [Tooltip("Prefab instantiated for each symbol slot if not pre-assigned in scene.")]
        [SerializeField] private GameObject symbolItemPrefab;

        [Header("Layout & Sizing Configuration")]
        [Tooltip("Width of each symbol item.")]
        [SerializeField] private float symbolWidth = 150f;

        [Tooltip("Height of each symbol item.")]
        [SerializeField] private float symbolHeight = 150f;

        [Tooltip("Vertical distance between adjacent symbol centers.")]
        [SerializeField] private float verticalPitch = 150f;

        [Tooltip("Total slots instantiated per reel (e.g., 5: TopBuffer, Top, Center, Bottom, BottomBuffer).")]
        [SerializeField] private int totalSlotCount = 5;

        [Header("Dependencies")]
        [SerializeField] private SymbolDatabase symbolDatabase;

        [Header("Runtime State")]
        [SerializeField] private bool isSpinning = false;
        [SerializeField] private SymbolData currentPaylineSymbol;
        [SerializeField] private SymbolItemView[] symbolSlots;

        public int ReelIndex { get { return reelIndex; } }
        public bool IsSpinning { get { return isSpinning; } }
        public SymbolData CurrentPaylineSymbol { get { return currentPaylineSymbol; } }
        public SymbolItemView[] SymbolSlots { get { return symbolSlots; } }
        public float VerticalPitch { get { return verticalPitch; } }
        public float SymbolHeight { get { return symbolHeight; } }
        public float SymbolWidth { get { return symbolWidth; } }

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
            ApplyLayoutAlignment();
        }

        /// <summary>
        /// Explicit initialization with index and symbol database.
        /// </summary>
        public void Initialize(int index, SymbolDatabase db)
        {
            reelIndex = index;
            symbolDatabase = db;
            EnsureSlotsInitialized();
            ApplyLayoutAlignment();
        }

        /// <summary>
        /// Explicitly injects slot views (useful for procedural generation or testing).
        /// </summary>
        public void SetSlots(SymbolItemView[] slots)
        {
            symbolSlots = slots;
            ApplyLayoutAlignment();
        }

        /// <summary>
        /// Ensures the array of SymbolItemView slots exists and is populated.
        /// </summary>
        public void EnsureSlotsInitialized()
        {
            // If already assigned in Inspector or children, grab them
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

        /// <summary>
        /// Aligns all symbol slots vertically with exact pitch and centering.
        /// Center Payline is always at index 2 (y = 0).
        /// </summary>
        public void ApplyLayoutAlignment()
        {
            if (symbolSlots == null || symbolSlots.Length == 0) return;

            int centerIndex = symbolSlots.Length / 2; // e.g., index 2 for a 5-slot strip
            for (int i = 0; i < symbolSlots.Length; i++)
            {
                if (symbolSlots[i] != null)
                {
                    // Center slot at y = 0, slots above have positive y, slots below have negative y
                    float yPos = (centerIndex - i) * verticalPitch;
                    symbolSlots[i].SetSize(symbolWidth, symbolHeight);
                    symbolSlots[i].SetVerticalPosition(yPos);
                }
            }
        }

        /// <summary>
        /// Replaces a single symbol at the specified slot index.
        /// </summary>
        public void ReplaceSymbol(int slotIndex, SymbolData newSymbol)
        {
            if (symbolSlots != null && slotIndex >= 0 && slotIndex < symbolSlots.Length)
            {
                if (symbolSlots[slotIndex] != null)
                {
                    symbolSlots[slotIndex].SetSymbol(newSymbol);
                }

                // If replacing center payline slot, update tracking
                int centerIndex = symbolSlots.Length / 2;
                if (slotIndex == centerIndex)
                {
                    currentPaylineSymbol = newSymbol;
                }
            }
        }

        /// <summary>
        /// Sets the 3 visible symbols on the reel (Top, Center Payline, Bottom).
        /// </summary>
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

        /// <summary>
        /// Sets only the center payline symbol and updates neighboring buffer symbols if database is available.
        /// </summary>
        public void SetCenterPaylineSymbol(SymbolData centerSymbol)
        {
            EnsureSlotsInitialized();
            if (symbolSlots == null || symbolSlots.Length == 0) return;

            int centerIndex = symbolSlots.Length / 2;
            ReplaceSymbol(centerIndex, centerSymbol);
            currentPaylineSymbol = centerSymbol;

            // Fill neighboring slots with diverse symbols if database present
            if (symbolDatabase != null && symbolDatabase.Symbols != null && symbolDatabase.Symbols.Length > 0)
            {
                for (int i = 0; i < symbolSlots.Length; i++)
                {
                    if (i != centerIndex && symbolSlots[i] != null && symbolSlots[i].CurrentData == null)
                    {
                        SymbolData neighbor = symbolDatabase.GetRandomSymbol();
                        symbolSlots[i].SetSymbol(neighbor);
                    }
                }
            }
        }

        /// <summary>
        /// Highlights or unhighlights the center payline symbol.
        /// </summary>
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
        /// Begins spinning animation loop.
        /// </summary>
        public void StartSpinning()
        {
            isSpinning = true;
            SetPaylineHighlight(false);
            if (OnReelSpinStarted != null)
            {
                OnReelSpinStarted(reelIndex);
            }
        }

        /// <summary>
        /// Stops the reel at the predetermined outcome symbol.
        /// </summary>
        public void StopAt(SymbolData predeterminedOutcome, Action onComplete = null)
        {
            currentPaylineSymbol = predeterminedOutcome;
            SetCenterPaylineSymbol(predeterminedOutcome);
            isSpinning = false;

            if (onComplete != null)
            {
                onComplete();
            }

            if (OnReelStopped != null)
            {
                OnReelStopped(reelIndex, currentPaylineSymbol);
            }
        }
    }
}
