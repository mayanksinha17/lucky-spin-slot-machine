#pragma warning disable 0649, 0169, 0414
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LuckySpin.SlotMachine.Gameplay
{
    /// <summary>
    /// Tracks recent spin results, provides data to the UI history drawer,
    /// and calculates performance statistics (win rate %, biggest win, total volume).
    /// </summary>
    public class GameHistoryManager : MonoBehaviour
    {
        [Tooltip("Maximum number of spin records to retain.")]
        [SerializeField] private int maxHistoryCount = 10;

        private readonly List<SpinRecord> history = new List<SpinRecord>();
        private int totalSpins = 0;
        private int totalWins = 0;
        private int totalLosses = 0;
        private int biggestWin = 0;
        private int biggestWinSpinNumber = 0;

        public List<SpinRecord> History { get { return history; } }
        public int TotalSpins { get { return totalSpins; } }
        public int TotalWins { get { return totalWins; } }
        public int TotalLosses { get { return totalLosses; } }
        public int BiggestWin { get { return biggestWin; } }
        public int BiggestWinSpinNumber { get { return biggestWinSpinNumber; } }

        public float WinRatePercentage
        {
            get
            {
                if (totalSpins == 0) return 0f;
                return ((float)totalWins / totalSpins) * 100f;
            }
        }

        public event Action<SpinRecord> OnSpinRecorded;
        public event Action OnHistoryCleared;

        public void RecordSpin(SpinRecord record)
        {
            totalSpins++;
            record.SpinNumber = totalSpins;

            if (record.IsWin)
            {
                totalWins++;
                if (record.PayoutAwarded > biggestWin)
                {
                    biggestWin = record.PayoutAwarded;
                    biggestWinSpinNumber = totalSpins;
                }
            }
            else
            {
                totalLosses++;
            }

            history.Insert(0, record); // Most recent first
            if (history.Count > maxHistoryCount)
            {
                history.RemoveAt(history.Count - 1);
            }

            if (OnSpinRecorded != null)
            {
                OnSpinRecorded(record);
            }
        }

        public void ClearHistory()
        {
            history.Clear();
            totalSpins = 0;
            totalWins = 0;
            totalLosses = 0;
            biggestWin = 0;
            biggestWinSpinNumber = 0;

            if (OnHistoryCleared != null)
            {
                OnHistoryCleared();
            }
        }

        public string GetPerformanceSummary()
        {
            return string.Format("SPINS: {0} | WINS: {1} ({2:0.0}%) | BEST: +{3} G",
                totalSpins, totalWins, WinRatePercentage, biggestWin);
        }
    }
}
