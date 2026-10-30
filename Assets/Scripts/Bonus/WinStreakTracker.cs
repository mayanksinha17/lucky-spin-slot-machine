#pragma warning disable 0649, 0169, 0414
using System;
using UnityEngine;
using LuckySpin.SlotMachine.Gameplay;
using LuckySpin.SlotMachine.Audio;

namespace LuckySpin.SlotMachine.Bonus
{
    /// <summary>
    /// Tracks consecutive winning spins, awards dynamic bonus multipliers,
    /// triggers celebratory streak fanfares, and maintains player streak records.
    /// </summary>
    public class WinStreakTracker : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private AudioManager audioManager;

        [Header("Streak Multiplier Table")]
        [Tooltip("Multiplier for 1 win (base).")]
        [SerializeField] private float streak1Multiplier = 1.0f;

        [Tooltip("Multiplier for 2 consecutive wins.")]
        [SerializeField] private float streak2Multiplier = 1.25f;

        [Tooltip("Multiplier for 3 consecutive wins.")]
        [SerializeField] private float streak3Multiplier = 1.50f;

        [Tooltip("Multiplier for 4+ consecutive wins.")]
        [SerializeField] private float streak4PlusMultiplier = 2.0f;

        [Header("Runtime State")]
        [SerializeField] private int currentStreak = 0;
        [SerializeField] private int bestStreak = 0;
        [SerializeField] private int totalWinsTracked = 0;

        public int CurrentStreak { get { return currentStreak; } }
        public int BestStreak { get { return bestStreak; } }
        public int TotalWinsTracked { get { return totalWinsTracked; } }

        public event Action<int, float> OnStreakUpdated;
        public event Action OnStreakBroken;
        public event Action<int, string> OnMilestoneReached;

        public void Initialize(AudioManager am = null)
        {
            audioManager = am;
            currentStreak = 0;
            bestStreak = 0;
            totalWinsTracked = 0;
        }

        /// <summary>
        /// Evaluates a completed spin result to update the active win streak.
        /// </summary>
        public float RecordResult(bool isWin)
        {
            if (isWin)
            {
                currentStreak++;
                totalWinsTracked++;
                if (currentStreak > bestStreak)
                {
                    bestStreak = currentStreak;
                }

                float multiplier = GetStreakMultiplier(currentStreak);

                if (OnStreakUpdated != null)
                {
                    OnStreakUpdated(currentStreak, multiplier);
                }

                if (currentStreak >= 2)
                {
                    if (audioManager != null)
                    {
                        audioManager.PlayStreakFanfare();
                    }

                    string milestone = string.Format("STREAK x{0}! ({1:0.##}x Payout Boost)", currentStreak, multiplier);
                    if (OnMilestoneReached != null)
                    {
                        OnMilestoneReached(currentStreak, milestone);
                    }
                }

                return multiplier;
            }
            else
            {
                if (currentStreak > 0)
                {
                    currentStreak = 0;
                    if (OnStreakBroken != null)
                    {
                        OnStreakBroken();
                    }
                }
                return 1.0f;
            }
        }

        public float GetStreakMultiplier(int streak)
        {
            if (streak <= 1) return streak1Multiplier;
            if (streak == 2) return streak2Multiplier;
            if (streak == 3) return streak3Multiplier;
            return streak4PlusMultiplier;
        }

        public void ResetStreak()
        {
            currentStreak = 0;
            if (OnStreakBroken != null)
            {
                OnStreakBroken();
            }
        }
    }
}
