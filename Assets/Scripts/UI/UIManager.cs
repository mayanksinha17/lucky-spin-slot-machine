#pragma warning disable 0649, 0169, 0414
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using LuckySpin.SlotMachine.Animations;

namespace LuckySpin.SlotMachine.UI
{
    /// <summary>
    /// Master UI manager controlling HUD balance, bet deck, spin button, result banners, status indicator,
    /// particle animations, and UI transitions.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("HUD Displays")]
        [SerializeField] private TextMeshProUGUI balanceText;
        [SerializeField] private TextMeshProUGUI betText;
        [SerializeField] private TextMeshProUGUI gameTitleText;
        [SerializeField] private TextMeshProUGUI statusIndicatorText;

        [Header("Primary Action Buttons")]
        [SerializeField] private Button spinButton;
        [SerializeField] private RectTransform spinButtonRect;
        [SerializeField] private Button increaseBetButton;
        [SerializeField] private Button decreaseBetButton;
        [SerializeField] private Button maxBetButton;

        [Header("Quick Bet Preset Buttons")]
        [SerializeField] private Button bet10Button;
        [SerializeField] private Button bet50Button;
        [SerializeField] private Button bet100Button;
        [SerializeField] private Sprite presetNormalSprite;
        [SerializeField] private Sprite presetSelectedSprite;

        [Header("Result Notification Area")]
        [SerializeField] private GameObject resultBannerRoot;
        [SerializeField] private RectTransform resultBannerRect;
        [SerializeField] private TextMeshProUGUI resultTitleText;
        [SerializeField] private TextMeshProUGUI resultPayoutText;
        [SerializeField] private Image resultBannerBg;

        [Header("Visual Effects & Particles")]
        [SerializeField] private CoinParticleController coinParticles;

        [Header("Spin History Section")]
        [SerializeField] private SpinHistoryUI spinHistoryUI;
        [SerializeField] private Button historyToggleButton;

        [Header("Paytable Modal")]
        [SerializeField] private GameObject paytableModal;
        [SerializeField] private RectTransform paytableModalRect;
        [SerializeField] private Button paytableOpenButton;
        [SerializeField] private Button paytableCloseButton;

        [Header("Modular Bonus UI")]
        [SerializeField] private Button autoSpinButton;
        [SerializeField] private TextMeshProUGUI autoSpinButtonText;
        [SerializeField] private Button muteToggleButton;
        [SerializeField] private TextMeshProUGUI muteButtonText;

        public event Action OnSpinClicked;
        public event Action OnIncreaseBetClicked;
        public event Action OnDecreaseBetClicked;
        public event Action OnMaxBetClicked;
        public event Action<int> OnQuickBetSelected;
        public event Action OnAutoSpinClicked;
        public event Action OnMuteClicked;

        private void Awake()
        {
            if (spinButtonRect == null && spinButton != null)
            {
                spinButtonRect = spinButton.GetComponent<RectTransform>();
            }

            if (resultBannerRoot != null && resultBannerRect == null)
            {
                resultBannerRect = resultBannerRoot.GetComponent<RectTransform>();
            }

            // Spin Button with punch animation feedback
            if (spinButton != null)
            {
                spinButton.onClick.AddListener(delegate {
                    if (spinButtonRect != null)
                    {
                        StartCoroutine(UIAnimationHelper.PunchScaleRoutine(spinButtonRect, new Vector3(-0.15f, -0.15f, 0f), 0.22f));
                    }
                    if (OnSpinClicked != null) OnSpinClicked();
                });
            }

            // Bet Adjusters
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

            // Quick Bet Presets
            if (bet10Button != null)
            {
                bet10Button.onClick.AddListener(delegate { if (OnQuickBetSelected != null) OnQuickBetSelected(10); });
            }
            if (bet50Button != null)
            {
                bet50Button.onClick.AddListener(delegate { if (OnQuickBetSelected != null) OnQuickBetSelected(50); });
            }
            if (bet100Button != null)
            {
                bet100Button.onClick.AddListener(delegate { if (OnQuickBetSelected != null) OnQuickBetSelected(100); });
            }

            // Paytable Modal Controls
            if (paytableOpenButton != null)
            {
                paytableOpenButton.onClick.AddListener(OpenPaytable);
            }
            if (paytableCloseButton != null)
            {
                paytableCloseButton.onClick.AddListener(ClosePaytable);
            }

            // Modular Bonus Controls
            if (autoSpinButton != null)
            {
                autoSpinButton.onClick.AddListener(delegate {
                    if (OnAutoSpinClicked != null) OnAutoSpinClicked();
                });
            }
            if (muteToggleButton != null)
            {
                muteToggleButton.onClick.AddListener(delegate {
                    if (OnMuteClicked != null) OnMuteClicked();
                });
            }
        }

        public void UpdateAutoSpinDisplay(bool isActive, int remainingSpins)
        {
            if (autoSpinButtonText != null)
            {
                autoSpinButtonText.text = isActive ? string.Format("STOP ({0})", remainingSpins) : "AUTO";
            }
        }

        public void UpdateMuteDisplay(bool isMuted)
        {
            if (muteButtonText != null)
            {
                muteButtonText.text = isMuted ? "MUTED" : "SOUND ON";
            }
        }

        private static readonly Color PresetNormalColor = new Color(0.18f, 0.14f, 0.28f, 0.95f);
        private static readonly Color PresetSelectedColor = new Color(0.85f, 0.65f, 0.15f, 1f);

        private int cachedBalance = 1000;
        private int cachedBet = 10;
        private bool isSpinningActive = false;

        public void UpdateBalance(int newBalance)
        {
            cachedBalance = newBalance;
            if (balanceText != null)
            {
                balanceText.text = string.Format("{0:N0} G", newBalance);
            }
            if (!isSpinningActive)
            {
                RefreshBetControlStates();
            }
        }

        public void UpdateBet(int newBet)
        {
            cachedBet = newBet;
            if (betText != null)
            {
                betText.text = string.Format("BET {0:N0} G", newBet);
            }
            if (!isSpinningActive)
            {
                RefreshBetControlStates();
            }
        }

        public void UpdateStatus(string statusMessage, Color? statusColor = null)
        {
            if (statusIndicatorText != null)
            {
                statusIndicatorText.text = statusMessage;
                if (statusColor.HasValue)
                {
                    statusIndicatorText.color = statusColor.Value;
                }
            }
        }

        public void SetSpinInteractable(bool interactable)
        {
            isSpinningActive = !interactable;
            if (spinButton != null)
            {
                spinButton.interactable = interactable && (cachedBalance >= 10);
            }

            if (!interactable)
            {
                // Disable all bet adjustments during active spin
                if (increaseBetButton != null) increaseBetButton.interactable = false;
                if (decreaseBetButton != null) decreaseBetButton.interactable = false;
                if (maxBetButton != null) maxBetButton.interactable = false;
                if (bet10Button != null) bet10Button.interactable = false;
                if (bet50Button != null) bet50Button.interactable = false;
                if (bet100Button != null) bet100Button.interactable = false;
            }
            else
            {
                RefreshBetControlStates();
            }
        }

        private void RefreshBetControlStates()
        {
            // Minus button: enabled only if bet can be decreased (> 10)
            if (decreaseBetButton != null)
            {
                decreaseBetButton.interactable = (cachedBet > 10);
            }

            // Plus button: enabled only if bet < 100 and affordable
            if (increaseBetButton != null)
            {
                increaseBetButton.interactable = (cachedBet < 100 && cachedBet + 10 <= cachedBalance);
            }

            // Quick presets: enabled if affordable and highlight selected
            if (bet10Button != null)
            {
                bet10Button.interactable = (cachedBalance >= 10);
                SetButtonVisualHighlight(bet10Button, cachedBet == 10);
            }
            if (bet50Button != null)
            {
                bet50Button.interactable = (cachedBalance >= 50);
                SetButtonVisualHighlight(bet50Button, cachedBet == 50);
            }
            if (bet100Button != null)
            {
                bet100Button.interactable = (cachedBalance >= 100);
                SetButtonVisualHighlight(bet100Button, cachedBet == 100);
            }

            // Max button: enabled if current bet is less than max affordable bet
            if (maxBetButton != null)
            {
                int maxAffordable = Mathf.Min(100, cachedBalance);
                maxBetButton.interactable = (cachedBet < maxAffordable && maxAffordable >= 10);
            }

            // Spin button: enabled if balance can afford current bet
            if (spinButton != null && !isSpinningActive)
            {
                spinButton.interactable = (cachedBalance >= cachedBet && cachedBet >= 10);
            }
        }

        private void SetButtonVisualHighlight(Button btn, bool isSelected)
        {
            if (btn == null) return;
            Image img = btn.targetGraphic as Image;
            if (img != null)
            {
                if (presetSelectedSprite != null && presetNormalSprite != null)
                {
                    img.sprite = isSelected ? presetSelectedSprite : presetNormalSprite;
                    img.color = Color.white;
                }
                else
                {
                    img.color = isSelected ? PresetSelectedColor : PresetNormalColor;
                }
            }
        }

        /// <summary>
        /// Displays celebratory win notification banner with pop-in animation, rollup counter, and coin particles.
        /// </summary>
        public void ShowWinBanner(int amount, string symbol)
        {
            if (resultBannerRoot != null)
            {
                resultBannerRoot.SetActive(true);
                if (resultBannerRect != null)
                {
                    StartCoroutine(UIAnimationHelper.PopInRoutine(resultBannerRect, 0.45f));
                }
            }

            if (resultTitleText != null)
            {
                resultTitleText.text = string.Format("WINNER! 3x {0}", symbol.ToUpper());
                resultTitleText.color = new Color(1f, 0.85f, 0.2f, 1f); // Vibrant gold
            }

            if (resultPayoutText != null)
            {
                StartCoroutine(UIAnimationHelper.RollupTextRoutine(resultPayoutText, 0, amount, 0.65f, "+", " G"));
            }

            // Trigger coin fountain explosion
            if (coinParticles != null)
            {
                coinParticles.PlayCoinFountain(Vector2.zero);
            }
        }

        public void ShowLossBanner()
        {
            if (resultBannerRoot != null)
            {
                resultBannerRoot.SetActive(true);
                if (resultBannerRect != null)
                {
                    StartCoroutine(UIAnimationHelper.PopInRoutine(resultBannerRect, 0.35f));
                }
            }
            if (resultTitleText != null)
            {
                resultTitleText.text = "NO MATCH";
                resultTitleText.color = new Color(0.7f, 0.7f, 0.8f, 1f);
            }
            if (resultPayoutText != null)
            {
                resultPayoutText.text = "TRY AGAIN!";
            }
        }

        public void HideResultBanner()
        {
            if (resultBannerRoot != null)
            {
                resultBannerRoot.SetActive(false);
            }
        }

        public void DisplayMessage(string message)
        {
            UpdateStatus(message, new Color(1f, 0.4f, 0.4f, 1f));
        }

        public void ShowInsufficientCreditsNotice(int requiredBet, int currentBalance)
        {
            UpdateStatus(string.Format("NEED {0} G (HAVE {1} G) - PLEASE LOWER BET", requiredBet, currentBalance), new Color(1f, 0.35f, 0.35f, 1f));
        }

        public void OpenPaytable()
        {
            if (paytableModal != null)
            {
                paytableModal.SetActive(true);
                if (paytableModalRect != null)
                {
                    StartCoroutine(UIAnimationHelper.PopInRoutine(paytableModalRect, 0.35f));
                }
            }
        }

        public void ClosePaytable()
        {
            if (paytableModal != null) paytableModal.SetActive(false);
        }

        public SpinHistoryUI HistoryUI { get { return spinHistoryUI; } }
        public CoinParticleController CoinParticles { get { return coinParticles; } }
    }
}
