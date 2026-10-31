#pragma warning disable 0649, 0169, 0414
using System;
using UnityEngine;
using LuckySpin.SlotMachine.Data;
using LuckySpin.SlotMachine.Gameplay;
using LuckySpin.SlotMachine.UI;
using LuckySpin.SlotMachine.Audio;
using LuckySpin.SlotMachine.Bonus;

namespace LuckySpin.SlotMachine.Core
{
    /// <summary>
    /// Master coordinator orchestrating gameplay flow, RNG generation, evaluation, payout distribution, and UI/Audio sync.
    /// Modular Bonus: Integrates Auto-Spin, Jackpot Celebrations, and Win Streak multipliers.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Subsystem Controllers")]
        [SerializeField] private SlotMachineController slotMachine;
        [SerializeField] private RandomOutcomeGenerator outcomeGenerator;
        [SerializeField] private WinningEvaluator winningEvaluator;
        [SerializeField] private PayoutManager payoutManager;
        [SerializeField] private UIManager uiManager;
        [SerializeField] private AudioManager audioManager;
        [SerializeField] private GameHistoryManager historyManager;

        [Header("Modular Bonus Features (Optional)")]
        [SerializeField] private AutoSpinController autoSpinController;
        [SerializeField] private JackpotCelebrationController jackpotCelebration;
        [SerializeField] private WinStreakTracker winStreakTracker;

        [Header("Configuration")]
        [SerializeField] private SymbolDatabase symbolDatabase;

        [Header("Runtime State")]
        [SerializeField] private GameState currentState = GameState.Idle;
        [SerializeField] private EvaluationResult lastResult;
        [SerializeField] private SymbolData[] currentPredeterminedOutcomes;

        public GameState CurrentState { get { return currentState; } }
        public PayoutManager Payouts { get { return payoutManager; } }
        public EvaluationResult LastResult { get { return lastResult; } }
        public AutoSpinController AutoSpin { get { return autoSpinController; } }
        public JackpotCelebrationController Jackpot { get { return jackpotCelebration; } }
        public WinStreakTracker WinStreak { get { return winStreakTracker; } }

        public event Action<GameState> OnStateChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            EnsureComponents();
        }

        private void Start()
        {
            InitializeSubsystems();
            SetState(GameState.Idle);
            UpdateUI();
        }

        private void Update()
        {
            // WebGL & Desktop Keyboard shortcut: Spacebar or Enter to Spin
            if (currentState == GameState.Idle && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)))
            {
                RequestSpin();
            }
        }

        private void EnsureComponents()
        {
            if (payoutManager == null) payoutManager = GetComponent<PayoutManager>();
            if (payoutManager == null) payoutManager = gameObject.AddComponent<PayoutManager>();

            if (outcomeGenerator == null) outcomeGenerator = GetComponent<RandomOutcomeGenerator>();
            if (outcomeGenerator == null) outcomeGenerator = gameObject.AddComponent<RandomOutcomeGenerator>();

            if (winningEvaluator == null) winningEvaluator = GetComponent<WinningEvaluator>();
            if (winningEvaluator == null) winningEvaluator = gameObject.AddComponent<WinningEvaluator>();
        }

        private void InitializeSubsystems()
        {
            if (outcomeGenerator != null && symbolDatabase != null)
            {
                outcomeGenerator.Database = symbolDatabase;
            }

            if (payoutManager != null)
            {
                payoutManager.OnBalanceChanged += delegate(int bal) {
                    if (uiManager != null) uiManager.UpdateBalance(bal);
                    if (currentState == GameState.InsufficientFunds && payoutManager.HasSufficientCredits())
                    {
                        SetState(GameState.Idle);
                    }
                };
                payoutManager.OnBetChanged += delegate(int bet) {
                    if (uiManager != null) uiManager.UpdateBet(bet);
                    if (currentState == GameState.InsufficientFunds && payoutManager.HasSufficientCredits())
                    {
                        SetState(GameState.Idle);
                    }
                };
                payoutManager.OnInsufficientFunds += delegate {
                    SetState(GameState.InsufficientFunds);
                };
                payoutManager.OnInsufficientFundsDetailed += delegate(int req, int bal) {
                    SetState(GameState.InsufficientFunds);
                    if (uiManager != null) uiManager.ShowInsufficientCreditsNotice(req, bal);
                };
            }

            if (uiManager != null)
            {
                uiManager.OnSpinClicked += RequestSpin;
                uiManager.OnIncreaseBetClicked += delegate {
                    if (payoutManager != null && payoutManager.IncreaseBet())
                    {
                        if (audioManager != null) audioManager.PlayButtonClick();
                    }
                };
                uiManager.OnDecreaseBetClicked += delegate {
                    if (payoutManager != null && payoutManager.DecreaseBet())
                    {
                        if (audioManager != null) audioManager.PlayButtonClick();
                    }
                };
                uiManager.OnMaxBetClicked += delegate {
                    if (payoutManager != null)
                    {
                        payoutManager.SetMaxBet();
                        if (audioManager != null) audioManager.PlayButtonClick();
                    }
                };
                uiManager.OnQuickBetSelected += delegate(int b) {
                    if (payoutManager != null)
                    {
                        payoutManager.SetBet(b);
                        if (audioManager != null) audioManager.PlayButtonClick();
                    }
                };
            }

            if (slotMachine != null)
            {
                slotMachine.OnAllReelsStopped += HandleAllReelsStopped;
            }

            if (historyManager != null && uiManager != null && uiManager.HistoryUI != null)
            {
                historyManager.OnSpinRecorded += uiManager.HistoryUI.AddRecord;
            }

            if (autoSpinController != null)
            {
                autoSpinController.Initialize(this, payoutManager);
            }

            if (winStreakTracker != null)
            {
                winStreakTracker.Initialize(audioManager);
            }
        }

        /// <summary>
        /// Begins a spin sequence: bet is deducted, outcome is generated by RNG, reels spin, and stop at predetermined symbols.
        /// </summary>
        public void RequestSpin()
        {
            if (currentState != GameState.Idle) return;

            if (payoutManager == null || !payoutManager.HasSufficientCredits())
            {
                SetState(GameState.InsufficientFunds);
                if (uiManager != null) uiManager.DisplayMessage("Insufficient Credits! Lower bet or reset balance.");
                return;
            }

            // 1. Deduct bet from virtual currency balance
            if (!payoutManager.TryDeductBet()) return;

            // 2. Determine outcome via RNG BEFORE reel presentation begins
            currentPredeterminedOutcomes = outcomeGenerator.GenerateOutcome(symbolDatabase);

            // 3. Evaluate predetermined outcome (independent from visual presentation)
            lastResult = winningEvaluator.Evaluate(currentPredeterminedOutcomes, payoutManager.CurrentBet);

            // 4. Update UI and Audio for active spin state
            SetState(GameState.Spinning);
            if (uiManager != null)
            {
                uiManager.SetSpinInteractable(false);
                uiManager.HideResultBanner();
            }
            if (audioManager != null)
            {
                audioManager.PlayButtonClick();
                audioManager.StartSpinLoop();
            }

            // 5. Trigger reel animation to stop on the predetermined symbols
            if (slotMachine != null)
            {
                slotMachine.SpinAll();
                slotMachine.StopReels(currentPredeterminedOutcomes);
            }
            else
            {
                // Fallback if slot machine presentation is absent (e.g. headless)
                HandleAllReelsStopped();
            }
        }

        public void InitializeForTesting(
            SlotMachineController sm,
            RandomOutcomeGenerator rog,
            WinningEvaluator we,
            PayoutManager pm,
            UIManager ui,
            AudioManager am,
            GameHistoryManager hm,
            SymbolDatabase db,
            AutoSpinController asc = null,
            JackpotCelebrationController jcc = null,
            WinStreakTracker wst = null)
        {
            slotMachine = sm;
            outcomeGenerator = rog;
            winningEvaluator = we;
            payoutManager = pm;
            uiManager = ui;
            audioManager = am;
            historyManager = hm;
            symbolDatabase = db;
            autoSpinController = asc;
            jackpotCelebration = jcc;
            winStreakTracker = wst;

            InitializeSubsystems();
            SetState(GameState.Idle);
            UpdateUI();
        }

        public void HandleAllReelsStopped()
        {
            if (audioManager != null)
            {
                audioManager.StopSpinLoop();
                audioManager.PlayReelStop();
            }

            SetState(GameState.Evaluating);

            // Settle transaction and process payout
            int payoutAmount = 0;
            if (payoutManager != null)
            {
                payoutAmount = payoutManager.AwardPayout(lastResult);
            }

            if (lastResult.IsWin)
            {
                SetState(GameState.Payout);
                if (slotMachine != null)
                {
                    slotMachine.SetPaylineHighlights(true);
                }
                if (audioManager != null)
                {
                    audioManager.PlayWin();
                    audioManager.PlayCoinPayout();
                }
                if (uiManager != null)
                {
                    string symName = lastResult.WinningSymbol != null ? lastResult.WinningSymbol.DisplayName : "MATCH";
                    uiManager.ShowWinBanner(payoutAmount, symName);
                }
            }
            else
            {
                if (uiManager != null)
                {
                    uiManager.ShowLossBanner();
                }
            }

            // Modular Bonus 1: Update Win Streak Tracker
            float streakBonus = 1.0f;
            if (winStreakTracker != null)
            {
                streakBonus = winStreakTracker.RecordResult(lastResult.IsWin);
            }

            // Modular Bonus 2: Check & Trigger Jackpot Animation (Diamond match or high payout)
            bool isDiamondMatch = lastResult.IsWin && lastResult.WinningType == SymbolType.Diamond;
            if (jackpotCelebration != null && (isDiamondMatch || payoutAmount >= 1000))
            {
                jackpotCelebration.CheckAndTriggerJackpot(payoutAmount, isDiamondMatch);
            }

            // Record spin into history
            if (historyManager != null && currentPredeterminedOutcomes != null)
            {
                SymbolType[] types = new SymbolType[currentPredeterminedOutcomes.Length];
                for (int i = 0; i < currentPredeterminedOutcomes.Length; i++)
                {
                    types[i] = currentPredeterminedOutcomes[i] != null ? currentPredeterminedOutcomes[i].Type : (SymbolType)(-1);
                }
                int bet = payoutManager != null ? payoutManager.CurrentBet : 10;
                int balAfter = payoutManager != null ? payoutManager.CurrentBalance : 0;
                SpinRecord record = new SpinRecord(0, types, bet, payoutAmount, lastResult.IsWin, balAfter);
                historyManager.RecordSpin(record);
            }

            // Auto-adjust bet if balance is now less than current bet
            if (payoutManager != null)
            {
                payoutManager.AutoAdjustBetToAffordable();
            }

            // Return to Idle ready for next spin
            SetState(GameState.Idle);
            if (uiManager != null)
            {
                uiManager.SetSpinInteractable(true);
            }
        }

        private void SetState(GameState newState)
        {
            currentState = newState;
            if (OnStateChanged != null)
            {
                OnStateChanged(currentState);
            }

            if (uiManager != null)
            {
                switch (currentState)
                {
                    case GameState.Idle:
                        uiManager.UpdateStatus("READY TO SPIN", new Color(0.4f, 0.9f, 0.5f, 1f));
                        break;
                    case GameState.Spinning:
                        uiManager.UpdateStatus("SPINNING...", new Color(0.85f, 0.65f, 1f, 1f));
                        break;
                    case GameState.Evaluating:
                        uiManager.UpdateStatus("EVALUATING...", new Color(1f, 0.85f, 0.3f, 1f));
                        break;
                    case GameState.Payout:
                        uiManager.UpdateStatus("PAYOUT!", new Color(1f, 0.9f, 0.2f, 1f));
                        break;
                    case GameState.InsufficientFunds:
                        uiManager.UpdateStatus("INSUFFICIENT FUNDS", new Color(1f, 0.35f, 0.35f, 1f));
                        break;
                }
            }
        }

        private void UpdateUI()
        {
            if (uiManager != null && payoutManager != null)
            {
                uiManager.UpdateBalance(payoutManager.CurrentBalance);
                uiManager.UpdateBet(payoutManager.CurrentBet);
                uiManager.SetSpinInteractable(currentState == GameState.Idle);
            }
        }

        /// <summary>
        /// Reloads player's virtual balance with demo credits (default: 1,000 G).
        /// </summary>
        public void ReloadCredits(int amount = 1000)
        {
            if (payoutManager != null)
            {
                payoutManager.ResetBalance(amount);
            }
            SetState(GameState.Idle);
            if (uiManager != null)
            {
                uiManager.DisplayMessage("Credits Reloaded! +1,000 G");
            }
        }
    }
}
