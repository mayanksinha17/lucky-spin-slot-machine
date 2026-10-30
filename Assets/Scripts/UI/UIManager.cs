#pragma warning disable 0649, 0169, 0414
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace LuckySpin.SlotMachine.UI
{
    /// <summary>
    /// Controls UI elements including HUD balance, bet selector, spin button states, and modal dialogs.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("HUD Text Displays")]
        [SerializeField] private TextMeshProUGUI balanceText;
        [SerializeField] private TextMeshProUGUI betText;
        [SerializeField] private TextMeshProUGUI winAmountText;
        [SerializeField] private TextMeshProUGUI messageBannerText;

        [Header("Primary Action Buttons")]
        [SerializeField] private Button spinButton;
        [SerializeField] private Button increaseBetButton;
        [SerializeField] private Button decreaseBetButton;
        [SerializeField] private Button maxBetButton;

        [Header("Modals & Overlays")]
        [SerializeField] private GameObject paytableModal;
        [SerializeField] private GameObject winNotificationBanner;

        public event Action OnSpinClicked;
        public event Action OnIncreaseBetClicked;
        public event Action OnDecreaseBetClicked;
        public event Action OnMaxBetClicked;

        private void Awake()
        {
            if (spinButton != null)
            {
                spinButton.onClick.AddListener(delegate { if (OnSpinClicked != null) OnSpinClicked(); });
            }
            if (increaseBetButton != null)
            {
                increaseBetButton.onClick.AddListener(delegate { if (OnIncreaseBetClicked != null) OnIncreaseBetClicked(); });
            }
            if (decreaseBetButton != null)
            {
                decreaseBetButton.onClick.AddListener(delegate { if (OnDecreaseBetClicked != null) OnDecreaseBetClicked(); });
            }
            if (maxBetButton != null)
            {
                maxBetButton.onClick.AddListener(delegate { if (OnMaxBetClicked != null) OnMaxBetClicked(); });
            }
        }

        public void UpdateBalance(int newBalance)
        {
            if (balanceText != null)
            {
                balanceText.text = string.Format("{0:N0} G", newBalance);
            }
        }

        public void UpdateBet(int newBet)
        {
            if (betText != null)
            {
                betText.text = string.Format("BET {0:N0}", newBet);
            }
        }

        public void SetSpinInteractable(bool interactable)
        {
            if (spinButton != null)
            {
                spinButton.interactable = interactable;
            }
        }

        public void DisplayMessage(string message)
        {
            if (messageBannerText != null)
            {
                messageBannerText.text = message;
            }
        }
    }
}
