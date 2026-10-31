#pragma warning disable 0649, 0169, 0414
using System;
using System.Collections.Generic;
using UnityEngine;
using LuckySpin.SlotMachine.Data;
using LuckySpin.SlotMachine.Core;
using LuckySpin.SlotMachine.Gameplay;
using LuckySpin.SlotMachine.Reels;
using LuckySpin.SlotMachine.Bonus;
using LuckySpin.SlotMachine.Audio;
using LuckySpin.SlotMachine.UI;
using LuckySpin.SlotMachine.Animations;

namespace LuckySpin.SlotMachine.Tests
{
    /// <summary>
    /// Phase 8: Comprehensive End-to-End Integration, Cross-System Coordination,
    /// Stress Testing (100 & 500 Spins), Financial Invariant Auditing, and Concurrency Validation.
    /// </summary>
    public class IntegrationAndStressTests
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

            // TEST 1: Full Gameplay Lifecycle
            RunTest("Lifecycle_EndToEnd_LaunchToIdleWinCycle", Test_Lifecycle_EndToEnd_LaunchToIdleWinCycle, ref report);
            RunTest("Lifecycle_EndToEnd_LaunchToIdleLossCycle", Test_Lifecycle_EndToEnd_LaunchToIdleLossCycle, ref report);

            // TEST 2: Cross-System Integration (All 12 Components)
            RunTest("CrossSystem_AllTwelveComponents_CoordinatedCleanly", Test_CrossSystem_AllTwelveComponents_CoordinatedCleanly, ref report);
            RunTest("CrossSystem_JackpotAndStreak_IntegratedCelebration", Test_CrossSystem_JackpotAndStreak_IntegratedCelebration, ref report);

            // TEST 3: Stress Testing
            RunTest("Stress_Deterministic100Spins_Simulation", Test_Stress_Deterministic100Spins_Simulation, ref report);
            RunTest("Stress_Deterministic500Spins_Simulation", Test_Stress_Deterministic500Spins_Simulation, ref report);
            RunTest("Stress_RapidSpinSpam_ZeroDuplicateDebits", Test_Stress_RapidSpinSpam_ZeroDuplicateDebits, ref report);
            RunTest("Stress_AutoSpin_LowCreditsHaltsSafely", Test_Stress_AutoSpin_LowCreditsHaltsSafely, ref report);
            RunTest("Stress_WildCombinations_FullIntegration", Test_Stress_WildCombinations_FullIntegration, ref report);

            // TEST 4: Financial Integrity
            RunTest("FinancialIntegrity_StrictInvariantEquationHolds", Test_FinancialIntegrity_StrictInvariantEquationHolds, ref report);
            RunTest("FinancialIntegrity_NoNegativeBalanceOrOrphanedTransactions", Test_FinancialIntegrity_NoNegativeBalanceOrOrphanedTransactions, ref report);

            // TEST 5: Performance & Resource Management
            RunTest("Performance_ParticleAndAudioLifecycleCleanup", Test_Performance_ParticleAndAudioLifecycleCleanup, ref report);

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

        // =========================================================================
        // HELPER FIXTURE: CONSTRUCT COMPLETE INTEGRATED SLOT MACHINE ENVIRONMENT
        // =========================================================================

        private class IntegratedGameEnvironment
        {
            public GameObject Root;
            public GameManager GM;
            public PayoutManager PM;
            public RandomOutcomeGenerator ROG;
            public WinningEvaluator WE;
            public SlotMachineController SMC;
            public ReelController[] Reels;
            public AutoSpinController ASC;
            public JackpotCelebrationController JCC;
            public WinStreakTracker WST;
            public GameHistoryManager GHM;
            public AudioManager AM;
            public UIManager UIM;
            public SymbolDatabase DB;
            public SymbolData Cherry, Bell, Bar, Seven, Diamond, Wild;
        }

        private static IntegratedGameEnvironment CreateEnvironment(int startBal = 1000, int defBet = 10)
        {
            IntegratedGameEnvironment env = new IntegratedGameEnvironment();
            env.Root = new GameObject("SlotMachineSystem");

            // Symbols
            env.Cherry = ScriptableObject.CreateInstance<SymbolData>();
            env.Cherry.Initialize(SymbolType.Cherry, "Cherry", null, 2, 35);

            env.Bell = ScriptableObject.CreateInstance<SymbolData>();
            env.Bell.Initialize(SymbolType.Bell, "Bell", null, 5, 25);

            env.Bar = ScriptableObject.CreateInstance<SymbolData>();
            env.Bar.Initialize(SymbolType.Bar, "BAR", null, 10, 20);

            env.Seven = ScriptableObject.CreateInstance<SymbolData>();
            env.Seven.Initialize(SymbolType.Seven, "Seven", null, 25, 15);

            env.Diamond = ScriptableObject.CreateInstance<SymbolData>();
            env.Diamond.Initialize(SymbolType.Diamond, "Diamond", null, 100, 5);

            env.Wild = ScriptableObject.CreateInstance<SymbolData>();
            env.Wild.Initialize(SymbolType.Wild, "Wild", null, 50, 8);

            env.DB = ScriptableObject.CreateInstance<SymbolDatabase>();
            env.DB.Initialize(new SymbolData[] { env.Cherry, env.Bell, env.Bar, env.Seven, env.Diamond, env.Wild });

            // Subsystems
            env.GM = env.Root.AddComponent<GameManager>();
            env.PM = env.Root.AddComponent<PayoutManager>();
            env.PM.Initialize(startBal, defBet, 10, 100, 10);

            env.ROG = env.Root.AddComponent<RandomOutcomeGenerator>();
            env.ROG.Database = env.DB;
            env.ROG.RandomProvider = new SeededRandomProvider(42);

            env.WE = env.Root.AddComponent<WinningEvaluator>();
            env.WE.EnableWildSubstitutions = true;

            env.SMC = env.Root.AddComponent<SlotMachineController>();
            env.Reels = new ReelController[3];
            for (int i = 0; i < 3; i++)
            {
                GameObject rObj = new GameObject("Reel_" + i);
                rObj.transform.SetParent(env.Root.transform);
                env.Reels[i] = rObj.AddComponent<ReelController>();
                env.Reels[i].Initialize(i, env.DB);
                env.Reels[i].DecelerationTime = 0f; // Instant in tests
            }
            env.SMC.SetReels(env.Reels);

            env.ASC = env.Root.AddComponent<AutoSpinController>();
            env.JCC = env.Root.AddComponent<JackpotCelebrationController>();
            env.WST = env.Root.AddComponent<WinStreakTracker>();
            env.GHM = env.Root.AddComponent<GameHistoryManager>();
            env.AM = env.Root.AddComponent<AudioManager>();
            env.UIM = env.Root.AddComponent<UIManager>();

            // Wire Master
            env.GM.InitializeForTesting(
                env.SMC, env.ROG, env.WE, env.PM, env.UIM, env.AM, env.GHM, env.DB,
                env.ASC, env.JCC, env.WST);

            return env;
        }

        // =========================================================================
        // TEST 1: COMPLETE GAMEPLAY LIFECYCLE
        // =========================================================================

        public static void Test_Lifecycle_EndToEnd_LaunchToIdleWinCycle()
        {
            IntegratedGameEnvironment env = CreateEnvironment(1000, 50);

            // Step 1: Game Launch -> Initial State
            Assert(env.GM.CurrentState == GameState.Idle, "State must be Idle upon launch");
            Assert(env.PM.CurrentBalance == 1000, "Starting balance is 1000 G");
            Assert(env.PM.CurrentBet == 50, "Current bet is 50 G");

            // Step 2: Bet Selection
            env.PM.SetBet(20);
            Assert(env.PM.CurrentBet == 20, "Bet set to 20 G");

            // Step 3: Credit Validation & Spin Request
            Assert(env.PM.HasSufficientCredits(), "Credits are sufficient for 20 G bet");

            // Force RNG provider to deterministic Cherry match
            env.ROG.UseWeightedProbabilities = false;
            env.ROG.SetRandomProvider(new DeterministicOutcomeProvider(new SymbolData[] { env.Cherry, env.Cherry, env.Cherry }, env.DB));

            // Step 4: Dispatch Spin (Atomic Bet Deduction -> RNG -> Spin State)
            env.GM.RequestSpin();

            // Bet deducted: 1000 - 20 = 980
            Assert(env.PM.CurrentBalance == 980, "Balance must be debited to 980 G immediately");
            Assert(env.PM.HasActiveTransaction, "Transaction must be active during spin");

            // Step 5: Reel Animation & Stopping at Predetermined Outcome
            env.SMC.StopReelsImmediately(new SymbolData[] { env.Cherry, env.Cherry, env.Cherry });

            // Step 6: Payout Settlement (Cherry 2x: 20 * 2 = +40 G)
            Assert(env.PM.CurrentBalance == 1020, "Balance must be 980 + 40 = 1020 G");
            Assert(!env.PM.HasActiveTransaction, "Transaction must be settled cleanly");

            // Step 7: History Recorded
            Assert(env.GHM.TotalSpins == 1, "Total spins recorded = 1");
            Assert(env.GHM.History[0].PayoutAwarded == 40, "History recorded 40 G win");
            Assert(env.GHM.History[0].BalanceAfter == 1020, "History recorded balance after = 1020 G");

            // Step 8: Return to Idle
            Assert(env.GM.CurrentState == GameState.Idle, "System must return to Idle state ready for next spin");
        }

        public static void Test_Lifecycle_EndToEnd_LaunchToIdleLossCycle()
        {
            IntegratedGameEnvironment env = CreateEnvironment(1000, 100);

            // Force non-matching outcome: Cherry | Diamond | Bell
            env.ROG.UseWeightedProbabilities = false;
            env.ROG.SetRandomProvider(new DeterministicOutcomeProvider(new SymbolData[] { env.Cherry, env.Diamond, env.Bell }, env.DB));

            env.GM.RequestSpin();
            Assert(env.PM.CurrentBalance == 900, "100 G bet debited immediately (1000 -> 900)");

            env.SMC.StopReelsImmediately(new SymbolData[] { env.Cherry, env.Diamond, env.Bell });

            Assert(env.PM.CurrentBalance == 900, "Balance remains 900 G on loss");
            Assert(!env.PM.HasActiveTransaction, "Transaction settled with zero payout");
            Assert(env.GHM.TotalSpins == 1, "History records 1 spin");
            Assert(!env.GHM.History[0].IsWin, "History records loss");
            Assert(env.GHM.History[0].NetProfitLoss == -100, "Net loss is -100 G");
            Assert(env.GM.CurrentState == GameState.Idle, "State returned to Idle");
        }

        // =========================================================================
        // TEST 2: CROSS-SYSTEM INTEGRATION (ALL 12 COMPONENTS)
        // =========================================================================

        public static void Test_CrossSystem_AllTwelveComponents_CoordinatedCleanly()
        {
            IntegratedGameEnvironment env = CreateEnvironment(1000, 10);

            // Verify all 12 subsystem components are non-null and linked
            Assert(env.GM != null, "GameManager present");
            Assert(env.PM != null, "PayoutManager present");
            Assert(env.ROG != null, "RandomOutcomeGenerator present");
            Assert(env.WE != null, "WinningEvaluator present");
            Assert(env.SMC != null, "SlotMachineController present");
            Assert(env.Reels.Length == 3, "3 ReelControllers present");
            Assert(env.ASC != null, "AutoSpinController present");
            Assert(env.JCC != null, "JackpotCelebrationController present");
            Assert(env.WST != null, "WinStreakTracker present");
            Assert(env.GHM != null, "GameHistoryManager present");
            Assert(env.AM != null, "AudioManager present");
            Assert(env.UIM != null, "UIManager present");

            // Execute coordinated spin
            env.ROG.UseWeightedProbabilities = false;
            env.ROG.SetRandomProvider(new DeterministicOutcomeProvider(new SymbolData[] { env.Bell, env.Bell, env.Bell }, env.DB));
            env.GM.RequestSpin();
            env.SMC.StopReelsImmediately(new SymbolData[] { env.Bell, env.Bell, env.Bell });

            // Bell 5x on 10 bet = 50 G payout
            Assert(env.PM.CurrentBalance == 1040, "Payout awarded (1000 - 10 + 50 = 1040)");
            Assert(env.WST.CurrentStreak == 1, "Win streak incremented to 1");
            Assert(env.GHM.TotalWins == 1, "History total wins = 1");
        }

        public static void Test_CrossSystem_JackpotAndStreak_IntegratedCelebration()
        {
            IntegratedGameEnvironment env = CreateEnvironment(1000, 100);

            // Spin 1: Win Bell (100 * 5 = 500 G) -> Streak 1
            env.ROG.UseWeightedProbabilities = false;
            env.ROG.SetRandomProvider(new DeterministicOutcomeProvider(new SymbolData[] { env.Bell, env.Bell, env.Bell }, env.DB));
            env.GM.RequestSpin();
            env.SMC.StopReelsImmediately(new SymbolData[] { env.Bell, env.Bell, env.Bell });
            Assert(env.WST.CurrentStreak == 1, "Streak is 1 after first win");

            // Spin 2: Win Diamond Jackpot (100 * 100 = 10,000 G) -> Streak 2 + Jackpot!
            env.ROG.SetRandomProvider(new DeterministicOutcomeProvider(new SymbolData[] { env.Diamond, env.Diamond, env.Diamond }, env.DB));
            env.GM.RequestSpin();
            env.SMC.StopReelsImmediately(new SymbolData[] { env.Diamond, env.Diamond, env.Diamond });

            Assert(env.WST.CurrentStreak == 2, "Streak is 2");
            Assert(env.JCC.IsCelebrationActive, "Jackpot celebration triggered on Diamond 3-match");
            Assert(env.GHM.BiggestWin == 10000, "Biggest win recorded as 10,000 G");
        }

        // =========================================================================
        // TEST 3: STRESS TESTING (100 & 500 SPINS, SPAM, AUTO-SPIN)
        // =========================================================================

        public static void Test_Stress_Deterministic100Spins_Simulation()
        {
            IntegratedGameEnvironment env = CreateEnvironment(5000, 20);
            env.ROG.RandomProvider = new SeededRandomProvider(12345);

            int initialBal = env.PM.CurrentBalance;
            int totalBetsAccum = 0;
            int totalWinsAccum = 0;

            for (int i = 0; i < 100; i++)
            {
                if (!env.PM.HasSufficientCredits()) break;

                totalBetsAccum += env.PM.CurrentBet;
                env.GM.RequestSpin();
                env.SMC.StopReelsImmediately(env.ROG.GenerateOutcome(env.DB));

                if (env.GM.LastResult.IsWin)
                {
                    totalWinsAccum += env.GM.LastResult.Payout;
                }
            }

            Assert(env.GHM.TotalSpins == 100, "Exactly 100 spins completed");
            Assert(env.PM.CurrentBalance == initialBal - totalBetsAccum + totalWinsAccum, 
                "Financial invariant holds across 100 spins");
            Assert(!env.PM.HasActiveTransaction, "Zero orphaned transactions");
        }

        public static void Test_Stress_Deterministic500Spins_Simulation()
        {
            IntegratedGameEnvironment env = CreateEnvironment(50000, 10);
            env.ROG.RandomProvider = new SeededRandomProvider(98765);

            int initialBal = env.PM.CurrentBalance;
            int totalBetsAccum = 0;
            int totalWinsAccum = 0;

            for (int i = 0; i < 500; i++)
            {
                if (!env.PM.HasSufficientCredits()) break;

                totalBetsAccum += env.PM.CurrentBet;
                env.GM.RequestSpin();
                env.SMC.StopReelsImmediately(env.ROG.GenerateOutcome(env.DB));

                if (env.GM.LastResult.IsWin)
                {
                    totalWinsAccum += env.GM.LastResult.Payout;
                }
            }

            Assert(env.GHM.TotalSpins == 500, "Exactly 500 spins simulated cleanly");
            Assert(env.PM.CurrentBalance == initialBal - totalBetsAccum + totalWinsAccum, 
                "Strict conservation of virtual currency holds across 500 spins");
            Assert(env.PM.TotalSpinsDebited == 500, "Total debits match 500");
            Assert(!env.PM.HasActiveTransaction, "No active transaction leakage");
        }

        public static void Test_Stress_RapidSpinSpam_ZeroDuplicateDebits()
        {
            IntegratedGameEnvironment env = CreateEnvironment(1000, 50);

            // Begin 1 legitimate spin
            env.GM.RequestSpin();
            Assert(env.PM.CurrentBalance == 950, "Initial debit of 50 G");

            // Rapid spam: Attempt 50 rapid RequestSpin calls while active
            for (int i = 0; i < 50; i++)
            {
                env.GM.RequestSpin();
            }

            // Balance must remain strictly at 950 (no duplicate deductions permitted)
            Assert(env.PM.CurrentBalance == 950, "Spam calls while active must NOT deduct credits");
            Assert(env.PM.TotalSpinsDebited == 1, "Only 1 debit recorded");

            // Settle original spin
            env.SMC.StopReelsImmediately(new SymbolData[] { env.Cherry, env.Cherry, env.Cherry });
            Assert(env.GM.CurrentState == GameState.Idle, "Returned to Idle");
        }

        public static void Test_Stress_AutoSpin_LowCreditsHaltsSafely()
        {
            IntegratedGameEnvironment env = CreateEnvironment(25, 10); // Balance 25 G, Bet 10 G

            AutoSpinStopReason? stopReason = null;
            env.ASC.OnAutoSpinStopped += delegate(AutoSpinStopReason r) {
                stopReason = r;
            };

            // Start auto-spin for 10 spins
            env.ASC.StartAutoSpin(10);

            // Step 1: Spin 1 (25 -> 15 G)
            AutoSpinStopReason? r1;
            env.ASC.SimulateTestStep(out r1);
            env.SMC.StopReelsImmediately(new SymbolData[] { env.Cherry, env.Bell, env.Bar });

            // Step 2: Spin 2 (15 -> 5 G)
            AutoSpinStopReason? r2;
            env.ASC.SimulateTestStep(out r2);
            env.SMC.StopReelsImmediately(new SymbolData[] { env.Cherry, env.Bell, env.Bar });

            // Step 3: Spin 3 attempted with 5 G balance (< 10 G bet)
            AutoSpinStopReason? r3;
            bool canStep = env.ASC.SimulateTestStep(out r3);

            Assert(!canStep, "Auto-spin must halt when balance cannot afford bet");
            Assert(stopReason == AutoSpinStopReason.InsufficientCredits, "Halt reason must be InsufficientCredits");
            Assert(env.PM.CurrentBalance == 5, "Remaining 5 G balance intact");
        }

        public static void Test_Stress_WildCombinations_FullIntegration()
        {
            IntegratedGameEnvironment env = CreateEnvironment(1000, 10);

            // Test 1 Wild + 2 Sevens
            env.ROG.UseWeightedProbabilities = false;
            env.ROG.SetRandomProvider(new DeterministicOutcomeProvider(new SymbolData[] { env.Seven, env.Wild, env.Seven }, env.DB));
            env.GM.RequestSpin();
            env.SMC.StopReelsImmediately(new SymbolData[] { env.Seven, env.Wild, env.Seven });

            // Seven 25x on 10 bet = 250 G payout
            Assert(env.PM.CurrentBalance == 1240, "Wild-assisted Seven payout awarded (1000 - 10 + 250 = 1240 G)");
            Assert(env.GM.LastResult.IsWildAssisted, "Marked as wild-assisted");
        }

        // =========================================================================
        // TEST 4: FINANCIAL INTEGRITY
        // =========================================================================

        public static void Test_FinancialIntegrity_StrictInvariantEquationHolds()
        {
            IntegratedGameEnvironment env = CreateEnvironment(10000, 50);
            env.ROG.RandomProvider = new SeededRandomProvider(54321);

            for (int i = 0; i < 50; i++)
            {
                env.GM.RequestSpin();
                env.SMC.StopReelsImmediately(env.ROG.GenerateOutcome(env.DB));
            }

            int formulaBalance = env.PM.StartingBalance - env.PM.TotalBetsPlaced + env.PM.TotalPayoutsAwarded;
            Assert(env.PM.CurrentBalance == formulaBalance, 
                string.Format("Financial Invariant Violated! Initial ({0}) - Bets ({1}) + Wins ({2}) != Current ({3})",
                env.PM.StartingBalance, env.PM.TotalBetsPlaced, env.PM.TotalPayoutsAwarded, env.PM.CurrentBalance));
        }

        public static void Test_FinancialIntegrity_NoNegativeBalanceOrOrphanedTransactions()
        {
            IntegratedGameEnvironment env = CreateEnvironment(30, 20);

            env.GM.RequestSpin(); // 30 - 20 = 10 G
            env.SMC.StopReelsImmediately(new SymbolData[] { env.Cherry, env.Bell, env.Bar }); // Loss

            // Explicitly set bet back to 20 G to attempt an unaffordable spin with 10 G balance
            env.PM.SetBet(20);
            Assert(env.PM.CurrentBet == 20, "Bet set to 20 G");
            Assert(!env.PM.HasSufficientCredits(), "Credits insufficient for 20 G bet with 10 G balance");

            // Attempt another 20 G spin when balance is 10 G
            env.GM.RequestSpin();

            Assert(env.PM.CurrentBalance >= 0, "Balance must never drop below 0");
            Assert(env.PM.CurrentBalance == 10, "Balance remains 10 G (rejected unauthorized debit)");
            Assert(!env.PM.HasActiveTransaction, "No orphaned transaction");
            Assert(env.GM.CurrentState == GameState.InsufficientFunds, "State transitions to InsufficientFunds on credit failure");

            // Lowering bet to affordable 10 G restores Idle state
            env.PM.SetBet(10);
            Assert(env.GM.CurrentState == GameState.Idle, "System returns to Idle state when affordable bet selected");
        }

        // =========================================================================
        // TEST 5: PERFORMANCE & RESOURCE AUDIT
        // =========================================================================

        public static void Test_Performance_ParticleAndAudioLifecycleCleanup()
        {
            GameObject pObj = new GameObject("Particles");
            CoinParticleController particles = pObj.AddComponent<CoinParticleController>();

            particles.PlayCoinFountain(Vector2.zero);
            particles.Clear();
            Assert(particles.ActiveCoinCount == 0, "All particles cleared from memory");
            Assert(!particles.IsActive, "Particle controller state reset to inactive");

            GameObject aObj = new GameObject("Audio");
            AudioManager audio = aObj.AddComponent<AudioManager>();
            audio.SetMuted(true);
            Assert(audio.IsMuted, "Audio muted properly");
            audio.SetMuted(false);
            Assert(!audio.IsMuted, "Audio unmuted properly");
        }

        // =========================================================================
        // DETERMINISTIC OUTCOME PROVIDER FOR UNIT & INTEGRATION TESTING
        // =========================================================================

        private class DeterministicOutcomeProvider : IRandomProvider
        {
            private readonly SymbolData[] outcome;
            private readonly SymbolDatabase database;
            private int step = 0;

            public DeterministicOutcomeProvider(SymbolData[] forcedOutcome, SymbolDatabase db)
            {
                outcome = forcedOutcome;
                database = db;
            }

            public int Range(int min, int max)
            {
                if (outcome != null && outcome.Length > 0 && database != null && database.Symbols != null)
                {
                    SymbolData target = outcome[step % outcome.Length];
                    step++;
                    for (int i = 0; i < database.Symbols.Length; i++)
                    {
                        if (database.Symbols[i] == target)
                        {
                            return i;
                        }
                    }
                }
                return min;
            }

            public float Value() { return 0f; }
        }
    }
}
