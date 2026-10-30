#pragma warning disable 0649, 0169, 0414
using System;
using System.Collections;
using UnityEngine;
using LuckySpin.SlotMachine.Core;
using LuckySpin.SlotMachine.Gameplay;

namespace LuckySpin.SlotMachine.Bonus
{
    public enum AutoSpinStopReason
    {
        Completed = 0,
        UserCancelled = 1,
        InsufficientCredits = 2,
        TargetWinReached = 3,
        JackpotHit = 4
    }

    /// <summary>
    /// Autonomous Auto-Spin Coordinator.
    /// Manages recurring spin execution, enforces credit validation, strictly prevents overlapping spins,
    /// and halts automatically upon user cancellation, completion, or reaching configured thresholds.
    /// </summary>
    public class AutoSpinController : MonoBehaviour
    {
        [Header("Auto-Spin Configuration")]
        [Tooltip("Delay in seconds between settling a spin and dispatching the next auto-spin.")]
        [SerializeField] private float spinInterval = 0.8f;

        [Tooltip("When true, auto-spin halts on any winning combination.")]
        [SerializeField] private bool stopOnAnyWin = false;

        [Tooltip("Single-spin payout threshold that triggers an emergency jackpot halt (0 = disabled).")]
        [SerializeField] private int jackpotStopThreshold = 1000;

        [Header("Dependencies")]
        [SerializeField] private GameManager gameManager;
        [SerializeField] private PayoutManager payoutManager;

        [Header("Runtime State")]
        [SerializeField] private bool isActive = false;
        [SerializeField] private int remainingSpins = 0;
        [SerializeField] private int totalSpinsRequested = 0;

        private Coroutine autoSpinRoutine = null;

        public bool IsActive { get { return isActive; } }
        public int RemainingSpins { get { return remainingSpins; } }
        public int TotalSpinsRequested { get { return totalSpinsRequested; } }
        public float SpinInterval { get { return spinInterval; } set { spinInterval = Mathf.Max(0.1f, value); } }
        public bool StopOnAnyWin { get { return stopOnAnyWin; } set { stopOnAnyWin = value; } }
        public int JackpotStopThreshold { get { return jackpotStopThreshold; } set { jackpotStopThreshold = value; } }

        public event Action<int> OnAutoSpinStarted;
        public event Action<int> OnAutoSpinCountUpdated;
        public event Action<AutoSpinStopReason> OnAutoSpinStopped;

        public void Initialize(GameManager gm, PayoutManager pm)
        {
            gameManager = gm;
            payoutManager = pm;
        }

        /// <summary>
        /// Begins an autonomous spin session for the designated count.
        /// </summary>
        public bool StartAutoSpin(int spinCount, bool stopOnWin = false, int jackpotThreshold = 1000)
        {
            if (isActive) return false;
            if (spinCount <= 0) return false;

            // Validate player has sufficient credits for at least the initial spin
            if (payoutManager != null && !payoutManager.HasSufficientCredits())
            {
                if (OnAutoSpinStopped != null)
                {
                    OnAutoSpinStopped(AutoSpinStopReason.InsufficientCredits);
                }
                return false;
            }

            isActive = true;
            remainingSpins = spinCount;
            totalSpinsRequested = spinCount;
            stopOnAnyWin = stopOnWin;
            jackpotStopThreshold = jackpotThreshold;

            if (OnAutoSpinStarted != null)
            {
                OnAutoSpinStarted(totalSpinsRequested);
            }
            if (OnAutoSpinCountUpdated != null)
            {
                OnAutoSpinCountUpdated(remainingSpins);
            }

            if (autoSpinRoutine != null) StopCoroutine(autoSpinRoutine);
            autoSpinRoutine = StartCoroutine(AutoSpinLoop());

            return true;
        }

        /// <summary>
        /// Manually or programmatically halts the auto-spin session.
        /// </summary>
        public void StopAutoSpin(AutoSpinStopReason reason = AutoSpinStopReason.UserCancelled)
        {
            if (!isActive) return;

            isActive = false;
            remainingSpins = 0;

            if (autoSpinRoutine != null)
            {
                StopCoroutine(autoSpinRoutine);
                autoSpinRoutine = null;
            }

            if (OnAutoSpinStopped != null)
            {
                OnAutoSpinStopped(reason);
            }
        }

        private IEnumerator AutoSpinLoop()
        {
            while (isActive && remainingSpins > 0)
            {
                // 1. Concurrency Guard: Ensure game manager is strictly in Idle state (never overlap active spins)
                if (gameManager != null)
                {
                    while (gameManager.CurrentState != GameState.Idle)
                    {
                        yield return null;
                    }
                }

                // 2. Credit Validation: Check credits prior to dispatching each spin
                if (payoutManager != null && !payoutManager.HasSufficientCredits())
                {
                    StopAutoSpin(AutoSpinStopReason.InsufficientCredits);
                    yield break;
                }

                // 3. Dispatch Spin
                remainingSpins--;
                if (OnAutoSpinCountUpdated != null)
                {
                    OnAutoSpinCountUpdated(remainingSpins);
                }

                if (gameManager != null)
                {
                    gameManager.RequestSpin();
                }

                // Wait until the spin starts and finishes returning to Idle
                yield return null;
                if (gameManager != null)
                {
                    while (gameManager.CurrentState != GameState.Idle)
                    {
                        yield return null;
                    }
                }

                // 4. Check Post-Spin Halt Conditions
                if (gameManager != null && gameManager.LastResult.IsWin)
                {
                    if (stopOnAnyWin)
                    {
                        StopAutoSpin(AutoSpinStopReason.TargetWinReached);
                        yield break;
                    }

                    if (jackpotStopThreshold > 0 && gameManager.LastResult.Payout >= jackpotStopThreshold)
                    {
                        StopAutoSpin(AutoSpinStopReason.JackpotHit);
                        yield break;
                    }
                }

                // 5. Arcade Pacing Interval between spins
                if (remainingSpins > 0)
                {
                    yield return new WaitForSeconds(spinInterval);
                }
            }

            if (isActive)
            {
                StopAutoSpin(AutoSpinStopReason.Completed);
            }
        }

        /// <summary>
        /// Synchronous single-step execution utility for headless unit test simulation.
        /// </summary>
        public bool SimulateTestStep(out AutoSpinStopReason? stopReason)
        {
            stopReason = null;
            if (!isActive || remainingSpins <= 0) return false;

            if (payoutManager != null && !payoutManager.HasSufficientCredits())
            {
                stopReason = AutoSpinStopReason.InsufficientCredits;
                StopAutoSpin(AutoSpinStopReason.InsufficientCredits);
                return false;
            }

            remainingSpins--;
            if (OnAutoSpinCountUpdated != null)
            {
                OnAutoSpinCountUpdated(remainingSpins);
            }

            if (gameManager != null && gameManager.CurrentState == GameState.Idle)
            {
                gameManager.RequestSpin();
            }

            if (remainingSpins <= 0)
            {
                stopReason = AutoSpinStopReason.Completed;
                StopAutoSpin(AutoSpinStopReason.Completed);
            }

            return true;
        }
    }
}
