#pragma warning disable 0649, 0169, 0414
using System;
using UnityEngine;
using LuckySpin.SlotMachine.Data;

namespace LuckySpin.SlotMachine.Gameplay
{
    /// <summary>
    /// Result structure produced by the WinningEvaluator.
    /// </summary>
    [Serializable]
    public struct EvaluationResult
    {
        public bool IsWin;
        public SymbolType WinningType;
        public SymbolData WinningSymbol;
        public int Multiplier;
        public int BetPlaced;
        public int Payout;
        public string Message;

        public static EvaluationResult Loss(int bet)
        {
            EvaluationResult res = new EvaluationResult();
            res.IsWin = false;
            res.WinningType = (SymbolType)(-1);
            res.WinningSymbol = null;
            res.Multiplier = 0;
            res.BetPlaced = bet;
            res.Payout = 0;
            res.Message = "NO MATCH - TRY AGAIN";
            return res;
        }

        public static EvaluationResult Win(SymbolData symbol, int bet, int multiplier)
        {
            EvaluationResult res = new EvaluationResult();
            res.IsWin = true;
            res.WinningType = symbol != null ? symbol.Type : (SymbolType)(-1);
            res.WinningSymbol = symbol;
            res.Multiplier = multiplier;
            res.BetPlaced = bet;
            res.Payout = bet * multiplier;
            string name = symbol != null ? symbol.DisplayName.ToUpper() : "SYMBOL";
            res.Message = string.Format("WINNER! 3x {0} (+{1:N0} G)", name, res.Payout);
            return res;
        }
    }

    /// <summary>
    /// Pure gameplay logic component that evaluates slot payline results independently of visual animation.
    /// Mandatory winning rule: 3 identical symbols on the payline.
    /// </summary>
    public class WinningEvaluator : MonoBehaviour
    {
        /// <summary>
        /// Evaluates the 3 center payline symbols and calculates payout based on the bet placed.
        /// Formula: Payout = Bet Amount * Symbol Multiplier
        /// </summary>
        public EvaluationResult Evaluate(SymbolData[] paylineSymbols, int betAmount)
        {
            if (paylineSymbols == null || paylineSymbols.Length < 3)
            {
                return EvaluationResult.Loss(betAmount);
            }

            SymbolData s0 = paylineSymbols[0];
            SymbolData s1 = paylineSymbols[1];
            SymbolData s2 = paylineSymbols[2];

            // Validate non-null
            if (s0 == null || s1 == null || s2 == null)
            {
                return EvaluationResult.Loss(betAmount);
            }

            // 3 matching symbols required for a win
            if (s0.Type == s1.Type && s1.Type == s2.Type)
            {
                int multiplier = Mathf.Max(1, s0.PayoutMultiplier);
                return EvaluationResult.Win(s0, betAmount, multiplier);
            }

            // Otherwise, it's a loss
            return EvaluationResult.Loss(betAmount);
        }
    }
}
