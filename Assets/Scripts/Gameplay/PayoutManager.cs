#pragma warning disable 0649, 0169, 0414
using System;
using UnityEngine;

namespace LuckySpin.SlotMachine.Gameplay
{
    /// <summary>
    /// Manages the virtual currency economy: starting balance, bet adjustments, validation, and payout distribution.
    /// Formula: Payout = Bet Amount * Symbol Multiplier
    /// </summary>
    public class PayoutManager : MonoBehaviour
    {
        [Header("Economy Configuration")]
        [Tooltip("Starting virtual credit balance.")]
        [SerializeField] private int startingBalance = 1000;

        [Tooltip("Current bet per spin.")]
        [SerializeField] private int currentBet = 10;

        [Tooltip("Minimum allowable bet.")]
        [SerializeField] private int minBet = 10;

        [Tooltip("Maximum allowable bet.")]
        [SerializeField] private int maxBet = 100;

        [Tooltip("Step increment for bet adjustments.")]
        [SerializeField] private int betStep = 10;

        [Header("Runtime State")]
        [SerializeField] private int currentBalance;

        public int CurrentBalance { get { return currentBalance; } }
        public int CurrentBet { get { return currentBet; } }
        public int MinBet { get { return minBet; } }
        public int MaxBet { get { return maxBet; } }
        public int BetStep { get { return betStep; } }

        public event Action<int> OnBalanceChanged;
        public event Action<int> OnBetChanged;
        public event Action<int> OnPayoutAwarded;
        public event Action OnInsufficientFunds;

        private void Awake()
        {
            currentBalance = startingBalance;
        }

        /// <summary>
        /// Explicit initialization for configuration and automated testing.
        /// </summary>
        public void Initialize(int startBal, int defaultBet, int minimumBet, int maximumBet, int step)
        {
            startingBalance = startBal;
            minBet = minimumBet;
            maxBet = maximumBet;
            betStep = step;
            currentBet = Mathf.Clamp(defaultBet, minBet, maxBet);
            currentBalance = startingBalance;
        }

        /// <summary>
        /// Validates that the player has sufficient credits before a spin begins.
        /// </summary>
        public bool HasSufficientCredits()
        {
            return currentBalance >= currentBet;
        }

        /// <summary>
        /// Deducts the current bet from player balance if credits are sufficient.
        /// </summary>
        public bool TryDeductBet()
        {
            if (!HasSufficientCredits())
            {
                if (OnInsufficientFunds != null)
                {
                    OnInsufficientFunds();
                }
                return false;
            }

            currentBalance -= currentBet;
            if (OnBalanceChanged != null)
            {
                OnBalanceChanged(currentBalance);
            }
            return true;
        }

        /// <summary>
        /// Calculates and credits payout to player balance based on evaluation result.
        /// Formula: Payout = Bet Amount * Symbol Multiplier
        /// </summary>
        public int AwardPayout(EvaluationResult result)
        {
            if (!result.IsWin || result.Payout <= 0)
            {
                return 0;
            }

            currentBalance += result.Payout;

            if (OnBalanceChanged != null)
            {
                OnBalanceChanged(currentBalance);
            }

            if (OnPayoutAwarded != null)
            {
                OnPayoutAwarded(result.Payout);
            }

            return result.Payout;
        }

        /// <summary>
        /// Adjusts bet amount within [minBet, maxBet] limits.
        /// </summary>
        public bool SetBet(int newBet)
        {
            int clamped = Mathf.Clamp(newBet, minBet, maxBet);
            if (clamped != currentBet)
            {
                currentBet = clamped;
                if (OnBetChanged != null)
                {
                    OnBetChanged(currentBet);
                }
                return true;
            }
            return false;
        }

        public bool IncreaseBet()
        {
            if (currentBet + betStep <= maxBet)
            {
                return SetBet(currentBet + betStep);
            }
            return false;
        }

        public bool DecreaseBet()
        {
            if (currentBet - betStep >= minBet)
            {
                return SetBet(currentBet - betStep);
            }
            return false;
        }

        public void SetMaxBet()
        {
            SetBet(maxBet);
        }

        public void ResetBalance(int amount)
        {
            currentBalance = amount;
            if (OnBalanceChanged != null)
            {
                OnBalanceChanged(currentBalance);
            }
        }
    }
}
