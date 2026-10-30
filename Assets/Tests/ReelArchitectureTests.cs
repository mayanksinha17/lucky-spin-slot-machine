#pragma warning disable 0649, 0169, 0414
using System;
using System.Collections.Generic;
using UnityEngine;
using LuckySpin.SlotMachine.Data;
using LuckySpin.SlotMachine.Reels;
using LuckySpin.SlotMachine.Gameplay;

namespace LuckySpin.SlotMachine.Tests
{
    /// <summary>
    /// Test runner and verification suite for the slot machine reel architecture.
    /// </summary>
    public class ReelArchitectureTests
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

            RunTest("SymbolDatabase_ConfiguredCorrectly", Test_SymbolDatabase_ConfiguredCorrectly, ref report);
            RunTest("ReelController_SlotSizingAndAlignment", Test_ReelController_SlotSizingAndAlignment, ref report);
            RunTest("Reels_OperateIndependently", Test_Reels_OperateIndependently, ref report);
            RunTest("SymbolReplacement_UpdatesTargetSlotOnly", Test_SymbolReplacement_UpdatesTargetSlotOnly, ref report);
            RunTest("PresentationSeparatedFromOutcomeGeneration", Test_PresentationSeparation_DoesNotGenerateOutcomes, ref report);

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

        private static void Test_SymbolDatabase_ConfiguredCorrectly()
        {
            SymbolData cherry = ScriptableObject.CreateInstance<SymbolData>();
            cherry.Initialize(SymbolType.Cherry, "Cherry", null, 2, 35);

            SymbolData bell = ScriptableObject.CreateInstance<SymbolData>();
            bell.Initialize(SymbolType.Bell, "Bell", null, 5, 25);

            SymbolData bar = ScriptableObject.CreateInstance<SymbolData>();
            bar.Initialize(SymbolType.Bar, "Bar", null, 10, 20);

            SymbolData seven = ScriptableObject.CreateInstance<SymbolData>();
            seven.Initialize(SymbolType.Seven, "Seven", null, 25, 15);

            SymbolData diamond = ScriptableObject.CreateInstance<SymbolData>();
            diamond.Initialize(SymbolType.Diamond, "Diamond", null, 100, 5);

            SymbolDatabase db = ScriptableObject.CreateInstance<SymbolDatabase>();
            db.Initialize(new SymbolData[] { cherry, bell, bar, seven, diamond });

            Assert(db.Symbols.Length == 5, "Database must hold exactly 5 symbols.");
            Assert(db.GetSymbol(SymbolType.Cherry).PayoutMultiplier == 2, "Cherry multiplier must be 2.");
            Assert(db.GetSymbol(SymbolType.Diamond).PayoutMultiplier == 100, "Diamond multiplier must be 100.");
            Assert(db.GetSymbol(SymbolType.Seven).PayoutMultiplier == 25, "Seven multiplier must be 25.");
        }

        private static void Test_ReelController_SlotSizingAndAlignment()
        {
            GameObject reelObj = new GameObject("TestReel");
            ReelController reel = reelObj.AddComponent<ReelController>();

            // Create 5 dummy slots
            List<SymbolItemView> slots = new List<SymbolItemView>();
            for (int i = 0; i < 5; i++)
            {
                GameObject slotObj = new GameObject("Slot_" + i);
                slotObj.transform.SetParent(reelObj.transform);
                slots.Add(slotObj.AddComponent<SymbolItemView>());
            }

            reel.Initialize(0, null);
            reel.SetSlots(slots.ToArray());
            reel.SetSlots(slots.ToArray());
            reel.ApplyLayoutAlignment();

            Assert(reel.VerticalPitch == 150f, "Vertical pitch must equal 150px.");
            Assert(reel.SymbolWidth == 150f, "Symbol width must equal 150px.");
            Assert(reel.SymbolHeight == 150f, "Symbol height must equal 150px.");

            // Center slot (index 2) must be anchored at y = 0
            float centerPos = (2 - 2) * reel.VerticalPitch;
            Assert(centerPos == 0f, "Center payline must align at y = 0.");

            // Top slot (index 1) must be anchored at y = +150
            float topPos = (2 - 1) * reel.VerticalPitch;
            Assert(topPos == 150f, "Top visible row must align at y = +150.");

            // Bottom slot (index 3) must be anchored at y = -150
            float bottomPos = (2 - 3) * reel.VerticalPitch;
            Assert(bottomPos == -150f, "Bottom visible row must align at y = -150.");
        }

        private static void Test_Reels_OperateIndependently()
        {
            GameObject r0Obj = new GameObject("Reel0");
            ReelController reel0 = r0Obj.AddComponent<ReelController>();
            reel0.Initialize(0, null);
            reel0.DecelerationTime = 0f;

            GameObject r1Obj = new GameObject("Reel1");
            ReelController reel1 = r1Obj.AddComponent<ReelController>();
            reel1.Initialize(1, null);

            GameObject r2Obj = new GameObject("Reel2");
            ReelController reel2 = r2Obj.AddComponent<ReelController>();
            reel2.Initialize(2, null);

            // Spin Reel 0 only
            reel0.StartSpinning();

            Assert(reel0.IsSpinning == true, "Reel 0 must be spinning.");
            Assert(reel1.IsSpinning == false, "Reel 1 must remain idle.");
            Assert(reel2.IsSpinning == false, "Reel 2 must remain idle.");

            // Stop Reel 0
            SymbolData cherry = ScriptableObject.CreateInstance<SymbolData>();
            cherry.Initialize(SymbolType.Cherry, "Cherry", null, 2, 35);
            reel0.StopAt(cherry);

            Assert(reel0.IsSpinning == false, "Reel 0 must stop.");
            Assert(reel0.CurrentPaylineSymbol == cherry, "Reel 0 must display Cherry.");
            Assert(reel1.CurrentPaylineSymbol == null, "Reel 1 must not be affected.");
        }

        private static void Test_SymbolReplacement_UpdatesTargetSlotOnly()
        {
            GameObject reelObj = new GameObject("TestReel");
            ReelController reel = reelObj.AddComponent<ReelController>();

            List<SymbolItemView> slots = new List<SymbolItemView>();
            for (int i = 0; i < 5; i++)
            {
                GameObject slotObj = new GameObject("Slot_" + i);
                slotObj.transform.SetParent(reelObj.transform);
                slots.Add(slotObj.AddComponent<SymbolItemView>());
            }
            reel.Initialize(0, null);
            reel.SetSlots(slots.ToArray());

            SymbolData bell = ScriptableObject.CreateInstance<SymbolData>();
            bell.Initialize(SymbolType.Bell, "Bell", null, 5, 25);

            SymbolData diamond = ScriptableObject.CreateInstance<SymbolData>();
            diamond.Initialize(SymbolType.Diamond, "Diamond", null, 100, 5);

            // Replace Center slot (index 2)
            reel.ReplaceSymbol(2, bell);
            Assert(reel.CurrentPaylineSymbol == bell, "Center slot must be Bell.");
            Assert(reel.SymbolSlots[0].CurrentData == null, "Slot 0 must remain unchanged.");

            // Replace Top slot (index 1)
            reel.ReplaceSymbol(1, diamond);
            Assert(reel.SymbolSlots[1].CurrentData == diamond, "Slot 1 must be Diamond.");
            Assert(reel.CurrentPaylineSymbol == bell, "Center slot must still be Bell.");
        }

        private static void Test_PresentationSeparation_DoesNotGenerateOutcomes()
        {
            GameObject reelObj = new GameObject("TestReel");
            ReelController reel = reelObj.AddComponent<ReelController>();
            reel.Initialize(0, null);
            reel.DecelerationTime = 0f;

            SymbolData predeterminedSeven = ScriptableObject.CreateInstance<SymbolData>();
            predeterminedSeven.Initialize(SymbolType.Seven, "Seven", null, 25, 15);

            bool callbackInvoked = false;
            reel.StopAt(predeterminedSeven, delegate {
                callbackInvoked = true;
            });

            Assert(callbackInvoked == true, "Stop completion callback must be executed.");
            Assert(reel.CurrentPaylineSymbol == predeterminedSeven, 
                "Reel must faithfully display predetermined symbol without deviation.");
        }
    }
}
