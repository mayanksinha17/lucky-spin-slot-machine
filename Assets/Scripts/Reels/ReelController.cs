#pragma warning disable 0649, 0169, 0414
using System;
using UnityEngine;
using LuckySpin.SlotMachine.Data;

namespace LuckySpin.SlotMachine.Reels
{
    /// <summary>
    /// Controls an individual slot reel, including its visual symbols, spin animation, and snapping.
    /// </summary>
    public class ReelController : MonoBehaviour
    {
        [Header("Reel Configuration")]
        [Tooltip("Zero-based index of this reel (0 = Left, 1 = Center, 2 = Right).")]
        [SerializeField] private int reelIndex = 0;

        [Tooltip("Transform container holding visible symbol items.")]
        [SerializeField] private RectTransform symbolContainer;

        [Header("Runtime State")]
        [SerializeField] private bool isSpinning = false;
        [SerializeField] private SymbolData currentVisibleSymbol;

        public int ReelIndex { get { return reelIndex; } }
        public bool IsSpinning { get { return isSpinning; } }
        public SymbolData CurrentVisibleSymbol { get { return currentVisibleSymbol; } }

        public event Action<int, SymbolData> OnReelStopped;

        public void Initialize(int index)
        {
            reelIndex = index;
        }

        public void StartSpinning()
        {
            isSpinning = true;
        }

        public void StopAt(SymbolData targetSymbol, Action onComplete = null)
        {
            currentVisibleSymbol = targetSymbol;
            isSpinning = false;
            if (onComplete != null)
            {
                onComplete();
            }
            if (OnReelStopped != null)
            {
                OnReelStopped(reelIndex, currentVisibleSymbol);
            }
        }
    }
}
