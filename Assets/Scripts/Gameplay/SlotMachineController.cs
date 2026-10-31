#pragma warning disable 0649, 0169, 0414
using System;
using System.Collections;
using UnityEngine;
using LuckySpin.SlotMachine.Data;
using LuckySpin.SlotMachine.Reels;

namespace LuckySpin.SlotMachine.Gameplay
{
    /// <summary>
    /// Coordinates the three independent reels, executing staggered sequential stops,
    /// tracking active spin animation state, and firing synchronization events.
    /// </summary>
    public class SlotMachineController : MonoBehaviour
    {
        [Header("Reel Controllers")]
        [Tooltip("Ordered references to the 3 reel controllers (0 = Left, 1 = Center, 2 = Right).")]
        [SerializeField] private ReelController[] reels = new ReelController[3];

        [Header("Timing Configuration")]
        [Tooltip("Minimum duration in seconds that all reels spin before the first reel starts stopping.")]
        [SerializeField] private float baseSpinDuration = 1.2f;

        [Tooltip("Staggered delay in seconds between stopping consecutive reels.")]
        [SerializeField] private float stopDelayBetweenReels = 0.38f;

        [Header("Symbol Configuration")]
        [SerializeField] private SymbolDatabase symbolDatabase;

        private Coroutine orchestrationRoutine;

        public ReelController[] Reels { get { return reels; } }
        public int ReelCount { get { return reels != null ? reels.Length : 0; } }

        public void SetReels(ReelController[] r)
        {
            reels = r;
            InitializeReelListeners();
        }

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
        /// Immediately stops all reels at their predetermined outcomes for testing and instantaneous settlement.
        /// </summary>
        public void StopReelsImmediately(SymbolData[] predeterminedOutcomes)
        {
            if (orchestrationRoutine != null) StopCoroutine(orchestrationRoutine);
            orchestrationRoutine = null;

            if (reels != null && predeterminedOutcomes != null)
            {
                for (int i = 0; i < reels.Length; i++)
                {
                    if (reels[i] != null && i < predeterminedOutcomes.Length)
                    {
                        reels[i].StopImmediately(predeterminedOutcomes[i]);
                    }
                }
            }

            if (OnAllReelsStopped != null)
            {
                OnAllReelsStopped();
            }
        }

        /// <summary>
        /// Orchestrates sequential reel deceleration and stopping at predetermined outcomes.
        /// </summary>
        public void StopReels(SymbolData[] predeterminedOutcomes)
        {
            if (orchestrationRoutine != null) StopCoroutine(orchestrationRoutine);
            orchestrationRoutine = StartCoroutine(OrchestrateReelStops(predeterminedOutcomes));
        }

        private IEnumerator OrchestrateReelStops(SymbolData[] predeterminedOutcomes)
        {
            if (reels == null || predeterminedOutcomes == null) yield break;

            // 1. Initial full-speed spin duration
            yield return new WaitForSeconds(baseSpinDuration);

            // 2. Stop Reel 0 (Left)
            if (reels.Length > 0 && reels[0] != null && predeterminedOutcomes.Length > 0)
            {
                reels[0].StopAt(predeterminedOutcomes[0]);
            }

            // Staggered delay before Reel 1
            yield return new WaitForSeconds(stopDelayBetweenReels);

            // 3. Stop Reel 1 (Center)
            if (reels.Length > 1 && reels[1] != null && predeterminedOutcomes.Length > 1)
            {
                reels[1].StopAt(predeterminedOutcomes[1]);
            }

            // Staggered delay before Reel 2
            yield return new WaitForSeconds(stopDelayBetweenReels);

            // 4. Stop Reel 2 (Right) and wait for it to settle
            bool finalReelSettled = false;
            if (reels.Length > 2 && reels[2] != null && predeterminedOutcomes.Length > 2)
            {
                reels[2].StopAt(predeterminedOutcomes[2], delegate {
                    finalReelSettled = true;
                });
            }
            else
            {
                finalReelSettled = true;
            }

            while (!finalReelSettled && reels.Length > 2 && reels[2] != null && reels[2].IsSpinning)
            {
                yield return null;
            }

            // Small settle pause before resolving payouts
            yield return new WaitForSeconds(0.1f);

            if (OnAllReelsStopped != null)
            {
                OnAllReelsStopped();
            }
        }
    }
}
