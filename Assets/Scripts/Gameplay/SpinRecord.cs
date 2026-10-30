using System;
using LuckySpin.SlotMachine.Data;

namespace LuckySpin.SlotMachine.Gameplay
{
    /// <summary>
    /// Stores the outcome and financial transaction details of an individual spin.
    /// </summary>
    [Serializable]
    public class SpinRecord
    {
        public int SpinNumber;
        public SymbolType[] Symbols;
        public int BetPlaced;
        public int PayoutAwarded;
        public bool IsWin;
        public DateTime Timestamp;

        public SpinRecord(int spinNumber, SymbolType[] symbols, int bet, int payout, bool isWin)
        {
            SpinNumber = spinNumber;
            Symbols = symbols;
            BetPlaced = bet;
            PayoutAwarded = payout;
            IsWin = isWin;
            Timestamp = DateTime.Now;
        }

        public string GetOutcomeSummary()
        {
            if (Symbols == null || Symbols.Length < 3) return "---";
            return string.Format("{0} | {1} | {2}", Symbols[0], Symbols[1], Symbols[2]);
        }
    }
}

