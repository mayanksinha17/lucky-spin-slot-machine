#pragma warning disable 0649, 0169, 0414
using System;
using UnityEngine;
using LuckySpin.SlotMachine.Data;
using LuckySpin.SlotMachine.Reels;

namespace LuckySpin.SlotMachine.Gameplay
{
    /// <summary>
    /// Coordinates the three reels, triggering spins, managing sequential stop delays, and reporting completion.
    /// </summary>
    public class SlotMachineController : MonoBehaviour
    {
        [Header("Reel References")]
        [Tooltip("Ordered references to the 3 reel controllers.")]
        [SerializeField] private ReelController[] reels = new ReelController[3];

        [Header("Timing Configuration")]
        [Tooltip("Delay in seconds between stopping consecutive reels.")]
        [SerializeField] private float stopDelayBetweenReels = 0.4f;

        public ReelController[] Reels { get { return reels; } }
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

        public event Action OnAllReelsStopped;

        public void SpinReels(SymbolData[] predeterminedOutcomes)
        {
            if (reels == null) return;
            for (int i = 0; i < reels.Length; i++)
            {
                if (reels[i] != null)
                {
                    reels[i].StartSpinning();
                }
            }
        }

        public void StopReels(SymbolData[] predeterminedOutcomes)
        {
            if (reels == null || predeterminedOutcomes == null) return;
            for (int i = 0; i < reels.Length && i < predeterminedOutcomes.Length; i++)
            {
                if (reels[i] != null)
                {
                    reels[i].StopAt(predeterminedOutcomes[i]);
                }
            }
            if (OnAllReelsStopped != null)
            {
                OnAllReelsStopped();
            }
        }
    }
}
