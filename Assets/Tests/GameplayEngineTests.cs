#pragma warning disable 0649, 0169, 0414
using System;
using System.Collections.Generic;
using UnityEngine;
using LuckySpin.SlotMachine.Data;
using LuckySpin.SlotMachine.Gameplay;

namespace LuckySpin.SlotMachine.Tests
{
    /// <summary>
    /// Automated test suite verifying RNG generation, winning evaluator logic, losing combinations, and payout mathematics.
    /// </summary>
    public class GameplayEngineTests
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

            RunTest("AllFiveWinningCombinations", Test_AllFiveWinningCombinations, ref report);
            RunTest("LosingCombinationsDetectedAccurately", Test_LosingCombinationsDetectedAccurately, ref report);
            RunTest("PayoutFormulaMatchesSpecification", Test_PayoutFormulaMatchesSpecification, ref report);
            RunTest("PayoutManager_CreditValidationAndDeduction", Test_PayoutManager_CreditValidationAndDeduction, ref report);
            RunTest("PayoutManager_BetAdjustmentBounds", Test_PayoutManager_BetAdjustmentBounds, ref report);
            RunTest("RandomOutcomeGenerator_AllSymbolsCanAppear", Test_RandomOutcomeGenerator_AllSymbolsCanAppear, ref report);
            RunTest("RandomOutcomeGenerator_NoHardcodedSequences", Test_RandomOutcomeGenerator_NoHardcodedSequences, ref report);
            RunTest("RNGDecoupledFromVisualAnimation", Test_RNGDecoupledFromVisualAnimation, ref report);

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

        private static SymbolDatabase CreateMockDatabase(out SymbolData cherry, out SymbolData bell, out SymbolData bar, out SymbolData seven, out SymbolData diamond)
        {
            cherry = ScriptableObject.CreateInstance<SymbolData>();
            cherry.Initialize(SymbolType.Cherry, "Cherry", null, 2, 35);

            bell = ScriptableObject.CreateInstance<SymbolData>();
            bell.Initialize(SymbolType.Bell, "Bell", null, 5, 25);

            bar = ScriptableObject.CreateInstance<SymbolData>();
            bar.Initialize(SymbolType.Bar, "Double BAR", null, 10, 20);

            seven = ScriptableObject.CreateInstance<SymbolData>();
            seven.Initialize(SymbolType.Seven, "Lucky 7", null, 25, 15);

            diamond = ScriptableObject.CreateInstance<SymbolData>();
            diamond.Initialize(SymbolType.Diamond, "Diamond", null, 100, 5);

            SymbolDatabase db = ScriptableObject.CreateInstance<SymbolDatabase>();
            db.Initialize(new SymbolData[] { cherry, bell, bar, seven, diamond });
            return db;
        }

        private static void Test_AllFiveWinningCombinations()
        {
            SymbolData cherry, bell, bar, seven, diamond;
            CreateMockDatabase(out cherry, out bell, out bar, out seven, out diamond);

            WinningEvaluator evaluator = new GameObject("Evaluator").AddComponent<WinningEvaluator>();
            int bet = 10;

            // 1. Cherry | Cherry | Cherry -> 2x
            EvaluationResult resCherry = evaluator.Evaluate(new SymbolData[] { cherry, cherry, cherry }, bet);
            Assert(resCherry.IsWin == true, "3x Cherry must be a win.");
            Assert(resCherry.Multiplier == 2, "Cherry multiplier must be 2.");
            Assert(resCherry.Payout == 20, "Cherry payout for 10 bet must be 20.");
            Assert(resCherry.WinningType == SymbolType.Cherry, "Winning type must be Cherry.");

            // 2. Bell | Bell | Bell -> 5x
            EvaluationResult resBell = evaluator.Evaluate(new SymbolData[] { bell, bell, bell }, bet);
            Assert(resBell.IsWin == true, "3x Bell must be a win.");
            Assert(resBell.Multiplier == 5, "Bell multiplier must be 5.");
            Assert(resBell.Payout == 50, "Bell payout for 10 bet must be 50.");

            // 3. BAR | BAR | BAR -> 10x
            EvaluationResult resBar = evaluator.Evaluate(new SymbolData[] { bar, bar, bar }, bet);
            Assert(resBar.IsWin == true, "3x BAR must be a win.");
            Assert(resBar.Multiplier == 10, "BAR multiplier must be 10.");
            Assert(resBar.Payout == 100, "BAR payout for 10 bet must be 100.");

            // 4. Seven | Seven | Seven -> 25x
            EvaluationResult resSeven = evaluator.Evaluate(new SymbolData[] { seven, seven, seven }, bet);
            Assert(resSeven.IsWin == true, "3x Seven must be a win.");
            Assert(resSeven.Multiplier == 25, "Seven multiplier must be 25.");
            Assert(resSeven.Payout == 250, "Seven payout for 10 bet must be 250.");

            // 5. Diamond | Diamond | Diamond -> 100x (Jackpot)
            EvaluationResult resDiamond = evaluator.Evaluate(new SymbolData[] { diamond, diamond, diamond }, bet);
            Assert(resDiamond.IsWin == true, "3x Diamond must be a win.");
            Assert(resDiamond.Multiplier == 100, "Diamond multiplier must be 100.");
            Assert(resDiamond.Payout == 1000, "Diamond payout for 10 bet must be 1000.");
        }

        private static void Test_LosingCombinationsDetectedAccurately()
        {
            SymbolData cherry, bell, bar, seven, diamond;
            CreateMockDatabase(out cherry, out bell, out bar, out seven, out diamond);

            WinningEvaluator evaluator = new GameObject("Evaluator").AddComponent<WinningEvaluator>();
            int bet = 10;

            // Scenario A: Cherry | Diamond | Bell (all distinct)
            EvaluationResult res1 = evaluator.Evaluate(new SymbolData[] { cherry, diamond, bell }, bet);
            Assert(res1.IsWin == false, "Cherry | Diamond | Bell must be a loss.");
            Assert(res1.Payout == 0, "Loss payout must be 0.");

            // Scenario B: Bell | Bell | Cherry (two matching, one different)
            EvaluationResult res2 = evaluator.Evaluate(new SymbolData[] { bell, bell, cherry }, bet);
            Assert(res2.IsWin == false, "Bell | Bell | Cherry must be a loss.");
            Assert(res2.Payout == 0, "Partial match payout must be 0.");

            // Scenario C: Seven | BAR | Seven (split match)
            EvaluationResult res3 = evaluator.Evaluate(new SymbolData[] { seven, bar, seven }, bet);
            Assert(res3.IsWin == false, "Seven | BAR | Seven must be a loss.");

            // Scenario D: Null/Incomplete inputs
            EvaluationResult resNull = evaluator.Evaluate(null, bet);
            Assert(resNull.IsWin == false, "Null symbols must safely evaluate as a loss.");

            EvaluationResult resShort = evaluator.Evaluate(new SymbolData[] { cherry, cherry }, bet);
            Assert(resShort.IsWin == false, "Less than 3 symbols must evaluate as a loss.");
        }

        private static void Test_PayoutFormulaMatchesSpecification()
        {
            SymbolData cherry, bell, bar, seven, diamond;
            CreateMockDatabase(out cherry, out bell, out bar, out seven, out diamond);

            WinningEvaluator evaluator = new GameObject("Evaluator").AddComponent<WinningEvaluator>();

            // Formula: Payout = Bet Amount * Symbol Multiplier
            int[] testBets = new int[] { 10, 20, 50, 100, 250 };
            for (int i = 0; i < testBets.Length; i++)
            {
                int b = testBets[i];
                EvaluationResult res = evaluator.Evaluate(new SymbolData[] { diamond, diamond, diamond }, b);
                int expected = b * 100;
                Assert(res.Payout == expected, 
                    string.Format("Payout mismatch for bet {0}: got {1}, expected {2}", b, res.Payout, expected));
            }
        }

        private static void Test_PayoutManager_CreditValidationAndDeduction()
        {
            PayoutManager pm = new GameObject("PM").AddComponent<PayoutManager>();
            pm.Initialize(1000, 10, 10, 100, 10);

            Assert(pm.CurrentBalance == 1000, "Initial balance must be 1000.");
            Assert(pm.HasSufficientCredits() == true, "Must have sufficient credits for 10 bet.");

            // Deduct bet 10
            bool deducted = pm.TryDeductBet();
            Assert(deducted == true, "Bet deduction must succeed.");
            Assert(pm.CurrentBalance == 990, "Balance must be 990 after 10 bet deduction.");

            // Award win payout of 250
            SymbolData cherry, bell, bar, seven, diamond;
            CreateMockDatabase(out cherry, out bell, out bar, out seven, out diamond);
            EvaluationResult win = EvaluationResult.Win(seven, 10, 25); // 250 payout
            int awarded = pm.AwardPayout(win);

            Assert(awarded == 250, "Awarded payout must be 250.");
            Assert(pm.CurrentBalance == 1240, "Balance must be 990 + 250 = 1240.");

            // Test Insufficient Funds
            pm.ResetBalance(5);
            Assert(pm.HasSufficientCredits() == false, "Balance 5 cannot cover bet 10.");

            bool failDeduct = pm.TryDeductBet();
            Assert(failDeduct == false, "Deduction must fail when credits are insufficient.");
            Assert(pm.CurrentBalance == 5, "Balance must remain unchanged when deduction fails.");
        }

        private static void Test_PayoutManager_BetAdjustmentBounds()
        {
            PayoutManager pm = new GameObject("PM").AddComponent<PayoutManager>();
            pm.Initialize(1000, 10, 10, 100, 10);

            // Step increase: 10 -> 20 -> 30
            pm.IncreaseBet();
            Assert(pm.CurrentBet == 20, "Bet must increase to 20.");
            pm.IncreaseBet();
            Assert(pm.CurrentBet == 30, "Bet must increase to 30.");

            // Step decrease: 30 -> 20
            pm.DecreaseBet();
            Assert(pm.CurrentBet == 20, "Bet must decrease to 20.");

            // Minimum Clamp: cannot go below 10
            pm.DecreaseBet(); // to 10
            pm.DecreaseBet(); // should remain 10
            Assert(pm.CurrentBet == 10, "Bet must not decrease below minimum 10.");

            // Maximum Bet clamp
            pm.SetMaxBet();
            Assert(pm.CurrentBet == 100, "Max bet must set bet to 100.");
            pm.IncreaseBet(); // should remain 100
            Assert(pm.CurrentBet == 100, "Bet must not exceed maximum 100.");
        }

        private static void Test_RandomOutcomeGenerator_AllSymbolsCanAppear()
        {
            SymbolData cherry, bell, bar, seven, diamond;
            SymbolDatabase db = CreateMockDatabase(out cherry, out bell, out bar, out seven, out diamond);

            RandomOutcomeGenerator rog = new GameObject("ROG").AddComponent<RandomOutcomeGenerator>();
            rog.Database = db;
            rog.SetRandomProvider(new SeededRandomProvider(4242));

            HashSet<SymbolType> observedSymbols = new HashSet<SymbolType>();

            // Generate 300 outcomes
            for (int i = 0; i < 300; i++)
            {
                SymbolData[] outcome = rog.GenerateOutcome(db);
                Assert(outcome.Length == 3, "Outcome must always contain 3 symbols.");
                for (int s = 0; s < outcome.Length; s++)
                {
                    observedSymbols.Add(outcome[s].Type);
                }
            }

            // Verify all 5 symbols appeared
            Assert(observedSymbols.Contains(SymbolType.Cherry), "Cherry must be generated.");
            Assert(observedSymbols.Contains(SymbolType.Bell), "Bell must be generated.");
            Assert(observedSymbols.Contains(SymbolType.Bar), "BAR must be generated.");
            Assert(observedSymbols.Contains(SymbolType.Seven), "Seven must be generated.");
            Assert(observedSymbols.Contains(SymbolType.Diamond), "Diamond must be generated.");
            Assert(observedSymbols.Count == 5, "All 5 symbols must appear in the RNG generation.");
        }

        private static void Test_RandomOutcomeGenerator_NoHardcodedSequences()
        {
            SymbolData cherry, bell, bar, seven, diamond;
            SymbolDatabase db = CreateMockDatabase(out cherry, out bell, out bar, out seven, out diamond);

            RandomOutcomeGenerator rog = new GameObject("ROG").AddComponent<RandomOutcomeGenerator>();
            rog.Database = db;
            rog.SetRandomProvider(new SeededRandomProvider(999));

            // Verify successive spins are non-identical
            SymbolData[] spin1 = rog.GenerateOutcome(db);
            SymbolData[] spin2 = rog.GenerateOutcome(db);
            SymbolData[] spin3 = rog.GenerateOutcome(db);

            string s1 = string.Format("{0}-{1}-{2}", spin1[0].Type, spin1[1].Type, spin1[2].Type);
            string s2 = string.Format("{0}-{1}-{2}", spin2[0].Type, spin2[1].Type, spin2[2].Type);
            string s3 = string.Format("{0}-{1}-{2}", spin3[0].Type, spin3[1].Type, spin3[2].Type);

            Assert(s1 != s2 || s2 != s3, "RNG outcomes must vary across spins without repeating sequences.");
        }

        private static void Test_RNGDecoupledFromVisualAnimation()
        {
            SymbolData cherry, bell, bar, seven, diamond;
            SymbolDatabase db = CreateMockDatabase(out cherry, out bell, out bar, out seven, out diamond);

            // Create outcome generator with no Scene, Canvas, or Reel Controllers
            RandomOutcomeGenerator rog = new GameObject("HeadlessROG").AddComponent<RandomOutcomeGenerator>();
            rog.Database = db;

            SymbolData[] outcome = rog.GenerateOutcome(db);
            Assert(outcome != null && outcome.Length == 3, 
                "RNG outcome generation must operate completely headless without visual dependencies.");
        }
    }
}
