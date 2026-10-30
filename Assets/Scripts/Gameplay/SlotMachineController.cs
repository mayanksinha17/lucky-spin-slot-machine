#pragma warning disable 0649, 0169, 0414
using System;
using System.Collections;
using UnityEngine;
using LuckySpin.SlotMachine.Data;
using LuckySpin.SlotMachine.Reels;

namespace LuckySpin.SlotMachine.Gameplay
{
    /// <summary>
    /// Coordinates the three independent reels, managing initialization, spin orchestration, and stop sequencing.
    /// </summary>
    public class SlotMachineController : MonoBehaviour
    {
        [Header("Reel Controllers")]
        [Tooltip("Ordered references to the 3 reel controllers (0 = Left, 1 = Center, 2 = Right).")]
        [SerializeField] private ReelController[] reels = new ReelController[3];

        [Header("Timing Configuration")]
        [Tooltip("Delay in seconds between stopping consecutive reels.")]
        [SerializeField] private float stopDelayBetweenReels = 0.35f;

        [Header("Symbol Configuration")]
        [SerializeField] private SymbolDatabase symbolDatabase;

        public ReelController[] Reels { get { return reels; } }
        public int ReelCount { get { return reels != null ? reels.Length : 0; } }

        public bool IsAnyReelSpinning
        {
            get
            {
                if (reels == null) return false;
                for (int i = 0; i < reels.Length; i++)
                {
                    if (reels[i] != null && reels[i].IsSpinning) return true;
                }
                return false;
            }
        }

        public event Action<int, SymbolData> OnSingleReelStopped;
        public event Action OnAllReelsStopped;

        private void Awake()
        {
            InitializeReelListeners();
        }

        private void Start()
        {
            if (symbolDatabase != null)
            {
                InitializeReels(symbolDatabase);
            }
        }

        private void InitializeReelListeners()
        {
            if (reels == null) return;
            for (int i = 0; i < reels.Length; i++)
            {
                if (reels[i] != null)
                {
                    int index = i;
                    reels[i].OnReelStopped += delegate(int rIdx, SymbolData sym) {
                        if (OnSingleReelStopped != null) OnSingleReelStopped(rIdx, sym);
                    };
                }
            }
        }

        /// <summary>
        /// Initializes each reel with its index and shared symbol database.
        /// </summary>
        public void InitializeReels(SymbolDatabase db)
        {
            symbolDatabase = db;
            if (reels == null) return;
            for (int i = 0; i < reels.Length; i++)
            {
                if (reels[i] != null)
                {
                    reels[i].Initialize(i, db);
                }
            }
        }

        /// <summary>
        /// Explicitly configures symbols for a specific reel.
        /// </summary>
        public void SetReelSymbols(int reelIndex, SymbolData top, SymbolData center, SymbolData bottom)
        {
            if (reels != null && reelIndex >= 0 && reelIndex < reels.Length)
            {
                if (reels[reelIndex] != null)
                {
                    reels[reelIndex].SetVisibleSymbols(top, center, bottom);
                }
            }
        }

        /// <summary>
        /// Highlights or unhighlights the payline on all reels (e.g. for wins).
        /// </summary>
        public void SetPaylineHighlights(bool active)
        {
            if (reels == null) return;
            for (int i = 0; i < reels.Length; i++)
            {
                if (reels[i] != null)
                {
                    reels[i].SetPaylineHighlight(active);
                }
            }
        }

        /// <summary>
        /// Starts all reels spinning simultaneously.
        /// </summary>
        public void SpinAll()
        {
            SetPaylineHighlights(false);
            if (reels == null) return;
            for (int i = 0; i < reels.Length; i++)
            {
                if (reels[i] != null)
                {
                    reels[i].StartSpinning();
                }
            }
        }

        /// <summary>
        /// Stops all reels sequentially with the predetermined outcomes.
        /// </summary>
        public void StopReels(SymbolData[] predeterminedOutcomes)
        {
            if (reels == null || predeterminedOutcomes == null) return;
            StartCoroutine(StopReelsRoutine(predeterminedOutcomes));
        }

        private IEnumerator StopReelsRoutine(SymbolData[] predeterminedOutcomes)
        {
            for (int i = 0; i < reels.Length && i < predeterminedOutcomes.Length; i++)
            {
                if (reels[i] != null)
                {
                    reels[i].StopAt(predeterminedOutcomes[i]);
                }
                yield return new WaitForSeconds(stopDelayBetweenReels);
            }

            if (OnAllReelsStopped != null)
            {
                OnAllReelsStopped();
            }
        }
    }
}
