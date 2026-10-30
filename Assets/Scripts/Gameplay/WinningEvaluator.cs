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
        public bool IsWildAssisted;

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
            res.IsWildAssisted = false;
            return res;
        }

        public static EvaluationResult Win(SymbolData symbol, int bet, int multiplier, bool wildAssisted = false)
        {
            EvaluationResult res = new EvaluationResult();
            res.IsWin = true;
            res.WinningType = symbol != null ? symbol.Type : (SymbolType)(-1);
            res.WinningSymbol = symbol;
            res.Multiplier = multiplier;
            res.BetPlaced = bet;
            res.Payout = bet * multiplier;
            res.IsWildAssisted = wildAssisted;
            string name = symbol != null ? symbol.DisplayName.ToUpper() : "SYMBOL";
            res.Message = wildAssisted
                ? string.Format("WILD WIN! 3x {0} (+{1:N0} G)", name, res.Payout)
                : string.Format("WINNER! 3x {0} (+{1:N0} G)", name, res.Payout);
            return res;
        }
    }

    /// <summary>
    /// Pure gameplay logic component that evaluates slot payline results independently of visual animation.
    /// Mandatory winning rule: 3 identical symbols on the payline.
    /// Modular Bonus: Configurable Wild symbol substitution.
    /// </summary>
    public class WinningEvaluator : MonoBehaviour
    {
        [Header("Bonus Features")]
        [Tooltip("When enabled, Wild symbols can substitute for any standard symbol to form a winning line.")]
        [SerializeField] private bool enableWildSubstitutions = false;

        public bool EnableWildSubstitutions
        {
            get { return enableWildSubstitutions; }
            set { enableWildSubstitutions = value; }
        }

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

            // 1. Exact 3-of-a-kind match (Mandatory Winning Rule)
            if (s0.Type == s1.Type && s1.Type == s2.Type)
            {
                int multiplier = Mathf.Max(1, s0.PayoutMultiplier);
                return EvaluationResult.Win(s0, betAmount, multiplier, false);
            }

            // 2. Modular Wild Symbol Substitution (Optional Bonus Feature)
            if (enableWildSubstitutions)
            {
                bool isWild0 = s0.Type == SymbolType.Wild;
                bool isWild1 = s1.Type == SymbolType.Wild;
                bool isWild2 = s2.Type == SymbolType.Wild;

                int wildCount = (isWild0 ? 1 : 0) + (isWild1 ? 1 : 0) + (isWild2 ? 1 : 0);

                if (wildCount == 2)
                {
                    // 2 Wilds + 1 Standard symbol: substitutes as 3 of the standard symbol
                    SymbolData standard = !isWild0 ? s0 : (!isWild1 ? s1 : s2);
                    int multiplier = Mathf.Max(1, standard.PayoutMultiplier);
                    return EvaluationResult.Win(standard, betAmount, multiplier, true);
                }
                else if (wildCount == 1)
                {
                    // 1 Wild + 2 matching standard symbols: substitutes as 3 of the matching symbol
                    if (!isWild0 && !isWild1 && s0.Type == s1.Type)
                    {
                        return EvaluationResult.Win(s0, betAmount, Mathf.Max(1, s0.PayoutMultiplier), true);
                    }
                    if (!isWild0 && !isWild2 && s0.Type == s2.Type)
                    {
                        return EvaluationResult.Win(s0, betAmount, Mathf.Max(1, s0.PayoutMultiplier), true);
                    }
                    if (!isWild1 && !isWild2 && s1.Type == s2.Type)
                    {
                        return EvaluationResult.Win(s1, betAmount, Mathf.Max(1, s1.PayoutMultiplier), true);
                    }
                }
            }

            // Otherwise, it's a loss
            return EvaluationResult.Loss(betAmount);
        }
    }
}
