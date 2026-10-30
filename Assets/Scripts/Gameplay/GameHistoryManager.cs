#pragma warning disable 0649, 0169, 0414
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LuckySpin.SlotMachine.Gameplay
{
    /// <summary>
    /// Tracks recent spin results and provides data to the UI history drawer.
    /// </summary>
    public class GameHistoryManager : MonoBehaviour
    {
        [Tooltip("Maximum number of spin records to retain.")]
        [SerializeField] private int maxHistoryCount = 10;

        private readonly List<SpinRecord> history = new List<SpinRecord>();
        private int totalSpins = 0;

        public List<SpinRecord> History { get { return history; } }
        public int TotalSpins { get { return totalSpins; } }

        public event Action<SpinRecord> OnSpinRecorded;
        public event Action OnHistoryCleared;

        public void RecordSpin(SpinRecord record)
        {
            totalSpins++;
            record.SpinNumber = totalSpins;

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
            if (OnHistoryCleared != null)
            {
                OnHistoryCleared();
            }
        }
    }
}
