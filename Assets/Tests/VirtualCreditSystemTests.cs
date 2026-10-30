#pragma warning disable 0649, 0169, 0414
using System;
using System.Collections.Generic;
using UnityEngine;
using LuckySpin.SlotMachine.Data;
using LuckySpin.SlotMachine.Gameplay;

namespace LuckySpin.SlotMachine.Tests
{
    /// <summary>
    /// Automated test suite verifying the complete virtual credit system:
    /// centralized balance operations, bet bounds, insufficient credit validation,
    /// atomic deduction, payout formula calculation, transaction locking, duplicate prevention, and history auditing.
    /// </summary>
    public class VirtualCreditSystemTests
    {
        public struct TestReport
        {
            public int TestsRun;
            public int TestsPassed;
            public int TestsFailed;
        }

        public static TestReport RunAllTests()
        {
            TestReport report = new TestReport();

            RunTest("StartingBalance_InitializedAccurately", Test_StartingBalance_InitializedAccurately, ref report);
            RunTest("BetAdjustments_WithinMinMaxAndStepBounds", Test_BetAdjustments_WithinMinMaxAndStepBounds, ref report);
            RunTest("BetPresets_SetExactValidValues", Test_BetPresets_SetExactValidValues, ref report);
            RunTest("InsufficientCredits_SpinBlockedAndFiresEvent", Test_InsufficientCredits_SpinBlockedAndFiresEvent, ref report);
            RunTest("ExactBalanceSpin_AllowedAndResultsInZeroBalance", Test_ExactBalanceSpin_AllowedAndResultsInZeroBalance, ref report);
            RunTest("CreditDeduction_DeductsExactBetAmount", Test_CreditDeduction_DeductsExactBetAmount, ref report);
            RunTest("PayoutCalculation_FormulaExact_AllMultipliers", Test_PayoutCalculation_FormulaExact_AllMultipliers, ref report);
            RunTest("PayoutAward_CreditsBalanceAccurately", Test_PayoutAward_CreditsBalanceAccurately, ref report);
            RunTest("LossEvaluation_ZeroPayoutAndBalanceSettled", Test_LossEvaluation_ZeroPayoutAndBalanceSettled, ref report);
            RunTest("TransactionLocking_PreventsDuplicateDeduction", Test_TransactionLocking_PreventsDuplicateDeduction, ref report);
            RunTest("TransactionLocking_PreventsDuplicatePayoutClaims", Test_TransactionLocking_PreventsDuplicatePayoutClaims, ref report);
            RunTest("InvalidBetAmount_RejectedGracefully", Test_InvalidBetAmount_RejectedGracefully, ref report);
            RunTest("BalanceReset_RestoresExactDemoBalance", Test_BalanceReset_RestoresExactDemoBalance, ref report);
            RunTest("AutoAdjustBet_StepsDownWhenBalanceLow", Test_AutoAdjustBet_StepsDownWhenBalanceLow, ref report);
            RunTest("TransactionRefund_CancelsActiveTransaction", Test_TransactionRefund_CancelsActiveTransaction, ref report);
            RunTest("SpinHistory_AuditTrailRecordsAllTransactions", Test_SpinHistory_AuditTrailRecordsAllTransactions, ref report);

            return report;
        }

        private static void RunTest(string testName, Action testAction, ref TestReport report)
        {
            report.TestsRun++;
            try
            {
                testAction();
                report.TestsPassed++;
                Debug.Log(string.Format("[PASS] {0}", testName));
            }
            catch (Exception ex)
            {
                report.TestsFailed++;
                Debug.LogError(string.Format("[FAIL] {0}: {1}", testName, ex.Message));
            }
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception("Assertion Failed: " + message);
            }
        }

        private static PayoutManager CreateTestManager(int startBal = 1000, int defBet = 10, int minBet = 10, int maxBet = 100, int step = 10)
        {
            GameObject go = new GameObject("TestPayoutManager");
            PayoutManager pm = go.AddComponent<PayoutManager>();
            pm.Initialize(startBal, defBet, minBet, maxBet, step);
            return pm;
        }

        /// <summary>
        /// 1. Verifies starting balance is initialized cleanly.
        /// </summary>
        public static void Test_StartingBalance_InitializedAccurately()
        {
            PayoutManager pm = CreateTestManager(1000, 10, 10, 100, 10);
            Assert(pm.CurrentBalance == 1000, "Starting balance must be 1000 G");
            Assert(pm.CurrentBet == 10, "Default bet must be 10 G");
            Assert(pm.MinBet == 10, "Min bet must be 10 G");
            Assert(pm.MaxBet == 100, "Max bet must be 100 G");
            Assert(pm.BetStep == 10, "Bet step must be 10 G");
        }

        /// <summary>
        /// 2. Verifies bet increase/decrease respects bounds and steps.
        /// </summary>
        public static void Test_BetAdjustments_WithinMinMaxAndStepBounds()
        {
            PayoutManager pm = CreateTestManager(1000, 10, 10, 100, 10);

            // Cannot decrease below minimum
            bool decreasedBelowMin = pm.DecreaseBet();
            Assert(!decreasedBelowMin, "Cannot decrease bet below minBet");
            Assert(pm.CurrentBet == 10, "Bet should remain at 10");

            // Increase step-by-step
            pm.IncreaseBet();
            Assert(pm.CurrentBet == 20, "Bet should be 20");
            pm.IncreaseBet();
            Assert(pm.CurrentBet == 30, "Bet should be 30");

            // Max bet
            pm.SetMaxBet();
            Assert(pm.CurrentBet == 100, "Bet should be 100 after SetMaxBet");

            // Cannot increase above maximum
            bool increasedAboveMax = pm.IncreaseBet();
            Assert(!increasedAboveMax, "Cannot increase bet above maxBet");
            Assert(pm.CurrentBet == 100, "Bet should remain capped at 100");
        }

        /// <summary>
        /// 3. Verifies quick bet presets (10, 50, 100).
        /// </summary>
        public static void Test_BetPresets_SetExactValidValues()
        {
            PayoutManager pm = CreateTestManager(1000, 10, 10, 100, 10);

            pm.SetBet(50);
            Assert(pm.CurrentBet == 50, "Bet should be 50");

            pm.SetBet(100);
            Assert(pm.CurrentBet == 100, "Bet should be 100");

            pm.SetBet(10);
            Assert(pm.CurrentBet == 10, "Bet should be 10");
        }

        /// <summary>
        /// 4. Edge Case: Insufficient credits blocks spin and fires event.
        /// </summary>
        public static void Test_InsufficientCredits_SpinBlockedAndFiresEvent()
        {
            PayoutManager pm = CreateTestManager(5, 10, 10, 100, 10); // Balance 5G, Bet 10G

            bool eventFired = false;
            int detailReq = 0, detailBal = 0;
            pm.OnInsufficientFundsDetailed += delegate(int req, int bal) {
                eventFired = true;
                detailReq = req;
                detailBal = bal;
            };

            bool deducted = pm.TryDeductBet();
            Assert(!deducted, "TryDeductBet must return false when credits are insufficient");
            Assert(pm.CurrentBalance == 5, "Balance must not be modified when spin is rejected");
            Assert(eventFired, "OnInsufficientFundsDetailed event must fire");
            Assert(detailReq == 10 && detailBal == 5, "Event details must match required bet and balance");
        }

        /// <summary>
        /// 5. Edge Case: Exact balance spin (balance == bet) succeeds, resulting in 0 balance.
        /// </summary>
        public static void Test_ExactBalanceSpin_AllowedAndResultsInZeroBalance()
        {
            PayoutManager pm = CreateTestManager(10, 10, 10, 100, 10);
            Assert(pm.HasSufficientCredits(), "HasSufficientCredits should be true when balance == bet");

            bool deducted = pm.TryDeductBet();
            Assert(deducted, "Deduction must succeed for exact balance");
            Assert(pm.CurrentBalance == 0, "Balance must be exactly 0 after deduction");
        }

        /// <summary>
        /// 6. Verifies exact credit deduction on spin.
        /// </summary>
        public static void Test_CreditDeduction_DeductsExactBetAmount()
        {
            PayoutManager pm = CreateTestManager(1000, 50, 10, 100, 10);
            bool deducted = pm.TryDeductBet();

            Assert(deducted, "Deduction must succeed");
            Assert(pm.CurrentBalance == 950, "Balance must be 950 after deducting 50 from 1000");
            Assert(pm.TotalSpinsDebited == 1, "Total spins debited must equal 1");
            Assert(pm.TotalBetsPlaced == 50, "Total bets placed must equal 50");
        }

        /// <summary>
        /// 7. Verifies Payout Formula: Payout = Bet Amount * Symbol Multiplier across all 5 symbols.
        /// </summary>
        public static void Test_PayoutCalculation_FormulaExact_AllMultipliers()
        {
            SymbolData cherry = ScriptableObject.CreateInstance<SymbolData>();
            cherry.Initialize(SymbolType.Cherry, "Cherry", null, 2, 35);

            SymbolData bell = ScriptableObject.CreateInstance<SymbolData>();
            bell.Initialize(SymbolType.Bell, "Bell", null, 5, 25);

            SymbolData bar = ScriptableObject.CreateInstance<SymbolData>();
            bar.Initialize(SymbolType.Bar, "BAR", null, 10, 20);

            SymbolData seven = ScriptableObject.CreateInstance<SymbolData>();
            seven.Initialize(SymbolType.Seven, "Seven", null, 25, 15);

            SymbolData diamond = ScriptableObject.CreateInstance<SymbolData>();
            diamond.Initialize(SymbolType.Diamond, "Diamond", null, 100, 5);

            // Cherry 2x: Bet 50 -> 100
            PayoutManager pm1 = CreateTestManager(1000, 50, 10, 100, 10);
            pm1.TryDeductBet();
            EvaluationResult resCherry = EvaluationResult.Win(cherry, 50, 2);
            int payoutCherry = pm1.AwardPayout(resCherry);
            Assert(payoutCherry == 100, "Cherry payout: 50 * 2 = 100");

            // Bell 5x: Bet 20 -> 100
            PayoutManager pm2 = CreateTestManager(1000, 20, 10, 100, 10);
            pm2.TryDeductBet();
            EvaluationResult resBell = EvaluationResult.Win(bell, 20, 5);
            int payoutBell = pm2.AwardPayout(resBell);
            Assert(payoutBell == 100, "Bell payout: 20 * 5 = 100");

            // BAR 10x: Bet 10 -> 100
            PayoutManager pm3 = CreateTestManager(1000, 10, 10, 100, 10);
            pm3.TryDeductBet();
            EvaluationResult resBar = EvaluationResult.Win(bar, 10, 10);
            int payoutBar = pm3.AwardPayout(resBar);
            Assert(payoutBar == 100, "BAR payout: 10 * 10 = 100");

            // Seven 25x: Bet 10 -> 250
            PayoutManager pm4 = CreateTestManager(1000, 10, 10, 100, 10);
            pm4.TryDeductBet();
            EvaluationResult resSeven = EvaluationResult.Win(seven, 10, 25);
            int payoutSeven = pm4.AwardPayout(resSeven);
            Assert(payoutSeven == 250, "Seven payout: 10 * 25 = 250");

            // Diamond 100x: Bet 100 -> 10000
            PayoutManager pm5 = CreateTestManager(1000, 100, 10, 100, 10);
            pm5.TryDeductBet();
            EvaluationResult resDiamond = EvaluationResult.Win(diamond, 100, 100);
            int payoutDiamond = pm5.AwardPayout(resDiamond);
            Assert(payoutDiamond == 10000, "Diamond jackpot: 100 * 100 = 10000");
        }

        /// <summary>
        /// 8. Verifies Payout award updates balance accurately.
        /// </summary>
        public static void Test_PayoutAward_CreditsBalanceAccurately()
        {
            PayoutManager pm = CreateTestManager(1000, 10, 10, 100, 10);
            pm.TryDeductBet(); // 1000 -> 990

            SymbolData bell = ScriptableObject.CreateInstance<SymbolData>();
            bell.Initialize(SymbolType.Bell, "Bell", null, 5, 25);
            EvaluationResult win = EvaluationResult.Win(bell, 10, 5);

            int awarded = pm.AwardPayout(win);
            Assert(awarded == 50, "Payout awarded must be 50");
            Assert(pm.CurrentBalance == 1040, "Balance must be 990 + 50 = 1040 G");
            Assert(pm.TotalPayoutsAwarded == 50, "Total payouts awarded must be 50 G");
            Assert(pm.NetLifetimeProfitLoss == 40, "Net profit must be +40 G (50 won - 10 bet)");
        }

        /// <summary>
        /// 9. Verifies losing outcome results in 0 payout and settles transaction cleanly.
        /// </summary>
        public static void Test_LossEvaluation_ZeroPayoutAndBalanceSettled()
        {
            PayoutManager pm = CreateTestManager(1000, 20, 10, 100, 10);
            pm.TryDeductBet(); // 1000 -> 980

            EvaluationResult loss = EvaluationResult.Loss(20);
            int payout = pm.AwardPayout(loss);

            Assert(payout == 0, "Loss must award 0 payout");
            Assert(pm.CurrentBalance == 980, "Balance must remain at debited amount 980 G");
            Assert(!pm.HasActiveTransaction, "Transaction must be settled and cleared");
            Assert(pm.NetLifetimeProfitLoss == -20, "Net lifetime loss must be -20 G");
        }

        /// <summary>
        /// 10. Concurrency & Security: Prevents duplicate debit while a transaction is active.
        /// </summary>
        public static void Test_TransactionLocking_PreventsDuplicateDeduction()
        {
            PayoutManager pm = CreateTestManager(1000, 10, 10, 100, 10);
            bool firstDeduct = pm.TryDeductBet();
            Assert(firstDeduct, "First deduction must succeed");
            Assert(pm.HasActiveTransaction, "PayoutManager must report active transaction");

            // Attempt duplicate deduction while previous spin is still in-flight
            bool secondDeduct = pm.TryDeductBet();
            Assert(!secondDeduct, "Second deduction must be blocked while transaction is active");
            Assert(pm.CurrentBalance == 990, "Balance must not be debited twice");
        }

        /// <summary>
        /// 11. Concurrency & Security: Prevents duplicate payout claims for the same spin.
        /// </summary>
        public static void Test_TransactionLocking_PreventsDuplicatePayoutClaims()
        {
            PayoutManager pm = CreateTestManager(1000, 10, 10, 100, 10);
            pm.TryDeductBet();

            SymbolData cherry = ScriptableObject.CreateInstance<SymbolData>();
            cherry.Initialize(SymbolType.Cherry, "Cherry", null, 2, 35);
            EvaluationResult win = EvaluationResult.Win(cherry, 10, 2);

            int firstClaim = pm.AwardPayout(win);
            Assert(firstClaim == 20, "First claim must award 20");
            Assert(pm.CurrentBalance == 1010, "Balance must be 1010");

            // Second claim on settled transaction
            int secondClaim = pm.AwardPayout(win);
            Assert(secondClaim == 0, "Second claim must be rejected with 0 payout");
            Assert(pm.CurrentBalance == 1010, "Balance must not be credited twice");
        }

        /// <summary>
        /// 12. Edge Case: Invalid bets outside [minBet, maxBet] cannot be set or deducted.
        /// </summary>
        public static void Test_InvalidBetAmount_RejectedGracefully()
        {
            PayoutManager pm = CreateTestManager(1000, 10, 10, 100, 10);

            // Attempt setting bet to negative or 0
            pm.SetBet(0);
            Assert(pm.CurrentBet == 10, "Bet must clamp to minBet (10)");

            // Attempt setting bet above maxBet
            pm.SetBet(500);
            Assert(pm.CurrentBet == 100, "Bet must clamp to maxBet (100)");
        }

        /// <summary>
        /// 13. Verifies balance reset cleanly restores designated virtual credits.
        /// </summary>
        public static void Test_BalanceReset_RestoresExactDemoBalance()
        {
            PayoutManager pm = CreateTestManager(0, 10, 10, 100, 10);
            Assert(pm.CurrentBalance == 0, "Initial balance should be 0");

            pm.ResetBalance(1000);
            Assert(pm.CurrentBalance == 1000, "Balance must be restored to 1000");
            Assert(pm.HasSufficientCredits(), "HasSufficientCredits must be true after reload");
        }

        /// <summary>
        /// 14. Edge Case: Auto-adjust bet downward when player balance cannot afford current bet.
        /// </summary>
        public static void Test_AutoAdjustBet_StepsDownWhenBalanceLow()
        {
            PayoutManager pm = CreateTestManager(1000, 50, 10, 100, 10);
            pm.SetBet(50);

            // Simulate balance dropping to 35 G
            pm.ResetBalance(35);
            Assert(!pm.HasSufficientCredits(), "Cannot afford 50 G with 35 G balance");

            bool adjusted = pm.AutoAdjustBetToAffordable();
            Assert(adjusted, "AutoAdjustBetToAffordable should return true");
            Assert(pm.CurrentBet == 30, "Bet should step down to highest affordable valid bet (30 G)");
            Assert(pm.HasSufficientCredits(), "Player can now afford the auto-adjusted 30 G bet");
        }

        /// <summary>
        /// 15. Edge Case: Transaction refund restores credits if a spin is cancelled.
        /// </summary>
        public static void Test_TransactionRefund_CancelsActiveTransaction()
        {
            PayoutManager pm = CreateTestManager(1000, 50, 10, 100, 10);
            pm.TryDeductBet(); // 1000 -> 950
            Assert(pm.CurrentBalance == 950, "Balance debited to 950");

            bool refunded = pm.CancelAndRefundActiveTransaction();
            Assert(refunded, "Refund must succeed");
            Assert(pm.CurrentBalance == 1000, "Balance must be refunded back to 1000");
            Assert(!pm.HasActiveTransaction, "Active transaction must be cleared");
        }

        /// <summary>
        /// 16. Verifies audit trail logging in SpinHistoryManager and SpinRecord.
        /// </summary>
        public static void Test_SpinHistory_AuditTrailRecordsAllTransactions()
        {
            GameObject go = new GameObject("HistoryManager");
            GameHistoryManager history = go.AddComponent<GameHistoryManager>();

            SymbolType[] winSymbols = new SymbolType[] { SymbolType.Cherry, SymbolType.Cherry, SymbolType.Cherry };
            SpinRecord r1 = new SpinRecord(0, winSymbols, 10, 20, true, 1010, "TXN_1");
            history.RecordSpin(r1);

            Assert(history.TotalSpins == 1, "Total spins recorded must be 1");
            Assert(history.History.Count == 1, "History count must be 1");
            Assert(history.History[0].NetProfitLoss == 10, "Net profit for 20 win on 10 bet must be +10");
            Assert(history.History[0].BalanceAfter == 1010, "Balance after spin must be 1010");
            Assert(history.History[0].IsWin, "Record must mark IsWin as true");
            Assert(history.History[0].GetOutcomeSummary() == "Cherry | Cherry | Cherry", "Outcome summary matches symbols");

            SymbolType[] lossSymbols = new SymbolType[] { SymbolType.Cherry, SymbolType.Bell, SymbolType.Bar };
            SpinRecord r2 = new SpinRecord(0, lossSymbols, 50, 0, false, 960, "TXN_2");
            history.RecordSpin(r2);

            Assert(history.TotalSpins == 2, "Total spins recorded must be 2");
            Assert(history.History[0].SpinNumber == 2, "Latest spin is at index 0");
            Assert(history.History[0].NetProfitLoss == -50, "Loss net profit must be -50");
            Assert(history.History[0].BalanceAfter == 960, "Balance after loss must be 960");
        }
    }
}
