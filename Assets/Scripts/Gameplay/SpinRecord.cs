using System;
using LuckySpin.SlotMachine.Data;

namespace LuckySpin.SlotMachine.Gameplay
{
    /// <summary>
    /// Stores the outcome and financial transaction details of an individual spin.
    /// Centralized audit record tracking initial bet, winnings, net profit/loss, and resulting balance.
    /// </summary>
    [Serializable]
    public class SpinRecord
    {
        public int SpinNumber;
        public SymbolType[] Symbols;
        public int BetPlaced;
        public int PayoutAwarded;
        public bool IsWin;
        public int BalanceAfter;
        public string TransactionId;
        public DateTime Timestamp;

        public int NetProfitLoss
        {
            get { return PayoutAwarded - BetPlaced; }
        }

        public SpinRecord(int spinNumber, SymbolType[] symbols, int bet, int payout, bool isWin)
            : this(spinNumber, symbols, bet, payout, isWin, 0, string.Empty)
        {
        }

        public SpinRecord(int spinNumber, SymbolType[] symbols, int bet, int payout, bool isWin, int balanceAfter, string transactionId = "")
        {
            SpinNumber = spinNumber;
            Symbols = symbols;
            BetPlaced = bet;
            PayoutAwarded = payout;
            IsWin = isWin;
            BalanceAfter = balanceAfter;
            TransactionId = transactionId;
            Timestamp = DateTime.Now;
        }

        public string GetOutcomeSummary()
        {
            if (Symbols == null || Symbols.Length < 3) return "---";
            return string.Format("{0} | {1} | {2}", Symbols[0], Symbols[1], Symbols[2]);
        }
    }
}
