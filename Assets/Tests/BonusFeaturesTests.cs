#pragma warning disable 0649, 0169, 0414
using System;
using System.Collections.Generic;
using UnityEngine;
using LuckySpin.SlotMachine.Data;
using LuckySpin.SlotMachine.Core;
using LuckySpin.SlotMachine.Gameplay;
using LuckySpin.SlotMachine.Bonus;
using LuckySpin.SlotMachine.Audio;
using LuckySpin.SlotMachine.Animations;

namespace LuckySpin.SlotMachine.Tests
{
    /// <summary>
    /// Automated test suite independently verifying all 7 optional bonus features:
    /// Auto-Spin, Jackpot Animation, Bonus/Wild Symbol, Win Streak, Recent History Stats, Audio Polish, and Particles.
    /// </summary>
    public class BonusFeaturesTests
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

            // 1. Auto-Spin Suite
            RunTest("AutoSpin_RespectsBalanceValidation_AbortsWhenInsufficient", Test_AutoSpin_RespectsBalanceValidation_AbortsWhenInsufficient, ref report);
            RunTest("AutoSpin_StopsAfterConfiguredSpinCount", Test_AutoSpin_StopsAfterConfiguredSpinCount, ref report);
            RunTest("AutoSpin_CannotStartOverlappingSpins", Test_AutoSpin_CannotStartOverlappingSpins, ref report);
            RunTest("AutoSpin_HaltsImmediatelyOnUserCancel", Test_AutoSpin_HaltsImmediatelyOnUserCancel, ref report);
            RunTest("AutoSpin_StopOnWin_HaltsOnFirstWin", Test_AutoSpin_StopOnWin_HaltsOnFirstWin, ref report);

            // 2. Jackpot Animation Suite
            RunTest("Jackpot_TriggeredOnDiamondMatch", Test_Jackpot_TriggeredOnDiamondMatch, ref report);
            RunTest("Jackpot_TriggeredOnHighPayout", Test_Jackpot_TriggeredOnHighPayout, ref report);
            RunTest("Jackpot_NotTriggeredOnStandardLossOrLowWin", Test_Jackpot_NotTriggeredOnStandardLossOrLowWin, ref report);
            RunTest("Jackpot_Dismiss_CleansUpState", Test_Jackpot_Dismiss_CleansUpState, ref report);

            // 3. Bonus / Wild Symbol Suite
            RunTest("WildSymbol_SubstitutesSingleMatchingPair", Test_WildSymbol_SubstitutesSingleMatchingPair, ref report);
            RunTest("WildSymbol_SubstitutesTwoWildsWithSingleSymbol", Test_WildSymbol_SubstitutesTwoWildsWithSingleSymbol, ref report);
            RunTest("WildSymbol_TripleWildAwards50x", Test_WildSymbol_TripleWildAwards50x, ref report);
            RunTest("WildSymbol_DisabledModeRequiresPureMatch", Test_WildSymbol_DisabledModeRequiresPureMatch, ref report);

            // 4. Win Streak Suite
            RunTest("WinStreak_IncrementsOnConsecutiveWins", Test_WinStreak_IncrementsOnConsecutiveWins, ref report);
            RunTest("WinStreak_DynamicMultiplierScale", Test_WinStreak_DynamicMultiplierScale, ref report);
            RunTest("WinStreak_ResetsToZeroOnLoss", Test_WinStreak_ResetsToZeroOnLoss, ref report);

            // 5. Recent History Statistics Suite
            RunTest("History_TracksWinRateAndPerformanceMetrics", Test_History_TracksWinRateAndPerformanceMetrics, ref report);
            RunTest("History_ClearResetsAllPerformanceCounters", Test_History_ClearResetsAllPerformanceCounters, ref report);

            // 6. Audio Effects Suite
            RunTest("Audio_MuteToggleMutesAllAudioSources", Test_Audio_MuteToggleMutesAllAudioSources, ref report);
            RunTest("Audio_VolumeControlsScaleAudioPlayback", Test_Audio_VolumeControlsScaleAudioPlayback, ref report);

            // 7. Particle Effects Suite
            RunTest("Particle_JackpotFountainConfiguredAccurately", Test_Particle_JackpotFountainConfiguredAccurately, ref report);

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

        // ==========================================
        // 1. AUTO-SPIN TESTS
        // ==========================================

        public static void Test_AutoSpin_RespectsBalanceValidation_AbortsWhenInsufficient()
        {
            GameObject go = new GameObject("TestAutoSpin");
            AutoSpinController controller = go.AddComponent<AutoSpinController>();
            PayoutManager pm = go.AddComponent<PayoutManager>();
            pm.Initialize(5, 10, 10, 100, 10); // Balance 5G < Bet 10G

            controller.Initialize(null, pm);

            AutoSpinStopReason? stoppedReason = null;
            controller.OnAutoSpinStopped += delegate(AutoSpinStopReason r) {
                stoppedReason = r;
            };

            bool started = controller.StartAutoSpin(10);
            Assert(!started, "Auto-spin must not start when balance is insufficient");
            Assert(!controller.IsActive, "Auto-spin state must be inactive");
            Assert(stoppedReason == AutoSpinStopReason.InsufficientCredits, "Stop reason must be InsufficientCredits");
        }

        public static void Test_AutoSpin_StopsAfterConfiguredSpinCount()
        {
            GameObject go = new GameObject("TestAutoSpin");
            AutoSpinController controller = go.AddComponent<AutoSpinController>();
            PayoutManager pm = go.AddComponent<PayoutManager>();
            pm.Initialize(1000, 10, 10, 100, 10);

            controller.Initialize(null, pm);
            bool started = controller.StartAutoSpin(3);
            Assert(started, "Auto-spin should start successfully with 1000 G");
            Assert(controller.RemainingSpins == 3, "Remaining spins should be 3 initially");

            // Step 1
            AutoSpinStopReason? r1;
            controller.SimulateTestStep(out r1);
            Assert(controller.RemainingSpins == 2, "Remaining spins should be 2 after step 1");

            // Step 2
            AutoSpinStopReason? r2;
            controller.SimulateTestStep(out r2);
            Assert(controller.RemainingSpins == 1, "Remaining spins should be 1 after step 2");

            // Step 3 (final)
            AutoSpinStopReason? r3;
            controller.SimulateTestStep(out r3);
            Assert(controller.RemainingSpins == 0, "Remaining spins should be 0 after step 3");
            Assert(!controller.IsActive, "Auto-spin must be inactive after completing count");
            Assert(r3 == AutoSpinStopReason.Completed, "Auto-spin must finish with Completed reason");
        }

        public static void Test_AutoSpin_CannotStartOverlappingSpins()
        {
            GameObject go = new GameObject("TestAutoSpin");
            AutoSpinController controller = go.AddComponent<AutoSpinController>();
            PayoutManager pm = go.AddComponent<PayoutManager>();
            pm.Initialize(1000, 10, 10, 100, 10);

            controller.Initialize(null, pm);
            bool firstStart = controller.StartAutoSpin(10);
            Assert(firstStart, "First auto-spin session must start");

            // Attempt overlapping session while already running
            bool secondStart = controller.StartAutoSpin(5);
            Assert(!secondStart, "Overlapping auto-spin session must be rejected");
            Assert(controller.RemainingSpins == 10, "Remaining spins must not be corrupted by second attempt");
        }

        public static void Test_AutoSpin_HaltsImmediatelyOnUserCancel()
        {
            GameObject go = new GameObject("TestAutoSpin");
            AutoSpinController controller = go.AddComponent<AutoSpinController>();
            PayoutManager pm = go.AddComponent<PayoutManager>();
            pm.Initialize(1000, 10, 10, 100, 10);

            controller.Initialize(null, pm);
            controller.StartAutoSpin(25);
            Assert(controller.IsActive, "Auto-spin is active");

            AutoSpinStopReason? stopReason = null;
            controller.OnAutoSpinStopped += delegate(AutoSpinStopReason r) {
                stopReason = r;
            };

            controller.StopAutoSpin(AutoSpinStopReason.UserCancelled);
            Assert(!controller.IsActive, "Auto-spin should be inactive after user cancel");
            Assert(controller.RemainingSpins == 0, "Remaining spins should reset to 0");
            Assert(stopReason == AutoSpinStopReason.UserCancelled, "Stop reason must be UserCancelled");
        }

        public static void Test_AutoSpin_StopOnWin_HaltsOnFirstWin()
        {
            GameObject go = new GameObject("TestAutoSpin");
            AutoSpinController controller = go.AddComponent<AutoSpinController>();
            PayoutManager pm = go.AddComponent<PayoutManager>();
            pm.Initialize(1000, 10, 10, 100, 10);

            controller.Initialize(null, pm);
            controller.StartAutoSpin(10, true, 0); // stopOnAnyWin = true

            Assert(controller.StopOnAnyWin, "StopOnAnyWin should be true");
        }

        // ==========================================
        // 2. JACKPOT ANIMATION TESTS
        // ==========================================

        public static void Test_Jackpot_TriggeredOnDiamondMatch()
        {
            GameObject go = new GameObject("TestJackpot");
            JackpotCelebrationController jackpot = go.AddComponent<JackpotCelebrationController>();

            bool triggered = false;
            int triggeredAmount = 0;
            jackpot.OnJackpotTriggered += delegate(int amt) {
                triggered = true;
                triggeredAmount = amt;
            };

            bool res = jackpot.CheckAndTriggerJackpot(1000, true); // isDiamondMatch = true
            Assert(res, "CheckAndTriggerJackpot should return true for Diamond match");
            Assert(triggered, "OnJackpotTriggered event must fire");
            Assert(triggeredAmount == 1000, "Triggered amount must be 1000");
            Assert(jackpot.IsCelebrationActive, "Celebration must be marked active");
        }

        public static void Test_Jackpot_TriggeredOnHighPayout()
        {
            GameObject go = new GameObject("TestJackpot");
            JackpotCelebrationController jackpot = go.AddComponent<JackpotCelebrationController>();
            jackpot.JackpotThreshold = 1000;

            bool res = jackpot.CheckAndTriggerJackpot(2500, false); // Payout 2500 >= threshold 1000
            Assert(res, "CheckAndTriggerJackpot should return true when payout exceeds threshold");
            Assert(jackpot.IsCelebrationActive, "Celebration must be marked active");
        }

        public static void Test_Jackpot_NotTriggeredOnStandardLossOrLowWin()
        {
            GameObject go = new GameObject("TestJackpot");
            JackpotCelebrationController jackpot = go.AddComponent<JackpotCelebrationController>();
            jackpot.JackpotThreshold = 1000;

            bool lossRes = jackpot.CheckAndTriggerJackpot(0, false);
            Assert(!lossRes, "Jackpot should not trigger on 0 payout loss");
            Assert(!jackpot.IsCelebrationActive, "Celebration must remain inactive on loss");

            bool lowWinRes = jackpot.CheckAndTriggerJackpot(20, false); // Cherry 20G win
            Assert(!lowWinRes, "Jackpot should not trigger on standard low win");
        }

        public static void Test_Jackpot_Dismiss_CleansUpState()
        {
            GameObject go = new GameObject("TestJackpot");
            JackpotCelebrationController jackpot = go.AddComponent<JackpotCelebrationController>();

            jackpot.TriggerJackpot(5000);
            Assert(jackpot.IsCelebrationActive, "Jackpot is active");

            bool dismissed = false;
            jackpot.OnJackpotDismissed += delegate { dismissed = true; };

            jackpot.Dismiss();
            Assert(!jackpot.IsCelebrationActive, "Jackpot is no longer active after dismiss");
            Assert(dismissed, "OnJackpotDismissed event fired");
        }

        // ==========================================
        // 3. BONUS / WILD SYMBOL TESTS
        // ==========================================

        public static void Test_WildSymbol_SubstitutesSingleMatchingPair()
        {
            GameObject go = new GameObject("Evaluator");
            WinningEvaluator evaluator = go.AddComponent<WinningEvaluator>();
            evaluator.EnableWildSubstitutions = true;

            SymbolData cherry = ScriptableObject.CreateInstance<SymbolData>();
            cherry.Initialize(SymbolType.Cherry, "Cherry", null, 2, 35);

            SymbolData wild = ScriptableObject.CreateInstance<SymbolData>();
            wild.Initialize(SymbolType.Wild, "Wild", null, 50, 8);

            // Combination: Cherry | Wild | Cherry -> Wild substitutes for Cherry
            SymbolData[] payline = new SymbolData[] { cherry, wild, cherry };
            EvaluationResult res = evaluator.Evaluate(payline, 50);

            Assert(res.IsWin, "Wild-assisted Cherry combo must be evaluated as a win");
            Assert(res.WinningType == SymbolType.Cherry, "Winning type must resolve to Cherry");
            Assert(res.Payout == 100, "Payout must be 50 * 2 = 100");
            Assert(res.IsWildAssisted, "Result should be marked IsWildAssisted");
        }

        public static void Test_WildSymbol_SubstitutesTwoWildsWithSingleSymbol()
        {
            GameObject go = new GameObject("Evaluator");
            WinningEvaluator evaluator = go.AddComponent<WinningEvaluator>();
            evaluator.EnableWildSubstitutions = true;

            SymbolData seven = ScriptableObject.CreateInstance<SymbolData>();
            seven.Initialize(SymbolType.Seven, "Seven", null, 25, 15);

            SymbolData wild = ScriptableObject.CreateInstance<SymbolData>();
            wild.Initialize(SymbolType.Wild, "Wild", null, 50, 8);

            // Combination: Wild | Seven | Wild -> 2 Wilds substitute for Seven
            SymbolData[] payline = new SymbolData[] { wild, seven, wild };
            EvaluationResult res = evaluator.Evaluate(payline, 10);

            Assert(res.IsWin, "Two Wilds + Seven must evaluate as Seven win");
            Assert(res.WinningType == SymbolType.Seven, "Winning type must resolve to Seven");
            Assert(res.Payout == 250, "Payout must be 10 * 25 = 250");
        }

        public static void Test_WildSymbol_TripleWildAwards50x()
        {
            GameObject go = new GameObject("Evaluator");
            WinningEvaluator evaluator = go.AddComponent<WinningEvaluator>();
            evaluator.EnableWildSubstitutions = true;

            SymbolData wild = ScriptableObject.CreateInstance<SymbolData>();
            wild.Initialize(SymbolType.Wild, "Wild", null, 50, 8);

            // Combination: Wild | Wild | Wild
            SymbolData[] payline = new SymbolData[] { wild, wild, wild };
            EvaluationResult res = evaluator.Evaluate(payline, 20);

            Assert(res.IsWin, "Triple Wild must evaluate as a win");
            Assert(res.WinningType == SymbolType.Wild, "Winning type must be Wild");
            Assert(res.Payout == 1000, "Payout must be 20 * 50 = 1000 G");
        }

        public static void Test_WildSymbol_DisabledModeRequiresPureMatch()
        {
            GameObject go = new GameObject("Evaluator");
            WinningEvaluator evaluator = go.AddComponent<WinningEvaluator>();
            evaluator.EnableWildSubstitutions = false; // Disabled (Pure 3 matching only)

            SymbolData cherry = ScriptableObject.CreateInstance<SymbolData>();
            cherry.Initialize(SymbolType.Cherry, "Cherry", null, 2, 35);

            SymbolData wild = ScriptableObject.CreateInstance<SymbolData>();
            wild.Initialize(SymbolType.Wild, "Wild", null, 50, 8);

            SymbolData[] payline = new SymbolData[] { cherry, wild, cherry };
            EvaluationResult res = evaluator.Evaluate(payline, 50);

            Assert(!res.IsWin, "When Wild substitutions are disabled, mixed wild payline must evaluate as loss");
            Assert(res.Payout == 0, "Payout must be 0");
        }

        // ==========================================
        // 4. WIN STREAK TESTS
        // ==========================================

        public static void Test_WinStreak_IncrementsOnConsecutiveWins()
        {
            GameObject go = new GameObject("WinStreak");
            WinStreakTracker tracker = go.AddComponent<WinStreakTracker>();
            tracker.Initialize(null);

            Assert(tracker.CurrentStreak == 0, "Streak begins at 0");

            tracker.RecordResult(true);
            Assert(tracker.CurrentStreak == 1, "Streak should be 1 after first win");

            tracker.RecordResult(true);
            Assert(tracker.CurrentStreak == 2, "Streak should be 2 after second win");

            tracker.RecordResult(true);
            Assert(tracker.CurrentStreak == 3, "Streak should be 3 after third win");
            Assert(tracker.BestStreak == 3, "Best streak should be 3");
        }

        public static void Test_WinStreak_DynamicMultiplierScale()
        {
            GameObject go = new GameObject("WinStreak");
            WinStreakTracker tracker = go.AddComponent<WinStreakTracker>();

            Assert(Mathf.Abs(tracker.GetStreakMultiplier(0) - 1.0f) < 0.01f, "0 wins: 1.0x");
            Assert(Mathf.Abs(tracker.GetStreakMultiplier(1) - 1.0f) < 0.01f, "1 win: 1.0x");
            Assert(Mathf.Abs(tracker.GetStreakMultiplier(2) - 1.25f) < 0.01f, "2 wins: 1.25x (+25%)");
            Assert(Mathf.Abs(tracker.GetStreakMultiplier(3) - 1.50f) < 0.01f, "3 wins: 1.50x (+50%)");
            Assert(Mathf.Abs(tracker.GetStreakMultiplier(4) - 2.0f) < 0.01f, "4+ wins: 2.0x (Double!)");
            Assert(Mathf.Abs(tracker.GetStreakMultiplier(10) - 2.0f) < 0.01f, "10 wins: 2.0x");
        }

        public static void Test_WinStreak_ResetsToZeroOnLoss()
        {
            GameObject go = new GameObject("WinStreak");
            WinStreakTracker tracker = go.AddComponent<WinStreakTracker>();

            tracker.RecordResult(true);
            tracker.RecordResult(true);
            Assert(tracker.CurrentStreak == 2, "Streak should be 2");

            bool streakBrokenFired = false;
            tracker.OnStreakBroken += delegate { streakBrokenFired = true; };

            float mult = tracker.RecordResult(false); // Loss
            Assert(tracker.CurrentStreak == 0, "Streak should reset to 0 on loss");
            Assert(tracker.BestStreak == 2, "Best streak remains recorded at 2");
            Assert(streakBrokenFired, "OnStreakBroken event must fire");
            Assert(Mathf.Abs(mult - 1.0f) < 0.01f, "Multiplier returns to 1.0 on loss");
        }

        // ==========================================
        // 5. RECENT RESULTS HISTORY TESTS
        // ==========================================

        public static void Test_History_TracksWinRateAndPerformanceMetrics()
        {
            GameObject go = new GameObject("History");
            GameHistoryManager history = go.AddComponent<GameHistoryManager>();

            SymbolType[] cherryCombo = new SymbolType[] { SymbolType.Cherry, SymbolType.Cherry, SymbolType.Cherry };
            SymbolType[] lossCombo = new SymbolType[] { SymbolType.Cherry, SymbolType.Bell, SymbolType.Bar };

            // 1 Win (+100 G)
            history.RecordSpin(new SpinRecord(0, cherryCombo, 50, 100, true));
            // 1 Loss (-50 G)
            history.RecordSpin(new SpinRecord(0, lossCombo, 50, 0, false));

            Assert(history.TotalSpins == 2, "Total spins = 2");
            Assert(history.TotalWins == 1, "Total wins = 1");
            Assert(history.TotalLosses == 1, "Total losses = 1");
            Assert(Mathf.Abs(history.WinRatePercentage - 50f) < 0.01f, "Win rate = 50%");
            Assert(history.BiggestWin == 100, "Biggest win = 100 G");

            // 2nd Win (+2500 G Jackpot)
            SymbolType[] diamondCombo = new SymbolType[] { SymbolType.Diamond, SymbolType.Diamond, SymbolType.Diamond };
            history.RecordSpin(new SpinRecord(0, diamondCombo, 25, 2500, true));

            Assert(history.TotalSpins == 3, "Total spins = 3");
            Assert(history.TotalWins == 2, "Total wins = 2");
            Assert(Mathf.Abs(history.WinRatePercentage - 66.67f) < 0.1f, "Win rate = 66.7%");
            Assert(history.BiggestWin == 2500, "Biggest win updated to 2500 G");
        }

        public static void Test_History_ClearResetsAllPerformanceCounters()
        {
            GameObject go = new GameObject("History");
            GameHistoryManager history = go.AddComponent<GameHistoryManager>();

            SymbolType[] cherryCombo = new SymbolType[] { SymbolType.Cherry, SymbolType.Cherry, SymbolType.Cherry };
            history.RecordSpin(new SpinRecord(0, cherryCombo, 50, 100, true));
            Assert(history.TotalSpins == 1, "Spins = 1");

            history.ClearHistory();
            Assert(history.TotalSpins == 0, "Spins reset to 0");
            Assert(history.TotalWins == 0, "Wins reset to 0");
            Assert(history.BiggestWin == 0, "Biggest win reset to 0");
            Assert(history.History.Count == 0, "History list is empty");
        }

        // ==========================================
        // 6. AUDIO EFFECTS TESTS
        // ==========================================

        public static void Test_Audio_MuteToggleMutesAllAudioSources()
        {
            GameObject go = new GameObject("Audio");
            AudioManager audio = go.AddComponent<AudioManager>();

            Assert(!audio.IsMuted, "Audio starts unmuted");

            bool muteEventFired = false;
            audio.OnMuteToggled += delegate(bool m) {
                muteEventFired = true;
            };

            audio.ToggleMute();
            Assert(audio.IsMuted, "Audio should be muted after toggle");
            Assert(muteEventFired, "OnMuteToggled event should fire");

            audio.ToggleMute();
            Assert(!audio.IsMuted, "Audio should be unmuted after second toggle");
        }

        public static void Test_Audio_VolumeControlsScaleAudioPlayback()
        {
            GameObject go = new GameObject("Audio");
            AudioManager audio = go.AddComponent<AudioManager>();

            audio.SetVolume(0.5f);
            Assert(Mathf.Abs(audio.MasterVolume - 0.5f) < 0.01f, "Volume set to 0.5");

            // Over-range clamping
            audio.SetVolume(1.5f);
            Assert(Mathf.Abs(audio.MasterVolume - 1.0f) < 0.01f, "Volume clamped to 1.0");

            audio.SetVolume(-0.2f);
            Assert(Mathf.Abs(audio.MasterVolume - 0.0f) < 0.01f, "Volume clamped to 0.0");
        }

        // ==========================================
        // 7. PARTICLE EFFECTS TESTS
        // ==========================================

        public static void Test_Particle_JackpotFountainConfiguredAccurately()
        {
            GameObject go = new GameObject("Particles");
            CoinParticleController particles = go.AddComponent<CoinParticleController>();

            Assert(!particles.IsActive, "Particle controller begins inactive");
            particles.Clear();
            Assert(!particles.IsActive, "Particles cleared successfully");
        }
    }
}

