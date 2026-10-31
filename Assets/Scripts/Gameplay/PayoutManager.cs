#pragma warning disable 0649, 0169, 0414
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LuckySpin.SlotMachine.Gameplay
{
    /// <summary>
    /// Financial transaction record within the centralized virtual credit ledger.
    /// </summary>
    [Serializable]
    public class CreditTransaction
    {
        public string TransactionId;
        public int BetAmount;
        public int PayoutAmount;
        public int BalanceBefore;
        public int BalanceAfter;
        public bool IsWin;
        public bool IsSettled;
        public DateTime Timestamp;

        public int NetProfitLoss { get { return PayoutAmount - BetAmount; } }

        public CreditTransaction(string id, int bet, int balanceBefore)
        {
            TransactionId = id;
            BetAmount = bet;
            PayoutAmount = 0;
            BalanceBefore = balanceBefore;
            BalanceAfter = balanceBefore - bet;
            IsWin = false;
            IsSettled = false;
            Timestamp = DateTime.Now;
        }

        public void Settle(int payout, int finalBalance, bool isWin)
        {
            PayoutAmount = payout;
            BalanceAfter = finalBalance;
            IsWin = isWin;
            IsSettled = true;
        }
    }

    /// <summary>
    /// Centralized Virtual Credit Manager & Economy Engine.
    /// Enforces single-source-of-truth for credit deductions, payout calculations,
    /// transaction locking to prevent duplicate operations, and edge-case validations.
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
        [SerializeField] private int totalSpinsDebited = 0;
        [SerializeField] private int totalBetsPlaced = 0;
        [SerializeField] private int totalPayoutsAwarded = 0;

        private CreditTransaction activeTransaction = null;
        private readonly List<CreditTransaction> transactionLedger = new List<CreditTransaction>();
        private long transactionCounter = 0;

        public int CurrentBalance { get { return currentBalance; } }
        public int CurrentBet { get { return currentBet; } }
        public int MinBet { get { return minBet; } }
        public int MaxBet { get { return maxBet; } }
        public int BetStep { get { return betStep; } }
        public int StartingBalance { get { return startingBalance; } }
        public int TotalSpinsDebited { get { return totalSpinsDebited; } }
        public int TotalBetsPlaced { get { return totalBetsPlaced; } }
        public int TotalPayoutsAwarded { get { return totalPayoutsAwarded; } }
        public int NetLifetimeProfitLoss { get { return totalPayoutsAwarded - totalBetsPlaced; } }
        public bool HasActiveTransaction { get { return activeTransaction != null && !activeTransaction.IsSettled; } }
        public CreditTransaction ActiveTransaction { get { return activeTransaction; } }
        public List<CreditTransaction> Ledger { get { return transactionLedger; } }

        public event Action<int> OnBalanceChanged;
        public event Action<int, int> OnBalanceDeltaChanged;
        public event Action<int> OnBetChanged;
        public event Action<int> OnPayoutAwarded;
        public event Action OnInsufficientFunds;
        public event Action<int, int> OnInsufficientFundsDetailed;
        public event Action<CreditTransaction> OnTransactionSettled;

        private void Awake()
        {
            currentBalance = startingBalance;
        }

        /// <summary>
        /// Explicit initialization for setup, resets, and automated unit testing.
        /// </summary>
        public void Initialize(int startBal, int defaultBet, int minimumBet, int maximumBet, int step)
        {
            startingBalance = Mathf.Max(0, startBal);
            minBet = Mathf.Max(1, minimumBet);
            maxBet = Mathf.Max(minBet, maximumBet);
            betStep = Mathf.Max(1, step);
            currentBet = Mathf.Clamp(defaultBet, minBet, maxBet);
            currentBalance = startingBalance;
            activeTransaction = null;
            transactionLedger.Clear();
            totalSpinsDebited = 0;
            totalBetsPlaced = 0;
            totalPayoutsAwarded = 0;
        }

        /// <summary>
        /// Validates that the player has sufficient credits before spinning.
        /// </summary>
        public bool HasSufficientCredits()
        {
            return currentBalance >= currentBet;
        }

        /// <summary>
        /// Validates if a specific bet amount can be afforded.
        /// </summary>
        public bool CanAffordBet(int betAmount)
        {
            return betAmount > 0 && currentBalance >= betAmount;
        }

        /// <summary>
        /// Atomically debits the current bet from player balance and locks the transaction.
        /// Prevents concurrent spins and duplicate deductions.
        /// </summary>
        public bool TryDeductBet()
        {
            // 1. Concurrency Check: Prevent duplicate debit if a transaction is currently in-flight
            if (HasActiveTransaction)
            {
                Debug.LogWarning("[PayoutManager] Spin rejected: Active unsettled transaction already in progress.");
                return false;
            }

            // 2. Insufficient Credit Validation
            if (!HasSufficientCredits())
            {
                if (OnInsufficientFunds != null)
                {
                    OnInsufficientFunds();
                }
                if (OnInsufficientFundsDetailed != null)
                {
                    OnInsufficientFundsDetailed(currentBet, currentBalance);
                }
                return false;
            }

            // 3. Bet Validity Validation
            if (currentBet < minBet || currentBet > maxBet)
            {
                Debug.LogError(string.Format("[PayoutManager] Invalid bet amount {0}. Must be between {1} and {2}.", currentBet, minBet, maxBet));
                return false;
            }

            // 4. Atomic Balance Deduction
            transactionCounter++;
            string txnId = string.Format("TXN_{0}_{1:yyyyMMddHHmmss}", transactionCounter, DateTime.Now);
            int balanceBefore = currentBalance;
            currentBalance -= currentBet;
            totalSpinsDebited++;
            totalBetsPlaced += currentBet;

            activeTransaction = new CreditTransaction(txnId, currentBet, balanceBefore);

            if (OnBalanceChanged != null)
            {
                OnBalanceChanged(currentBalance);
            }
            if (OnBalanceDeltaChanged != null)
            {
                OnBalanceDeltaChanged(currentBalance, -currentBet);
            }

            return true;
        }

        /// <summary>
        /// Calculates and credits payout to player balance based on evaluation result.
        /// Formula: Payout = Bet Amount * Symbol Multiplier
        /// Prevents duplicate payout claims by settling the active transaction.
        /// </summary>
        public int AwardPayout(EvaluationResult result)
        {
            // If no active transaction, reject unauthorized payout claim
            if (activeTransaction == null)
            {
                Debug.LogWarning("[PayoutManager] Attempted to award payout with no active transaction.");
                return 0;
            }

            if (activeTransaction.IsSettled)
            {
                Debug.LogWarning("[PayoutManager] Attempted to award payout on an already-settled transaction.");
                return 0;
            }

            int payout = 0;
            if (result.IsWin && result.Payout > 0)
            {
                // Verify formula: Payout = activeTransaction.BetAmount * Multiplier
                int multiplier = result.WinningSymbol != null ? result.WinningSymbol.PayoutMultiplier : 0;
                payout = activeTransaction.BetAmount * multiplier;

                currentBalance += payout;
                totalPayoutsAwarded += payout;

                if (OnBalanceChanged != null)
                {
                    OnBalanceChanged(currentBalance);
                }
                if (OnBalanceDeltaChanged != null)
                {
                    OnBalanceDeltaChanged(currentBalance, payout);
                }
                if (OnPayoutAwarded != null)
                {
                    OnPayoutAwarded(payout);
                }
            }

            // Complete and settle the transaction
            activeTransaction.Settle(payout, currentBalance, result.IsWin);
            transactionLedger.Add(activeTransaction);

            if (OnTransactionSettled != null)
            {
                OnTransactionSettled(activeTransaction);
            }

            CreditTransaction settled = activeTransaction;
            activeTransaction = null; // Clear active transaction lock

            return settled.PayoutAmount;
        }

        /// <summary>
        /// Cancels and refunds an active unsettled spin transaction (e.g. on emergency reset or abort).
        /// </summary>
        public bool CancelAndRefundActiveTransaction()
        {
            if (!HasActiveTransaction) return false;

            currentBalance += activeTransaction.BetAmount;
            totalBetsPlaced -= activeTransaction.BetAmount;
            totalSpinsDebited--;

            if (OnBalanceChanged != null)
            {
                OnBalanceChanged(currentBalance);
            }
            if (OnBalanceDeltaChanged != null)
            {
                OnBalanceDeltaChanged(currentBalance, activeTransaction.BetAmount);
            }

            activeTransaction = null;
            return true;
        }

        /// <summary>
        /// Adjusts bet amount within [minBet, maxBet] limits, snapped to bet step, capped to available balance.
        /// </summary>
        public bool SetBet(int newBet)
        {
            // Do not permit bet changes during an active spin
            if (HasActiveTransaction) return false;

            int clamped = Mathf.Clamp(newBet, minBet, maxBet);
            // Snap to nearest step multiple relative to minBet
            int remainder = (clamped - minBet) % betStep;
            if (remainder != 0)
            {
                clamped -= remainder;
            }
            clamped = Mathf.Clamp(clamped, minBet, maxBet);

            // Cap bet to available balance if balance allows at least minBet
            if (clamped > currentBalance && currentBalance >= minBet)
            {
                int affordableSteps = (currentBalance - minBet) / betStep;
                clamped = minBet + (affordableSteps * betStep);
            }
            else if (currentBalance < minBet)
            {
                clamped = minBet;
            }

            if (clamped != currentBet)
            {
                currentBet = clamped;
                if (OnBetChanged != null)
                {
                    OnBetChanged(currentBet);
                }
                return true;
            }
            else
            {
                // Refresh UI even if value didn't change so preset highlights stay synchronized
                if (OnBetChanged != null)
                {
                    OnBetChanged(currentBet);
                }
            }
            return false;
        }

        public bool IncreaseBet()
        {
            if (HasActiveTransaction) return false;
            int target = currentBet + betStep;
            if (target > maxBet) target = maxBet;
            if (target > currentBalance && currentBalance >= minBet)
            {
                int affordableSteps = (currentBalance - minBet) / betStep;
                target = minBet + (affordableSteps * betStep);
            }
            if (target > currentBet)
            {
                return SetBet(target);
            }
            return false;
        }

        public bool DecreaseBet()
        {
            if (HasActiveTransaction) return false;
            int target = currentBet - betStep;
            if (target < minBet) target = minBet;
            if (target < currentBet)
            {
                return SetBet(target);
            }
            return false;
        }

        public void SetMaxBet()
        {
            if (HasActiveTransaction) return;
            int maxAffordable = Mathf.Min(maxBet, currentBalance);
            if (maxAffordable >= minBet)
            {
                int affordableSteps = (maxAffordable - minBet) / betStep;
                int target = minBet + (affordableSteps * betStep);
                SetBet(target);
            }
            else
            {
                SetBet(minBet);
            }
        }

        /// <summary>
        /// Automatically adjusts the player's bet downward if their balance can no longer afford the current bet.
        /// </summary>
        public bool AutoAdjustBetToAffordable()
        {
            if (HasActiveTransaction) return false;
            if (currentBalance >= currentBet) return false;

            if (currentBalance < minBet)
            {
                // Cannot afford even minimum bet
                return false;
            }

            // Find highest affordable bet step
            int affordableSteps = (currentBalance - minBet) / betStep;
            int newBet = minBet + (affordableSteps * betStep);
            return SetBet(newBet);
        }

        /// <summary>
        /// Resets player virtual credits to a designated amount (e.g. reload 1,000 demo credits).
        /// </summary>
        public void ResetBalance(int amount)
        {
            int oldBalance = currentBalance;
            currentBalance = Mathf.Max(0, amount);
            activeTransaction = null;

            if (OnBalanceChanged != null)
            {
                OnBalanceChanged(currentBalance);
            }
            if (OnBalanceDeltaChanged != null)
            {
                OnBalanceDeltaChanged(currentBalance, currentBalance - oldBalance);
            }
        }
    }
}
