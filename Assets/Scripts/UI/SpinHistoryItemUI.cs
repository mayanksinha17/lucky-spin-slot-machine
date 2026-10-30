#pragma warning disable 0649, 0169, 0414
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LuckySpin.SlotMachine.Gameplay;

namespace LuckySpin.SlotMachine.UI
{
    /// <summary>
    /// Displays a single spin record row in the spin history panel.
    /// </summary>
    public class SpinHistoryItemUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI spinNumberText;
        [SerializeField] private TextMeshProUGUI outcomeText;
        [SerializeField] private TextMeshProUGUI betText;
        [SerializeField] private TextMeshProUGUI resultText;
        [SerializeField] private Image statusBadge;

        public void Bind(SpinRecord record)
        {
            if (record == null) return;

            if (spinNumberText != null)
            {
                spinNumberText.text = string.Format("#{0}", record.SpinNumber);
            }

            if (outcomeText != null)
            {
                outcomeText.text = record.GetOutcomeSummary();
            }

            if (betText != null)
            {
                betText.text = string.Format("BET {0}", record.BetPlaced);
            }

            if (resultText != null)
            {
                if (record.IsWin)
                {
                    resultText.text = string.Format("+{0} G", record.PayoutAwarded);
                    resultText.color = new Color(1f, 0.85f, 0.2f, 1f); // Gold
                }
                else
                {
                    resultText.text = string.Format("-{0} G", record.BetPlaced);
                    resultText.color = new Color(0.7f, 0.7f, 0.75f, 1f); // Muted silver
                }
            }

            if (statusBadge != null)
            {
                statusBadge.color = record.IsWin 
                    ? new Color(0.2f, 0.85f, 0.3f, 0.8f) // Green badge
                    : new Color(0.5f, 0.2f, 0.25f, 0.8f); // Red/purple badge
            }
        }
    }
}
